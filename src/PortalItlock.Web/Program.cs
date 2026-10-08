using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Components;
using PortalItlock.Web.Data;
using PortalItlock.Web.Services;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using PortalItlock.Web.Models;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(railwayPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");
}

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 200_000_000;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SignalR sin standardgrense for én enkelt JS<->.NET-melding er 32 KB - et
// base64-innlimt bilde fra utklippstavlen (se vareBilde.js) sprenger den
// grensen med det samme, og kretsen kobler da bare fra/til igjen uten noen
// synlig feilmelding. Økes derfor til 10 MB, likt grensen for filopplasting
// via InputFile andre steder i appen.
builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 10_000_000;
});

var dataProtectionKeysPath = Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH");
if (string.IsNullOrEmpty(dataProtectionKeysPath) && builder.Environment.IsDevelopment())
{
    // Uten dette genereres en ny nøkkel for hver "dotnet run" i lokal dev, som
    // gjør alle innloggingscookies ugyldige med en gang serveren restartes -
    // upraktisk når man tester etter hver kodeendring. Påvirker ikke prod,
    // som alltid har DATA_PROTECTION_KEYS_PATH satt via miljøvariabel.
    dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, ".dataprotection-keys");
}
if (!string.IsNullOrEmpty(dataProtectionKeysPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

// Flerkunde-oppsett (steg 1): ApplicationDbContext peker på én SQLite-fil
// per kunde (tenant), valgt av TenantContext i stedet for en fast
// connection string. PlatformDbContext er katalogen over hvilke kunder som
// finnes og hvilken fil hver av dem eier - lever i en egen liten SQLite-fil
// ved siden av den vanlige databasen (samme mappe/volum, ingen ny
// Railway-konfigurasjon nødvendig). I dag finnes det kun én kunde (itlock AS
// selv, markert ErStandard), som fortsatt peker på nøyaktig samme fil som før
// - ingen data er flyttet.
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var platformConnectionString = PlatformConnectionStringHelper.AvledFra(defaultConnectionString);

builder.Services.AddDbContext<PlatformDbContext>(options =>
    options.UseSqlite(platformConnectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var tenantContext = sp.GetRequiredService<ITenantContext>();
    options.UseSqlite(tenantContext.Current?.ConnectionString ?? defaultConnectionString);
});

builder.Services.AddScoped<TenantProvisioningService>();
builder.Services.AddSingleton<TenantStatistikkService>();
builder.Services.AddScoped<TenantOppslagService>();

builder.Services.AddScoped<PackageMatchingService>();
builder.Services.AddScoped<MobilVerktoylinjeService>();
builder.Services.AddScoped<TilbudPdfService>();
builder.Services.AddScoped<TimeoversiktService>();
builder.Services.AddScoped<FdvPdfService>();
builder.Services.AddScoped<DorBeslagslistePdfService>();
builder.Services.AddScoped<PlukklistePdfService>();
builder.Services.AddScoped<TicketRapportPdfService>();
builder.Services.AddScoped<ProduktsammendragPdfService>();
builder.Services.AddScoped<DorpakkePdfService>();
builder.Services.AddScoped<LasplanPdfService>();
builder.Services.AddScoped<TripletexOrdreCsvService>();
builder.Services.AddScoped<PrisimportService>();
builder.Services.AddScoped<PrisoppdateringService>();
builder.Services.AddScoped<PrisendringUtforerService>();
builder.Services.AddHostedService<PrisendringBackgroundService>();
builder.Services.AddScoped<AvvikPdfService>();
builder.Services.AddScoped<PlanUtstyrPdfService>();
builder.Services.AddScoped<KoblingsSkjemaPdfService>();
builder.Services.AddScoped<PlantegningDorPdfService>();
builder.Services.AddScoped<ServicerapportPdfService>();
builder.Services.AddScoped<ArbeidsordrePdfService>();
builder.Services.AddScoped<BefaringPdfService>();
builder.Services.AddScoped<SjekklistePdfService>();
builder.Services.AddScoped<LagretPdfService>();
builder.Services.AddScoped<TilbudSyncService>();
builder.Services.AddScoped<CeGodkjenningPdfService>();
builder.Services.AddScoped<ServiceVarselService>();
builder.Services.AddHostedService<ServiceVarselBackgroundService>();
builder.Services.AddScoped<LisensVarselService>();
builder.Services.AddHostedService<LisensVarselBackgroundService>();
builder.Services.AddScoped<TicketEskaleringService>();
builder.Services.AddScoped<VarselTellerService>();
builder.Services.AddHostedService<TicketEskaleringBackgroundService>();
builder.Services.AddHttpClient<DatabaseBackupService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
});
builder.Services.AddHostedService<DatabaseBackupBackgroundService>();
builder.Services.AddSingleton<PdfLogo>();
builder.Services.AddSingleton<PostnummerService>();
builder.Services.AddSingleton<PresenceService>();
builder.Services.AddHttpClient<EmailService>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddHttpClient<ResendReceivingClient>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddHttpClient<GeocodingService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHttpClient<BronnoysundClient>(client =>
{
    client.BaseAddress = new Uri("https://data.brreg.no/enhetsregisteret/api/");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHttpClient<AiAssistentService>(client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com/");
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHttpClient<DorImportService>(client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com/");
    client.Timeout = TimeSpan.FromSeconds(120);
});
builder.Services.Configure<TripletexOptions>(builder.Configuration.GetSection("Tripletex"));
// Navngitt (ikke typet) HttpClient - TripletexService må være singleton for at
// det cachede session-tokenet (se HentSessionTokenAsync) faktisk skal deles på
// tvers av forespørsler, i stedet for å lages på nytt for hver injeksjon.
builder.Services.AddHttpClient(nameof(TripletexService), (sp, client) =>
{
    var baseUrl = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TripletexOptions>>().Value.BaseUrl;
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddSingleton<TripletexService>(sp =>
    new TripletexService(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(TripletexService)),
        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TripletexOptions>>()));
builder.Services.AddScoped<TripletexSyncService>();
builder.Services.AddHostedService<TripletexSyncBackgroundService>();

// To helt adskilte innloggings-cookies: den vanlige (kundenes egne brukere,
// per tenant-database) og "Plattform" (kun deg - gir tilgang til /plattform
// for å opprette/administrere kunder). Ingen Admin-rolle hos noen kunde, selv
// itlock AS sin egen, gir noensinne tilgang til Plattform-cookien - det er en
// helt egen konto (PlattformBruker) i PlatformDbContext.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    })
    .AddCookie("Plattform", options =>
    {
        options.Cookie.Name = "PlattformAuth";
        options.LoginPath = "/plattform/logg-inn";
        options.AccessDeniedPath = "/plattform/logg-inn";
        // Kortere levetid enn den vanlige brukercookien (30 dager) med vilje -
        // denne gir tilgang til å opprette/administrere/slette organisasjoner,
        // så konsekvensen av en stjålet cookie er mye høyere, mens gjeninn-
        // logging her koster lite (lite trafikkert, kun én bruker). Se
        // sikkerhetsgjennomgangen 2026-10-08.
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization(options =>
{
    // Må godta BEGGE cookie-schemaene her - ikke bare standard-schemaet - ellers
    // mister en innlogget plattform-admin (egen "Plattform"-cookie, se over)
    // automatisk tilgang til Blazors egen interaktive krets (SignalR-hub/
    // rammeverksfiler går via denne fallback-policyen når de ikke har noen egen
    // [Authorize]), og sidene faller stille tilbake til ikke-interaktiv
    // sideinnlasting ved hvert klikk uten noen synlig feil.
    options.FallbackPolicy = new AuthorizationPolicyBuilder(
            CookieAuthenticationDefaults.AuthenticationScheme, "Plattform")
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy("Plattform", policy => policy
        .AddAuthenticationSchemes("Plattform")
        .RequireAuthenticatedUser());
});
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRateLimiter(options =>
{
    // Begrenser innloggings- og passord-tilbakestillingsforsøk PER IP (via
    // AddPolicy + partisjonsnøkkel), slik at én klient ikke kan tømme en
    // delt/global kvote og dermed sperre innlogging for alle andre brukere -
    // AddFixedWindowLimiter alene (slik dette sto før) lager én delt teller
    // for ALLE klienter samlet, uansett IP, og var dermed selv en DoS-vei.
    static string KlientIp(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "ukjent";

    options.AddPolicy("login", http => RateLimitPartition.GetFixedWindowLimiter(KlientIp(http), _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = 8,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0
    }));
    options.AddPolicy("passord-reset", http => RateLimitPartition.GetFixedWindowLimiter(KlientIp(http), _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = 3,
        Window = TimeSpan.FromMinutes(5),
        QueueLimit = 0
    }));
    options.OnRejected = (context, _) =>
    {
        var path = context.HttpContext.Request.Path;
        var malTilbake = path.StartsWithSegments("/account/glemt-passord")
            ? "/glemt-passord?forMange=1"
            : "/login?forMange=1";
        context.HttpContext.Response.Redirect(malTilbake);
        return ValueTask.CompletedTask;
    };
});

var app = builder.Build();

using (var seedScope = app.Services.CreateScope())
{
    var platformDb = seedScope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    platformDb.Database.Migrate();

    // Steg 1 av flerkunde-oppsettet: sørger for at itlock AS finnes som
    // "standard"-kunde, pekende på nøyaktig samme fil som appen alltid har
    // brukt - ingen data flyttes. Nye kunder legges til her senere via en
    // egen admin-side, ikke ved å redigere dette.
    if (!platformDb.Tenants.Any())
    {
        platformDb.Tenants.Add(new Tenant
        {
            Navn = "itlock AS",
            ConnectionString = defaultConnectionString,
            ErStandard = true
        });
        platformDb.SaveChanges();
    }

    // Backfyller vertsnavnet til standard-kunden (itlock) med Railways
    // offentlige domene, slik at den fortsatt resolves korrekt nå som et
    // ukjent vertsnavn ikke lenger faller tilbake til den automatisk (se
    // ITenantContext). Gjøres kun når Railway faktisk oppgir domenet (altså
    // aldri lokalt), og kun én gang - overskriver ikke et senere satt egendefinert domene.
    var produksjonsVertsnavn = Environment.GetEnvironmentVariable("RAILWAY_PUBLIC_DOMAIN");
    if (!string.IsNullOrEmpty(produksjonsVertsnavn))
    {
        var standardTenant = platformDb.Tenants.FirstOrDefault(t => t.ErStandard && t.Subdomene == null);
        if (standardTenant is not null)
        {
            standardTenant.Subdomene = produksjonsVertsnavn;
            platformDb.SaveChanges();
        }
    }

    if (!platformDb.PlattformBrukere.Any())
    {
        platformDb.PlattformBrukere.Add(new PlattformBruker
        {
            Navn = "Marius Karlsen",
            Epost = "marius@itlock.no"
        });
        platformDb.SaveChanges();
    }

    var aktiveTenants = platformDb.Tenants.Where(t => t.Status == TenantStatus.Aktiv).ToList();
    foreach (var tenant in aktiveTenants)
    {
        // Én leietakers database som feiler under migrering (f.eks. et
        // avbrutt tidligere migreringsforsøk, se 2026-10-08-krasjet) skal
        // aldri ta ned hele appen for alle andre kunder - logg og fortsett
        // til neste leietaker i stedet for å la unntaket boble opp og
        // krasje prosessen.
        try
        {
            var tenantOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(tenant.ConnectionString)
                .Options;
            using var seedDb = new ApplicationDbContext(tenantOptions);
            seedDb.Database.Migrate();
            PortalItlock.Web.Services.TenantSeedHelper.SeedNyheter(seedDb);

            if (!seedDb.Brukere.Any(b => b.Rolle == PortalItlock.Web.Models.BrukerRolle.Admin))
            {
                seedDb.Brukere.Add(new PortalItlock.Web.Models.Bruker
                {
                    Navn = "Marius Karlsen",
                    Epost = "marius@itlock.no",
                    Rolle = PortalItlock.Web.Models.BrukerRolle.Admin
                });
                seedDb.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Migrering/seeding feilet for leietaker {TenantId} ({TenantNavn}) - hopper over og fortsetter med neste leietaker.", tenant.Id, tenant.Navn);
        }
    }
}

// Configure the HTTP request pipeline.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Grunnleggende sikkerhets-headere på alt - trygt å sette globalt siden
// ingen av dem endrer hvordan siden faktisk fungerer, kun hvordan
// nettleseren håndterer den (hindrer f.eks. at innloggingssiden eller
// den autentiserte appen legges i en skjult iframe på et annet nettsted
// for clickjacking, se sikkerhetsgjennomgangen 2026-10-08).
app.Use(async (context, next) =>
{
    // SAMEORIGIN/'self', ikke DENY/'none' - appen bruker selv <iframe> for
    // PDF-forhåndsvisning (PdfPreviewModal.razor m.fl., samme-opphav), som
    // DENY/'none' ville blokkert.
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'";
    await next();
});

app.UseStaticFiles();
app.UseAntiforgery();

// UseAuthentication() MÅ kjøre før begge de egendefinerte middlewarene under
// - ITenantContext leser nå en "TenantId"-claim fra innloggingscookien
// (se Services/ITenantContext.cs), og den claimen finnes først etter at
// UseAuthentication() har bygget context.User fra cookien.
app.UseAuthentication();
app.UseAuthorization();

// Mobil skal alltid starte på Oppgaver ("/min-dag"), ikke skrivebordets
// prosjektoversikt på "/". Dette var tidligere en klientside-omdirigering
// (window.location.replace i App.razor), men det ga et kaldstart-tilfelle
// med TO fulle sidelastinger etter hverandre (først "/", så "/min-dag") som
// viste seg å utløse en iOS-kvirk i hjemskjerm-app-modus: den faste
// bunn-fanen (position: fixed) ble stående på feil posisjon til man byttet
// fane. Gjøres nå i stedet som en ren server-omdirigering FØR noe HTML i det
// hele tatt sendes, slik at nettleseren bare gjør ÉN navigasjon på kaldstart.
// Kjører fortsatt FØR Blazors egen AuthorizeRouteView-omdirigering til
// /login rekker å skje (den skjer inne i MapRazorComponents, lenger ned) -
// trenger bare å ligge etter UseAuthentication/UseAuthorization, ikke aller først.
app.Use(async (context, next) =>
{
    var erGet = HttpMethods.IsGet(context.Request.Method);

    // Unntak: "Gå til fullversjon" (TopBar.razor) navigerer bevisst til "/"
    // for å vise modul-oversikten - ?fullversjon=1 markerer at dette IKKE
    // skal fanges opp av mobil-omdirigeringen under.
    if (erGet
        && context.Request.Path == "/"
        && !context.Request.Query.ContainsKey("fullversjon")
        && MobilDeteksjon.GjettFraUserAgent(context.Request.Headers.UserAgent.ToString()))
    {
        context.Response.Redirect("/min-dag");
        return;
    }

    await next();
});

// Flerkunde-oppsett: et vertsnavn som ikke tilhører noen kunde (f.eks. det
// bare produkt-domenet uten subdomene, "fullkontroll.no") OG som ikke er
// innlogget (se ITenantContext - innlogget bruker resolves nå via
// TenantId-claimen uansett vertsnavn) skal IKKE lenger vise itlock sine data
// - i stedet sendes brukeren til "Finn min side" for å slå opp riktig kunde
// ut fra e-posten sin. Unntar plattform-sidene (helt egen innlogging, ikke
// kundeknyttet), konto-endepunktene, og statiske filer (kjennetegnet ved filendelse).
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "/";
    var erUnntatt = path.StartsWith("/finn-min-side", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/plattform", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/konto", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/account", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
        || Path.HasExtension(path);

    if (!erUnntatt && HttpMethods.IsGet(context.Request.Method))
    {
        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
        if (tenantContext.Current is null)
        {
            context.Response.Redirect("/finn-min-side");
            return;
        }
    }

    await next();
});

app.UseRateLimiter();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/account/login", async (HttpContext http, TenantOppslagService oppslag) =>
{
    var form = await http.Request.ReadFormAsync();
    var epost = form["username"].ToString().Trim();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var safeReturnUrl = !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
        ? returnUrl
        : "/";

    // Slår opp hvilken organisasjon e-posten hører til FØR passordsjekk - de
    // kan dele samme adresse med andre organisasjoner (se TenantOppslagService),
    // så dette er ikke nødvendigvis organisasjonen vertsnavnet i seg selv peker til.
    var tenant = await oppslag.FinnTenantForEpostAsync(epost);

    Bruker? bruker = null;
    if (tenant is not null)
    {
        var tenantOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(tenant.ConnectionString)
            .Options;
        await using var db = new ApplicationDbContext(tenantOptions);
        var brukerKandidater = await db.Brukere.ToListAsync();
        bruker = brukerKandidater.FirstOrDefault(b => EpostHjelper.ErLik(b.Epost, epost));
    }

    // Kjøres alltid - også når organisasjonen/brukeren ikke finnes, mot en
    // fast "dummy"-hash - slik at svartiden ikke avslører om kontoen
    // finnes (tidsbasert sidekanal, se sikkerhetsgjennomgangen 2026-10-08).
    var passwordOk = PasswordHasher.Verify(password, bruker?.PasswordHash ?? PasswordHasher.DummyHash);

    if (tenant is null || bruker is null || !passwordOk || !bruker.Aktiv)
    {
        return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(safeReturnUrl)}&feil=1");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, bruker.Navn),
        new(ClaimTypes.Role, bruker.Rolle.ToString()),
        new("BrukerId", bruker.Id.ToString()),
        new("TenantId", tenant.Id.ToString())
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    return Results.Redirect(safeReturnUrl);
}).AllowAnonymous().RequireRateLimiting("login");

app.MapPost("/account/glemt-passord", async (HttpContext http, TenantOppslagService oppslag, EmailService epost) =>
{
    var form = await http.Request.ReadFormAsync();
    var epostAdresse = form["epost"].ToString().Trim();

    // "Sett passord første gang" (/sett-passord) og "Glemt passord"
    // (/glemt-passord) er nå samme sikre, token-baserte flyt - det finnes
    // ingen reell forskjell mellom de to (begge ender med å sette
    // PasswordHash via et e-postet, tidsbegrenset token), så de deler dette
    // endepunktet. redirectTil styrer kun hvilken side den vennlige
    // "lenke sendt"-meldingen vises på, og er begrenset til disse to kjente,
    // trygge sidene - ikke en åpen omdirigering.
    var redirectTil = form["redirectTil"].ToString() == "sett-passord" ? "sett-passord" : "glemt-passord";

    var tenant = await oppslag.FinnTenantForEpostAsync(epostAdresse);
    if (tenant is not null)
    {
        var tenantOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(tenant.ConnectionString).Options;
        await using var db = new ApplicationDbContext(tenantOptions);

        var glemtKandidater = await db.Brukere.ToListAsync();
        var bruker = glemtKandidater.FirstOrDefault(b => EpostHjelper.ErLik(b.Epost, epostAdresse));
        if (bruker is not null && bruker.Aktiv)
        {
            var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            db.BrukerPasswordResetTokener.Add(new BrukerPasswordResetToken
            {
                BrukerId = bruker.Id,
                Token = token,
                UtlopsDato = DateTime.Now.AddHours(1)
            });
            await db.SaveChangesAsync();

            var lenke = $"{http.Request.Scheme}://{http.Request.Host}/tilbakestill-passord?token={token}";
            var html = "<p>Hei,</p>" +
                "<p>Du (eller noen andre) har bedt om å sette eller tilbakestille passordet for kontoen din hos itlock.</p>" +
                $"<p><a href=\"{lenke}\">Trykk her for å velge nytt passord</a></p>" +
                "<p>Lenken er gyldig i 1 time. Har du ikke bedt om dette, kan du se bort fra denne e-posten.</p>";
            await epost.SendAsync(bruker.Epost, "Tilbakestill passord - itlock", html);
        }
    }

    // Samme melding uansett om e-posten finnes hos oss eller ikke,
    // slik at man ikke kan bruke skjemaet til å sjekke hvem som er registrert.
    return Results.Redirect($"/{redirectTil}?sendt=1");
}).AllowAnonymous().RequireRateLimiting("passord-reset");

app.MapPost("/account/tilbakestill-passord", async (HttpContext http, PlatformDbContext platformDb) =>
{
    var form = await http.Request.ReadFormAsync();
    var token = form["token"].ToString();
    var passord = form["passord"].ToString();
    var bekreft = form["bekreft"].ToString();

    // Tokenet finnes i organisasjonens EGEN database, og vertsnavnet man
    // trykket lenken på er ikke nødvendigvis den organisasjonen (delt
    // adresse) - søker derfor på tvers av aktive organisasjoner, samme
    // mønster som i TenantOppslagService.
    var aktive = await platformDb.Tenants.Where(t => t.Status == TenantStatus.Aktiv).ToListAsync();
    foreach (var tenant in aktive)
    {
        var tenantOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(tenant.ConnectionString).Options;
        await using var db = new ApplicationDbContext(tenantOptions);

        var resetToken = await db.BrukerPasswordResetTokener
            .Include(t => t.Bruker)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (resetToken is null)
        {
            continue;
        }

        if (resetToken.Bruker is null || resetToken.Brukt || resetToken.UtlopsDato < DateTime.Now)
        {
            return Results.Redirect("/tilbakestill-passord?feil=ugyldig-token");
        }
        if (passord.Length < 8 || passord != bekreft)
        {
            return Results.Redirect($"/tilbakestill-passord?token={Uri.EscapeDataString(token)}&feil=ugyldig-passord");
        }

        resetToken.Bruker.PasswordHash = PasswordHasher.Hash(passord);
        resetToken.Brukt = true;
        await db.SaveChangesAsync();

        return Results.Redirect("/login?satt=1");
    }

    return Results.Redirect("/tilbakestill-passord?feil=ugyldig-token");
}).AllowAnonymous().RequireRateLimiting("passord-reset");

app.MapPost("/account/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).RequireAuthorization();

// "Finn min side": felles inngang på det bare produkt-domenet (uten
// subdomene) - slår opp hvilken kunde som eier e-posten og sender brukeren
// videre til riktig <kunde>.fullkontroll.no/login, forhåndsutfylt. Selve
// passordsjekken skjer fortsatt der, mot nøyaktig den kundens egen
// Brukere-tabell - dette er bare et oppslag, ikke en innlogging i seg selv.
app.MapPost("/konto/finn-min-side", async (HttpContext http, PlatformDbContext platformDb) =>
{
    var form = await http.Request.ReadFormAsync();
    var epost = form["epost"].ToString().Trim();

    if (!string.IsNullOrWhiteSpace(epost))
    {
        var aktiveKunder = await platformDb.Tenants
            .Where(t => t.Status == TenantStatus.Aktiv && t.Subdomene != null)
            .ToListAsync();

        foreach (var kunde in aktiveKunder)
        {
            var kundeOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(kunde.ConnectionString)
                .Options;
            using var kundeDb = new ApplicationDbContext(kundeOptions);
            var kundeBrukere = await kundeDb.Brukere.Select(b => b.Epost).ToListAsync();
            var finnes = kundeBrukere.Any(e => EpostHjelper.ErLik(e, epost));
            if (finnes)
            {
                var lenke = $"{http.Request.Scheme}://{kunde.Subdomene}/login?epost={Uri.EscapeDataString(epost)}";
                return Results.Redirect(lenke);
            }
        }
    }

    return Results.Redirect("/finn-min-side?ikkeFunnet=1");
}).AllowAnonymous().RequireRateLimiting("login");

// Plattform-innlogging: helt egen cookie ("Plattform"-schemaet) og helt egen
// konto-tabell (PlattformBruker i PlatformDbContext) - se kommentaren ved
// AddAuthentication lenger opp for hvorfor dette er adskilt fra /account/*.
app.MapPost("/plattform/konto/login", async (HttpContext http, PlatformDbContext db) =>
{
    var form = await http.Request.ReadFormAsync();
    var epost = form["username"].ToString().Trim();
    var password = form["password"].ToString();

    var plattformKandidater = await db.PlattformBrukere.ToListAsync();
    var bruker = plattformKandidater.FirstOrDefault(b => EpostHjelper.ErLik(b.Epost, epost));
    var passwordOk = bruker?.PasswordHash is not null && PasswordHasher.Verify(password, bruker.PasswordHash);

    if (bruker is null || !passwordOk)
    {
        return Results.Redirect("/plattform/logg-inn?feil=1");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, bruker.Navn),
        new("PlattformBrukerId", bruker.Id.ToString())
    };
    var identity = new ClaimsIdentity(claims, "Plattform");
    await http.SignInAsync("Plattform", new ClaimsPrincipal(identity));

    return Results.Redirect("/plattform");
}).AllowAnonymous().RequireRateLimiting("login");

app.MapPost("/plattform/konto/glemt-passord", async (HttpContext http, PlatformDbContext db, EmailService epost) =>
{
    var form = await http.Request.ReadFormAsync();
    var epostAdresse = form["epost"].ToString().Trim();

    // Se tilsvarende kommentar i /account/glemt-passord - "sett passord
    // første gang" og "glemt passord" er samme sikre, token-baserte flyt.
    var redirectTil = form["redirectTil"].ToString() == "sett-passord" ? "plattform/sett-passord" : "plattform/glemt-passord";

    var plattformGlemtKandidater = await db.PlattformBrukere.ToListAsync();
    var bruker = plattformGlemtKandidater.FirstOrDefault(b => EpostHjelper.ErLik(b.Epost, epostAdresse));
    if (bruker is not null)
    {
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        db.PlattformBrukerPasswordResetTokener.Add(new PlattformBrukerPasswordResetToken
        {
            PlattformBrukerId = bruker.Id,
            Token = token,
            UtlopsDato = DateTime.Now.AddHours(1)
        });
        await db.SaveChangesAsync();

        var lenke = $"{http.Request.Scheme}://{http.Request.Host}/plattform/tilbakestill-passord?token={token}";
        var html = "<p>Hei,</p>" +
            "<p>Du (eller noen andre) har bedt om å sette eller tilbakestille passordet for plattform-kontoen din.</p>" +
            $"<p><a href=\"{lenke}\">Trykk her for å velge nytt passord</a></p>" +
            "<p>Lenken er gyldig i 1 time. Har du ikke bedt om dette, kan du se bort fra denne e-posten.</p>";
        await epost.SendAsync(bruker.Epost, "Sett/tilbakestill plattform-passord - itlock", html);
    }

    return Results.Redirect($"/{redirectTil}?sendt=1");
}).AllowAnonymous().RequireRateLimiting("passord-reset");

app.MapPost("/plattform/konto/tilbakestill-passord", async (HttpContext http, PlatformDbContext db) =>
{
    var form = await http.Request.ReadFormAsync();
    var token = form["token"].ToString();
    var passord = form["passord"].ToString();
    var bekreft = form["bekreft"].ToString();

    var resetToken = await db.PlattformBrukerPasswordResetTokener
        .Include(t => t.PlattformBruker)
        .FirstOrDefaultAsync(t => t.Token == token);

    if (resetToken?.PlattformBruker is null || resetToken.Brukt || resetToken.UtlopsDato < DateTime.Now)
    {
        return Results.Redirect("/plattform/tilbakestill-passord?feil=ugyldig-token");
    }
    if (passord.Length < 8 || passord != bekreft)
    {
        return Results.Redirect($"/plattform/tilbakestill-passord?token={Uri.EscapeDataString(token)}&feil=ugyldig-passord");
    }

    resetToken.PlattformBruker.PasswordHash = PasswordHasher.Hash(passord);
    resetToken.Brukt = true;
    await db.SaveChangesAsync();

    return Results.Redirect("/plattform/logg-inn?satt=1");
}).AllowAnonymous().RequireRateLimiting("passord-reset");

app.MapPost("/plattform/konto/logout", async (HttpContext http) =>
{
    await http.SignOutAsync("Plattform");
    return Results.Redirect("/plattform/logg-inn");
}).RequireAuthorization("Plattform");

app.MapGet("/bilder/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var bilde = await db.BefaringDorfeltBilder.FindAsync(id);
    return bilde is null ? Results.NotFound() : Results.File(bilde.Data, FilSikkerhet.TryggContentType(bilde.ContentType));
}).RequireAuthorization();

app.MapGet("/systemvedlegg/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var vedlegg = await db.SystemVedlegg.FindAsync(id);
    return vedlegg is null
        ? Results.NotFound()
        : Results.File(vedlegg.Data, FilSikkerhet.TryggContentType(vedlegg.ContentType), vedlegg.Filnavn);
}).RequireAuthorization();

app.MapGet("/nokkelsystem/{id:int}/bilde", async (int id, ApplicationDbContext db) =>
{
    var system = await db.Nokkelsystemer.FindAsync(id);
    return system?.BildeData is null
        ? Results.NotFound()
        : Results.File(system.BildeData, FilSikkerhet.TryggContentType(system.BildeContentType ?? "image/jpeg"));
}).RequireAuthorization();

app.MapGet("/plantegningbilde/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var plantegning = await db.Plantegninger.FindAsync(id);
    return plantegning is null
        ? Results.NotFound()
        : Results.File(plantegning.Data, FilSikkerhet.TryggContentType(plantegning.ContentType));
}).RequireAuthorization();

app.MapGet("/prosjektvedlegg/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var vedlegg = await db.ProsjektVedlegg.FindAsync(id);
    // Inline i stedet for attachment - skal åpnes som forhåndsvisning i egen
    // fane akkurat som de genererte PDF-ene (tilbud, plukkliste osv.) gjør,
    // ikke tvinge frem nedlasting slik Results.File med filnavn ellers gjør.
    return vedlegg is null
        ? Results.NotFound()
        : new InlineFileResult(vedlegg.Data, vedlegg.ContentType, vedlegg.Filnavn);
}).RequireAuthorization();

app.MapGet("/nedlastningsfil/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var fil = await db.NedlastningsFiler.FindAsync(id);
    // Inline i stedet for attachment - skal kunne åpnes som forhåndsvisning i
    // egen fane, ikke tvinge frem nedlasting.
    return fil is null
        ? Results.NotFound()
        : new InlineFileResult(fil.Data, fil.ContentType, fil.Filnavn);
}).RequireAuthorization();

app.MapGet("/koblingsbibliotek/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var symbol = await db.KoblingsSymbolBibliotek.FindAsync(id);
    return symbol is null
        ? Results.NotFound()
        : Results.File(symbol.BildeData, FilSikkerhet.TryggContentType(symbol.BildeContentType));
}).RequireAuthorization();

app.MapGet("/tilbud/{id:int}/pdf", async (int id, HttpContext context, ApplicationDbContext db, TilbudPdfService pdfService) =>
{
    var pdf = await pdfService.GenerateAsync(id);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var tilbud = await db.Tilbud.Include(t => t.Prosjekt).FirstAsync(t => t.Id == id);
    var prosjektNavn = tilbud.Prosjekt?.Navn ?? "";
    var filnavn = $"Tilbud - {tilbud.Tittel} - {prosjektNavn} - itlock AS - {tilbud.OpprettetDato:dd.MM.yyyy}.pdf";
    foreach (var ugyldig in Path.GetInvalidFileNameChars())
    {
        filnavn = filnavn.Replace(ugyldig, '-');
    }

    // Vises i nettleseren først, slik at tilbudet kan leses gjennom før det evt. lastes ned.
    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/tilbud/{id:int}/tripletex-csv", async (int id, ApplicationDbContext db, TripletexOrdreCsvService csvService) =>
{
    var (data, feil) = await csvService.GenerateAsync(id);
    if (data is null)
    {
        return Results.BadRequest(feil);
    }

    var tilbud = await db.Tilbud.FirstAsync(t => t.Id == id);
    var filnavn = $"Tripletex-ordre - {tilbud.Tittel} - {DateTime.Now:dd.MM.yyyy}.csv";
    foreach (var ugyldig in Path.GetInvalidFileNameChars())
    {
        filnavn = filnavn.Replace(ugyldig, '-');
    }

    return Results.File(data, "text/csv", filnavn);
}).RequireAuthorization();

app.MapGet("/prosjekt/{id:int}/beslagsliste/pdf", async (int id, string? byggetrinn, HttpContext context, ApplicationDbContext db, DorBeslagslistePdfService beslagslisteService) =>
{
    var prosjekt = await db.Prosjekter.FindAsync(id);
    if (prosjekt is null)
    {
        return Results.NotFound();
    }

    var pdf = await beslagslisteService.GenerateAsync(id, byggetrinn);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var filnavn = $"Beslagsliste - {prosjekt.Navn} - itlock AS - {DateTime.Now:dd.MM.yyyy}.pdf";
    foreach (var ugyldig in Path.GetInvalidFileNameChars())
    {
        filnavn = filnavn.Replace(ugyldig, '-');
    }

    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/prosjekt/{id:int}/plukkliste/pdf", async (int id, string? byggetrinn, HttpContext context, ApplicationDbContext db, PlukklistePdfService plukklisteService) =>
{
    var pdf = await plukklisteService.GenerateAsync(id, byggetrinn);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var prosjekt = await db.Prosjekter.FindAsync(id);
    var filnavn = $"Plukkliste - {prosjekt?.Navn} - itlock AS - {DateTime.Now:dd.MM.yyyy}.pdf";
    foreach (var ugyldig in Path.GetInvalidFileNameChars())
    {
        filnavn = filnavn.Replace(ugyldig, '-');
    }

    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/prosjekt/{id:int}/produktsammendrag/pdf", async (int id, HttpContext context, ApplicationDbContext db, ProduktsammendragPdfService sammendragService) =>
{
    var pdf = await sammendragService.GenerateAsync(id);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var prosjekt = await db.Prosjekter.FindAsync(id);
    var filnavn = $"Produktsammendrag - {prosjekt?.Navn} - itlock AS - {DateTime.Now:dd.MM.yyyy}.pdf";
    foreach (var ugyldig in Path.GetInvalidFileNameChars())
    {
        filnavn = filnavn.Replace(ugyldig, '-');
    }

    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/dorpakke/{id:int}/pdf", async (int id, HttpContext context, ApplicationDbContext db, DorpakkePdfService dorpakkePdfService) =>
{
    var pdf = await dorpakkePdfService.GenerateAsync(id);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var pakke = await db.Packages.FindAsync(id);
    var filnavn = $"Dorpakke - {pakke?.Navn} - itlock AS - {DateTime.Now:dd.MM.yyyy}.pdf";
    foreach (var ugyldig in Path.GetInvalidFileNameChars())
    {
        filnavn = filnavn.Replace(ugyldig, '-');
    }

    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/prosjekt/{id:int}/lasplan/pdf", async (int id, HttpContext context, ApplicationDbContext db, LasplanPdfService lasplanService) =>
{
    var pdf = await lasplanService.GenerateAsync(id);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var prosjekt = await db.Prosjekter.FindAsync(id);
    var filnavn = $"Låsplan - {prosjekt?.Navn} - itlock AS - {DateTime.Now:dd.MM.yyyy}.pdf";
    foreach (var ugyldig in Path.GetInvalidFileNameChars())
    {
        filnavn = filnavn.Replace(ugyldig, '-');
    }

    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/servicehenvendelse/bilde/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var bilde = await db.ServicehenvendelseBilder.FindAsync(id);
    return bilde is null
        ? Results.NotFound()
        : Results.File(bilde.Data, FilSikkerhet.TryggContentType(bilde.ContentType), bilde.Filnavn);
}).RequireAuthorization();

app.MapGet("/tilvalgalternativ/{id:int}/bilde", async (int id, ApplicationDbContext db) =>
{
    var alternativ = await db.TilvalgAlternativer.FindAsync(id);
    return alternativ?.BildeData is null
        ? Results.NotFound()
        : Results.File(alternativ.BildeData, FilSikkerhet.TryggContentType(alternativ.BildeContentType ?? "application/octet-stream"));
}).RequireAuthorization();

app.MapGet("/tilvalgmalalternativ/{id:int}/bilde", async (int id, ApplicationDbContext db) =>
{
    var alternativ = await db.TilvalgMalAlternativer.FindAsync(id);
    return alternativ?.BildeData is null
        ? Results.NotFound()
        : Results.File(alternativ.BildeData, FilSikkerhet.TryggContentType(alternativ.BildeContentType ?? "application/octet-stream"));
}).RequireAuthorization();

app.MapGet("/tilvalg/{id:int}/kundebilde", async (int id, ApplicationDbContext db) =>
{
    var tilvalg = await db.Tilvalg.FindAsync(id);
    return tilvalg?.KundeOnskeBildeData is null
        ? Results.NotFound()
        : Results.File(tilvalg.KundeOnskeBildeData, FilSikkerhet.TryggContentType(tilvalg.KundeOnskeBildeContentType ?? "application/octet-stream"));
}).RequireAuthorization();

app.MapGet("/driftsmeldingmedia/{id:int}/fil", async (int id, ApplicationDbContext db) =>
{
    var media = await db.DriftsmeldingMedia.FindAsync(id);
    return media is null
        ? Results.NotFound()
        : Results.File(media.Data, FilSikkerhet.TryggContentType(media.ContentType), enableRangeProcessing: true);
}).RequireAuthorization();

app.MapGet("/ticketmedia/{id:int}/fil", async (int id, ApplicationDbContext db) =>
{
    var media = await db.TicketMedia.FindAsync(id);
    return media is null
        ? Results.NotFound()
        : Results.File(media.Data, FilSikkerhet.TryggContentType(media.ContentType), enableRangeProcessing: true);
}).RequireAuthorization();

app.MapGet("/foresporselmedia/{id:int}/fil", async (int id, ApplicationDbContext db) =>
{
    var media = await db.ForesporselMedia.FindAsync(id);
    return media is null
        ? Results.NotFound()
        : Results.File(media.Data, FilSikkerhet.TryggContentType(media.ContentType), enableRangeProcessing: true);
}).RequireAuthorization();

app.MapGet("/komponent/{id:int}/fdv", async (int id, ApplicationDbContext db) =>
{
    var komponent = await db.Components.FindAsync(id);
    return komponent?.FdvData is null
        ? Results.NotFound()
        : Results.File(komponent.FdvData, FilSikkerhet.TryggContentType(komponent.FdvContentType ?? "application/pdf"), komponent.FdvFilnavn);
}).RequireAuthorization();

app.MapGet("/komponent/fdvdokument/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var dokument = await db.ComponentFdvDokumenter.FindAsync(id);
    return dokument is null
        ? Results.NotFound()
        : Results.File(dokument.Data, FilSikkerhet.TryggContentType(dokument.ContentType), dokument.Filnavn);
}).RequireAuthorization();

app.MapGet("/komponent/{id:int}/montasjeblad", async (int id, ApplicationDbContext db) =>
{
    var komponent = await db.Components.FindAsync(id);
    return komponent?.MontasjebladData is null
        ? Results.NotFound()
        : Results.File(komponent.MontasjebladData, FilSikkerhet.TryggContentType(komponent.MontasjebladContentType ?? "application/pdf"), komponent.MontasjebladFilnavn);
}).RequireAuthorization();

app.MapGet("/komponent/montasjebladdokument/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var dokument = await db.ComponentMontasjebladDokumenter.FindAsync(id);
    return dokument is null
        ? Results.NotFound()
        : Results.File(dokument.Data, FilSikkerhet.TryggContentType(dokument.ContentType), dokument.Filnavn);
}).RequireAuthorization();

app.MapGet("/komponent/{id:int}/datablad", async (int id, ApplicationDbContext db) =>
{
    var komponent = await db.Components.FindAsync(id);
    return komponent?.DatabladData is null
        ? Results.NotFound()
        : Results.File(komponent.DatabladData, FilSikkerhet.TryggContentType(komponent.DatabladContentType ?? "application/pdf"), komponent.DatabladFilnavn);
}).RequireAuthorization();

app.MapGet("/komponent/databladdokument/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var dokument = await db.ComponentDatabladDokumenter.FindAsync(id);
    return dokument is null
        ? Results.NotFound()
        : Results.File(dokument.Data, FilSikkerhet.TryggContentType(dokument.ContentType), dokument.Filnavn);
}).RequireAuthorization();

app.MapGet("/komponent/{id:int}/bilde", async (int id, ApplicationDbContext db) =>
{
    var komponent = await db.Components.FindAsync(id);
    return komponent?.BildeData is null
        ? Results.NotFound()
        : Results.File(komponent.BildeData, FilSikkerhet.TryggContentType(komponent.BildeContentType ?? "image/jpeg"), komponent.BildeFilnavn);
}).RequireAuthorization();

app.MapGet("/prosjekt/{id:int}/fdv/pdf", async (int id, string? byggetrinn, HttpContext context, ApplicationDbContext db, FdvPdfService fdvService) =>
{
    var pdf = await fdvService.GenerateAsync(id, byggetrinn);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var prosjekt = await db.Prosjekter.FindAsync(id);
    var filnavn = $"FDV - {prosjekt?.Navn} - itlock AS - {DateTime.Now:dd.MM.yyyy}.pdf";
    foreach (var ugyldig in Path.GetInvalidFileNameChars())
    {
        filnavn = filnavn.Replace(ugyldig, '-');
    }

    // Vises i nettleseren først, slik at FDV-samlingen kan leses gjennom før den evt. lastes ned.
    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/dor/{id:int}/qr.png", async (int id, HttpContext context, ApplicationDbContext db) =>
{
    var finnes = await db.Dorer.AnyAsync(d => d.Id == id);
    if (!finnes)
    {
        return Results.NotFound();
    }

    var url = $"{context.Request.Scheme}://{context.Request.Host}/dor/{id}";
    var png = QrCodeService.GeneratePng(url);
    return Results.File(png, "image/png");
}).RequireAuthorization();

app.MapGet("/nokkelkvittering/{id:int}/signatur.png", async (int id, ApplicationDbContext db) =>
{
    var kvittering = await db.NokkelKvitteringer.FindAsync(id);
    return kvittering?.Signatur is null
        ? Results.NotFound()
        : Results.File(kvittering.Signatur, "image/png");
}).RequireAuthorization();

app.MapGet("/avvik/{id:int}/pdf", async (int id, HttpContext context, AvvikPdfService service) =>
{
    var pdf = await service.GenerateAsync(id);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var filnavn = $"Avvik-{id}-itlock-AS.pdf";
    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/ce/{id:int}/pdf", async (int id, HttpContext context, CeGodkjenningPdfService service) =>
{
    var pdf = await service.GenerateAsync(id);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var filnavn = $"CE-godkjenning-{id}-itlock-AS.pdf";
    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/cegodkjenningmedia/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var media = await db.CeGodkjenningMedia.FindAsync(id);
    return media is null
        ? Results.NotFound()
        : Results.File(media.Data, FilSikkerhet.TryggContentType(media.ContentType), enableRangeProcessing: true);
}).RequireAuthorization();

app.MapGet("/komponenttype/{id:int}/ce-dokument", async (int id, ApplicationDbContext db) =>
{
    var type = await db.ComponentTypes.FindAsync(id);
    return type?.CeDokumentData is null
        ? Results.NotFound()
        : Results.File(type.CeDokumentData, FilSikkerhet.TryggContentType(type.CeDokumentContentType ?? "application/pdf"), type.CeDokumentFilnavn);
}).RequireAuthorization();

app.MapGet("/befaring/{id:int}/pdf", async (int id, HttpContext context, BefaringPdfService service) =>
{
    var pdf = await service.GenerateAsync(id);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var filnavn = $"Befaring-{id}-itlock-AS.pdf";
    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/befaringpdf/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var pdf = await db.BefaringPdfer.FindAsync(id);
    return pdf is null
        ? Results.NotFound()
        : Results.File(pdf.Data, "application/pdf", pdf.Navn);
}).RequireAuthorization();

app.MapGet("/arbeidsordre/{id:int}/sjekkliste/pdf", async (int id, HttpContext context, SjekklistePdfService service) =>
{
    var pdf = await service.GenerateAsync(id);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var filnavn = $"Sjekkliste-{id}-itlock-AS.pdf";
    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/sjekklistepdf/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var pdf = await db.SjekklistePdfer.FindAsync(id);
    return pdf is null
        ? Results.NotFound()
        : Results.File(pdf.Data, "application/pdf", pdf.Navn);
}).RequireAuthorization();

app.MapGet("/lagretpdf/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var pdf = await db.LagredePdfer.FindAsync(id);
    return pdf is null
        ? Results.NotFound()
        : Results.File(pdf.Data, "application/pdf", pdf.Navn);
}).RequireAuthorization();

app.MapGet("/servicerunde/{id:int}/pdf", async (int id, HttpContext context, ServicerapportPdfService service) =>
{
    var pdf = await service.GenerateAsync(id);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var filnavn = $"Servicerapport-{id}-itlock-AS.pdf";
    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/plantegning/{id:int}/utstyr/pdf", async (int id, HttpContext context, PlanUtstyrPdfService service) =>
{
    var pdf = await service.GenerateAsync(id);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var filnavn = $"Utstyr-og-kabeltrekk-{id}-itlock-AS.pdf";
    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/koblingsskjema/{id:int}/pdf", async (int id, HttpContext context, KoblingsSkjemaPdfService service) =>
{
    var pdf = await service.GenerateAsync(id);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var filnavn = $"Koblingsskjema-{id}-itlock-AS.pdf";
    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/plantegning/{id:int}/pdf", async (int id, HttpContext context, PlantegningDorPdfService service) =>
{
    var pdf = await service.GenerateAsync(id);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var filnavn = $"Dorplantegning-{id}-itlock-AS.pdf";
    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/dormedia/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var media = await db.DorMedia.FindAsync(id);
    return media is null ? Results.NotFound() : Results.File(media.Data, FilSikkerhet.TryggContentType(media.ContentType), media.Filnavn);
}).RequireAuthorization();

app.MapGet("/servicerundemedia/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var media = await db.ServicerundeMedia.FindAsync(id);
    return media is null ? Results.NotFound() : Results.File(media.Data, FilSikkerhet.TryggContentType(media.ContentType), media.Filnavn);
}).RequireAuthorization();

app.MapGet("/arbeidsordremedia/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var media = await db.ArbeidsordreMedia.FindAsync(id);
    return media is null ? Results.NotFound() : Results.File(media.Data, FilSikkerhet.TryggContentType(media.ContentType), media.Filnavn);
}).RequireAuthorization();

app.MapGet("/kunngjoringbilde/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var kunngjoring = await db.Kunngjoringer.FindAsync(id);
    return kunngjoring?.BildeData is null
        ? Results.NotFound()
        : Results.File(kunngjoring.BildeData, FilSikkerhet.TryggContentType(kunngjoring.BildeContentType ?? "application/octet-stream"), kunngjoring.BildeFilnavn);
}).RequireAuthorization();

app.MapGet("/leverandorlogo/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var leverandor = await db.Leverandorer.FindAsync(id);
    return leverandor?.LogoData is null
        ? Results.NotFound()
        : Results.File(leverandor.LogoData, FilSikkerhet.TryggContentType(leverandor.LogoContentType ?? "application/octet-stream"), leverandor.LogoFilnavn);
}).RequireAuthorization();

// Organisasjonens eget merkevare-bilde (satt av plattformeier på /plattform)
// - vises i sidemenyen, innloggingssiden osv. for akkurat denne organisasjonen.
// Ingen RequireAuthorization: må kunne vises på innloggingssiden før man er
// logget inn. Faller tilbake til 404 (layoutene bruker da sin vanlige
// fk-logo-mark.png) når organisasjonen ikke har satt et eget.
app.MapGet("/organisasjon/logo", (ITenantContext tenantCtx) =>
{
    var tenant = tenantCtx.Current;
    return tenant?.LogoData is null
        ? Results.NotFound()
        : Results.File(tenant.LogoData, FilSikkerhet.TryggContentType(tenant.LogoContentType ?? "image/png"));
});

// Samme som over, men for forhåndsvisning på /plattform - der admin ser på en
// ANNEN organisasjon enn den han selv tilhører, så ITenantContext (som alltid
// peker på organisasjonen til den innloggede forespørselen) duger ikke her.
app.MapGet("/plattform/organisasjon/{id:int}/logo", async (int id, PlatformDbContext platformDb) =>
{
    var tenant = await platformDb.Tenants.FindAsync(id);
    return tenant?.LogoData is null
        ? Results.NotFound()
        : Results.File(tenant.LogoData, FilSikkerhet.TryggContentType(tenant.LogoContentType ?? "image/png"));
}).RequireAuthorization("Plattform");

app.MapGet("/kundebilde/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var kunde = await db.Kunder.FindAsync(id);
    return kunde?.BildeData is null
        ? Results.NotFound()
        : Results.File(kunde.BildeData, FilSikkerhet.TryggContentType(kunde.BildeContentType ?? "application/octet-stream"), kunde.BildeFilnavn);
}).RequireAuthorization();

app.MapGet("/kundedokument/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var dokument = await db.KundeDokumenter.FindAsync(id);
    return dokument is null
        ? Results.NotFound()
        : Results.File(dokument.Data, FilSikkerhet.TryggContentType(dokument.ContentType), dokument.Filnavn);
}).RequireAuthorization();

app.MapGet("/arbeidsordre/{id:int}/rapport/pdf", async (int id, HttpContext context, ArbeidsordrePdfService service) =>
{
    var pdf = await service.GenerateAsync(id);
    if (pdf is null)
    {
        return Results.NotFound();
    }

    var filnavn = $"Arbeidsordre-{id}-itlock-AS.pdf";
    var disposisjon = new ContentDispositionHeaderValue("inline");
    disposisjon.SetHttpFileName(filnavn);
    context.Response.Headers["Content-Disposition"] = disposisjon.ToString();
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/timeoversikt/eksport-csv", async (DateTime fra, DateTime til, int? montorId, TimeoversiktService service) =>
{
    var registreringer = await service.HentRegistreringerAsync(fra, til, montorId, kunGodkjent: true);
    var csv = service.GenererCsv(registreringer);
    var filnavn = $"timeoversikt-{fra:yyyy-MM-dd}-{til:yyyy-MM-dd}.csv";
    return Results.File(csv, "text/csv", filnavn);
}).RequireAuthorization();

app.MapGet("/timeoversikt/eksport-pdf", async (DateTime fra, DateTime til, int? montorId, ApplicationDbContext db, TimeoversiktService service) =>
{
    var registreringer = await service.HentRegistreringerAsync(fra, til, montorId, kunGodkjent: true);
    var montorNavn = montorId.HasValue
        ? (await db.Brukere.FindAsync(montorId.Value))?.Navn
        : null;
    var pdf = service.GenererPdf(registreringer, fra, til, montorNavn);
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapPost("/api/webhooks/resend-inbound", async (HttpContext http, ApplicationDbContext db, IConfiguration config, ILogger<Program> logger, ResendReceivingClient receivingClient) =>
{
    using var reader = new StreamReader(http.Request.Body);
    var body = await reader.ReadToEndAsync();

    var hemmelighet = config["Resend:InboundWebhookSecret"];
    if (string.IsNullOrWhiteSpace(hemmelighet) || !ResendWebhookVerifier.ErGyldig(http.Request.Headers, body, hemmelighet))
    {
        logger.LogWarning("Avviste innkommende Resend-webhook: ugyldig eller manglende signatur.");
        return Results.Unauthorized();
    }

    var tolket = ResendInboundParser.Tolk(body);
    var innhold = tolket.Innhold;
    var vedlegg = new List<ResendReceivingClient.VedleggData>();

    if (!string.IsNullOrEmpty(tolket.EmailId))
    {
        var fullInnhold = await receivingClient.HentFullInnholdAsync(tolket.EmailId);
        if (fullInnhold is not null)
        {
            if (!string.IsNullOrWhiteSpace(fullInnhold.Tekst))
            {
                innhold = fullInnhold.Tekst;
            }
            vedlegg = fullInnhold.Vedlegg;
        }
    }

    var foresporsel = new PortalItlock.Web.Models.Foresporsel
    {
        FraEpost = tolket.FraEpost,
        FraNavn = tolket.FraNavn,
        Emne = tolket.Emne,
        Innhold = innhold,
        RawJson = body
    };
    db.Foresporsler.Add(foresporsel);

    foreach (var v in vedlegg)
    {
        foresporsel.Media.Add(new PortalItlock.Web.Models.ForesporselMedia
        {
            Filnavn = v.Filnavn,
            ContentType = v.ContentType,
            Data = v.Data
        });
    }

    await db.SaveChangesAsync();

    return Results.Ok();
}).AllowAnonymous();

app.Run();

// IResult som setter Content-Disposition: inline (med bevart filnavn) i
// stedet for attachment - Results.File(...) sin filnavn-parameter tvinger
// alltid frem nedlasting, se bruk på /prosjektvedlegg/{id}.
// Opplastede/innkommende filer (vedlegg, bilder, dokumenter - inkl.
// e-postvedlegg fra den anonyme Resend-webhooken) lagres med AVSENDERENS
// egen, uvaliderte Content-Type. Uten denne sjekken kunne noen lastet opp
// (eller sendt inn via webhooken) en fil med Content-Type "text/html"
// eller "application/javascript", og fått den sendt tilbake akkurat slik
// til hvem som helst som åpnet vedlegget - som ville kjørt skript i
// mottakerens innloggede sesjon (lagret XSS, funnet i
// sikkerhetsgjennomgangen 2026-10-08). Brukes ved all serving av lagret,
// potensielt brukerkontrollert innhold - server-genererte PDF-er/bilder
// (QuestPDF/SkiaSharp) har alltid en hardkodet, trygg Content-Type og
// trenger ikke denne sjekken.
static class FilSikkerhet
{
    private static readonly string[] TrygeTyper =
    [
        "application/pdf", "application/octet-stream",
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp",
        "text/csv", "text/plain"
    ];

    public static string TryggContentType(string? lagretContentType)
    {
        var type = string.IsNullOrWhiteSpace(lagretContentType) ? "application/octet-stream" : lagretContentType.Split(';')[0].Trim();
        return Array.Exists(TrygeTyper, t => string.Equals(t, type, StringComparison.OrdinalIgnoreCase)) ? type : "application/octet-stream";
    }
}

sealed class InlineFileResult(byte[] data, string contentType, string filename) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        var tryggType = FilSikkerhet.TryggContentType(contentType);
        // Falt tilbake til application/octet-stream fordi den lagrede typen
        // ikke er på tillatelseslisten - da skal filen lastes ned, ikke
        // vises inline (som ellers ville kjørt den i nettleseren).
        var disposition = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue(tryggType == contentType ? "inline" : "attachment");
        disposition.SetHttpFileName(filename);
        httpContext.Response.Headers.ContentDisposition = disposition.ToString();
        httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
        httpContext.Response.ContentType = tryggType;
        httpContext.Response.ContentLength = data.Length;
        return httpContext.Response.Body.WriteAsync(data).AsTask();
    }
}

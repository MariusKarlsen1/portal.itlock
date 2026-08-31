using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Components;
using PortalItlock.Web.Data;
using PortalItlock.Web.Services;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<PackageMatchingService>();
builder.Services.AddScoped<TilbudPdfService>();
builder.Services.AddScoped<TimeoversiktService>();
builder.Services.AddScoped<FdvPdfService>();
builder.Services.AddScoped<DorBeslagslistePdfService>();
builder.Services.AddScoped<PlukklistePdfService>();
builder.Services.AddScoped<ProduktsammendragPdfService>();
builder.Services.AddScoped<LasplanPdfService>();
builder.Services.AddScoped<TripletexOrdreCsvService>();
builder.Services.AddScoped<PrisimportService>();
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
builder.Services.AddSingleton<PdfLogo>();
builder.Services.AddHttpClient<EmailService>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddHttpClient<GeocodingService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

using (var seedScope = app.Services.CreateScope())
{
    var seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/account/login", async (HttpContext http, ApplicationDbContext db) =>
{
    var form = await http.Request.ReadFormAsync();
    var epost = form["username"].ToString().Trim();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var safeReturnUrl = !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
        ? returnUrl
        : "/";

    var bruker = await db.Brukere.FirstOrDefaultAsync(b => b.Epost.ToLower() == epost.ToLower());
    var passwordOk = bruker?.PasswordHash is not null && PasswordHasher.Verify(password, bruker.PasswordHash);

    if (bruker is null || !passwordOk || !bruker.Aktiv)
    {
        return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(safeReturnUrl)}&feil=1");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, bruker.Navn),
        new(ClaimTypes.Role, bruker.Rolle.ToString()),
        new("BrukerId", bruker.Id.ToString())
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    return Results.Redirect(safeReturnUrl);
}).AllowAnonymous();

app.MapPost("/account/sett-passord", async (HttpContext http, ApplicationDbContext db) =>
{
    var form = await http.Request.ReadFormAsync();
    var epost = form["epost"].ToString().Trim();
    var passord = form["passord"].ToString();
    var bekreft = form["bekreft"].ToString();

    var bruker = await db.Brukere.FirstOrDefaultAsync(b => b.Epost.ToLower() == epost.ToLower());

    if (bruker is null)
    {
        return Results.Redirect("/sett-passord?feil=finnes-ikke");
    }
    if (bruker.PasswordHash is not null)
    {
        return Results.Redirect("/sett-passord?feil=allerede-satt");
    }
    if (passord.Length < 8 || passord != bekreft)
    {
        return Results.Redirect("/sett-passord?feil=ugyldig");
    }

    bruker.PasswordHash = PasswordHasher.Hash(passord);
    await db.SaveChangesAsync();

    return Results.Redirect("/login?satt=1");
}).AllowAnonymous();

app.MapPost("/account/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).RequireAuthorization();

app.MapGet("/bilder/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var bilde = await db.BefaringDorfeltBilder.FindAsync(id);
    return bilde is null ? Results.NotFound() : Results.File(bilde.Data, bilde.ContentType);
}).RequireAuthorization();

app.MapGet("/systemvedlegg/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var vedlegg = await db.SystemVedlegg.FindAsync(id);
    return vedlegg is null
        ? Results.NotFound()
        : Results.File(vedlegg.Data, vedlegg.ContentType, vedlegg.Filnavn);
}).RequireAuthorization();

app.MapGet("/plantegningbilde/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var plantegning = await db.Plantegninger.FindAsync(id);
    return plantegning is null
        ? Results.NotFound()
        : Results.File(plantegning.Data, plantegning.ContentType);
}).RequireAuthorization();

app.MapGet("/prosjektvedlegg/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var vedlegg = await db.ProsjektVedlegg.FindAsync(id);
    return vedlegg is null
        ? Results.NotFound()
        : Results.File(vedlegg.Data, vedlegg.ContentType, vedlegg.Filnavn);
}).RequireAuthorization();

app.MapGet("/koblingsbibliotek/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var symbol = await db.KoblingsSymbolBibliotek.FindAsync(id);
    return symbol is null
        ? Results.NotFound()
        : Results.File(symbol.BildeData, symbol.BildeContentType);
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
        : Results.File(bilde.Data, bilde.ContentType, bilde.Filnavn);
}).RequireAuthorization();

app.MapGet("/tilvalgalternativ/{id:int}/bilde", async (int id, ApplicationDbContext db) =>
{
    var alternativ = await db.TilvalgAlternativer.FindAsync(id);
    return alternativ?.BildeData is null
        ? Results.NotFound()
        : Results.File(alternativ.BildeData, alternativ.BildeContentType ?? "application/octet-stream");
}).RequireAuthorization();

app.MapGet("/tilvalgmalalternativ/{id:int}/bilde", async (int id, ApplicationDbContext db) =>
{
    var alternativ = await db.TilvalgMalAlternativer.FindAsync(id);
    return alternativ?.BildeData is null
        ? Results.NotFound()
        : Results.File(alternativ.BildeData, alternativ.BildeContentType ?? "application/octet-stream");
}).RequireAuthorization();

app.MapGet("/tilvalg/{id:int}/kundebilde", async (int id, ApplicationDbContext db) =>
{
    var tilvalg = await db.Tilvalg.FindAsync(id);
    return tilvalg?.KundeOnskeBildeData is null
        ? Results.NotFound()
        : Results.File(tilvalg.KundeOnskeBildeData, tilvalg.KundeOnskeBildeContentType ?? "application/octet-stream");
}).RequireAuthorization();

app.MapGet("/komponent/{id:int}/fdv", async (int id, ApplicationDbContext db) =>
{
    var komponent = await db.Components.FindAsync(id);
    return komponent?.FdvData is null
        ? Results.NotFound()
        : Results.File(komponent.FdvData, komponent.FdvContentType ?? "application/pdf", komponent.FdvFilnavn);
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
    return media is null ? Results.NotFound() : Results.File(media.Data, media.ContentType, media.Filnavn);
}).RequireAuthorization();

app.MapGet("/servicerundemedia/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var media = await db.ServicerundeMedia.FindAsync(id);
    return media is null ? Results.NotFound() : Results.File(media.Data, media.ContentType, media.Filnavn);
}).RequireAuthorization();

app.MapGet("/arbeidsordremedia/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var media = await db.ArbeidsordreMedia.FindAsync(id);
    return media is null ? Results.NotFound() : Results.File(media.Data, media.ContentType, media.Filnavn);
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

app.Run();

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Components;
using PortalItlock.Web.Data;
using PortalItlock.Web.Services;
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

    if (bruker is null || !passwordOk)
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

app.MapGet("/tilbud/{id:int}/pdf", async (int id, TilbudPdfService pdfService) =>
{
    var pdf = await pdfService.GenerateAsync(id);
    return pdf is null ? Results.NotFound() : Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/timeoversikt/eksport-csv", async (DateTime fra, DateTime til, int? montorId, TimeoversiktService service) =>
{
    var registreringer = await service.HentRegistreringerAsync(fra, til, montorId);
    var csv = service.GenererCsv(registreringer);
    var filnavn = $"timeoversikt-{fra:yyyy-MM-dd}-{til:yyyy-MM-dd}.csv";
    return Results.File(csv, "text/csv", filnavn);
}).RequireAuthorization();

app.MapGet("/timeoversikt/eksport-pdf", async (DateTime fra, DateTime til, int? montorId, ApplicationDbContext db, TimeoversiktService service) =>
{
    var registreringer = await service.HentRegistreringerAsync(fra, til, montorId);
    var montorNavn = montorId.HasValue
        ? (await db.Brukere.FindAsync(montorId.Value))?.Navn
        : null;
    var pdf = service.GenererPdf(registreringer, fra, til, montorNavn);
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.Run();

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Components;
using PortalItlock.Web.Data;
using PortalItlock.Web.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<PackageMatchingService>();

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

app.MapPost("/account/login", async (HttpContext http, IConfiguration config) =>
{
    var form = await http.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var safeReturnUrl = !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
        ? returnUrl
        : "/";

    var expectedUsername = config["Auth:Username"] ?? "";
    var expectedHash = config["Auth:PasswordHash"] ?? "";

    var usernameOk = string.Equals(username, expectedUsername, StringComparison.OrdinalIgnoreCase);
    var passwordOk = !string.IsNullOrEmpty(expectedHash) && PasswordHasher.Verify(password, expectedHash);

    if (!usernameOk || !passwordOk)
    {
        return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(safeReturnUrl)}&feil=1");
    }

    var claims = new List<Claim> { new(ClaimTypes.Name, username) };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    return Results.Redirect(safeReturnUrl);
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

app.Run();

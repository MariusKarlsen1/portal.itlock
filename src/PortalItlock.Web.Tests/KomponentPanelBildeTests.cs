using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using PortalItlock.Web.Components;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Tests;

// Varekortet: «Last opp bilde» med en PNG, og så Lagre - slik det gjøres i portalen.
public class KomponentPanelBildeTests
{
    private sealed class TestRenderer(IServiceProvider sp) : Renderer(sp, NullLoggerFactory.Instance)
    {
        public List<Exception> Feil { get; } = [];
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        protected override void HandleException(Exception exception) => Feil.Add(exception);
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
        protected override IComponent ResolveComponentForRenderMode(Type componentType, int? parentComponentId, IComponentActivator componentActivator, IComponentRenderMode renderMode)
            => componentActivator.CreateInstance(componentType);
        public Task<int> Legg(IComponent c) => Dispatcher.InvokeAsync(() => AssignRootComponentId(c));
        public Task Render(int id, ParameterView p) => Dispatcher.InvokeAsync(() => RenderRootComponentAsync(id, p));
    }

    private sealed class TomJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => ValueTask.FromResult(default(TValue)!);
    }

    private sealed class Fil(byte[] data) : IBrowserFile
    {
        public string Name => "bilde.png";
        public DateTimeOffset LastModified => DateTimeOffset.Now;
        public long Size => data.Length;
        public string ContentType => "image/png";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream(data);
    }

    private bool MedOmorg;
    private bool FullDisk;

    // Full disk på serveren: varekortet viser feilen i stedet for å stoppe siden.
    [Fact]
    public Task FullDisk_VisesPaVarekortet()
    {
        FullDisk = true;
        return LastOppPng_OgLagre_GirIngenFeil();
    }

    [Fact]
    public Task LastOppPng_UnderOmorganisering_GirIngenFeil()
    {
        MedOmorg = true;
        return LastOppPng_OgLagre_GirIngenFeil();
    }

    [Fact]
    public async Task LastOppPng_OgLagre_GirIngenFeil()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(conn));
        services.AddLogging();
        services.AddSingleton<IJSRuntime, TomJs>();
        services.AddSingleton<NavigationManager, TestNav>();
        await using var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        var c = new Component { Navn = "Dørmodul 5-serien offline", PrisNetto = 1000, BildeFilnavn = "bilde.png", BildeContentType = "image/png" };
        db.Components.Add(c);
        await db.SaveChangesAsync();
        if (MedOmorg)
        {
            foreach (var k in new[] { "FdvData", "MontasjebladData", "DatabladData", "BildeData" })
            {
                db.Database.ExecuteSqlRaw($"ALTER TABLE \"Components\" ADD COLUMN \"{k}__ny\" BLOB NULL;");
            }
            var sett = "\"FdvData__ny\" = NEW.\"FdvData\", \"MontasjebladData__ny\" = NEW.\"MontasjebladData\", \"DatabladData__ny\" = NEW.\"DatabladData\", \"BildeData__ny\" = NEW.\"BildeData\"";
            db.Database.ExecuteSqlRaw($"CREATE TRIGGER \"omorg_Components_u\" AFTER UPDATE OF \"FdvData\", \"MontasjebladData\", \"DatabladData\", \"BildeData\" ON \"Components\" BEGIN UPDATE \"Components\" SET {sett} WHERE \"Id\" = NEW.\"Id\"; END;");
            db.Database.ExecuteSqlRaw($"CREATE TRIGGER \"omorg_Components_i\" AFTER INSERT ON \"Components\" BEGIN UPDATE \"Components\" SET {sett} WHERE \"Id\" = NEW.\"Id\"; END;");
        }

        if (FullDisk)
        {
            db.Database.ExecuteSqlRaw("CREATE TRIGGER full_disk BEFORE UPDATE ON \"Components\" BEGIN SELECT RAISE(ABORT, 'database or disk is full'); END;");
        }

        var renderer = new TestRenderer(sp);
        var panel = new KomponentPanel();
        foreach (var prop in typeof(KomponentPanel).GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                     .Where(x => x.GetCustomAttribute<InjectAttribute>() is not null))
        {
            prop.SetValue(panel, sp.GetRequiredService(prop.PropertyType));
        }
        var id = await renderer.Legg(panel);
        await renderer.Render(id, ParameterView.FromDictionary(new Dictionary<string, object?> { ["ComponentId"] = (int?)c.Id }));

        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        async Task Kall(string metode, params object[] args)
        {
            var m = typeof(KomponentPanel).GetMethod(metode, BindingFlags.NonPublic | BindingFlags.Instance)!;
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                await (Task)m.Invoke(panel, args)!;
                typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(panel, null);
            });
        }

        await Kall("LastOppBilde", new InputFileChangeEventArgs([new Fil(png)]));
        await Kall("Lagre");

        Assert.Empty(renderer.Feil);
        if (FullDisk)
        {
            var feil = (string?)typeof(KomponentPanel).GetField("_filFeil", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(panel);
            Assert.Contains("full", feil);
            return;
        }
        db.ChangeTracker.Clear();
        var lagret = await db.Components.FirstAsync(x => x.Id == c.Id);
        await db.LastFilerAsync(lagret);
        Assert.NotNull(lagret.BildeData);
    }

    private sealed class TestNav : NavigationManager
    {
        public TestNav() => Initialize("http://localhost/", "http://localhost/komponenter");
        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }
}

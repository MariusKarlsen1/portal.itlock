using System.Data;
using Microsoft.Data.Sqlite;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

/// <summary>
/// Kopierer grunndata (dørpakker, kravdimensjoner, dørfunksjoner, forsider,
/// komponentregister, rabattgrupper, leverandører, produktgrupper) og hele
/// prosjekter med alt som hører til (tilbud, arbeidsordrer, dører, plantegninger
/// osv.) fra én organisasjon til en annen. Brukes fra plattform-admin.
///
/// Hver organisasjon har sin egen SQLite-fil. Kilden kobles på mottakerens
/// tilkobling (ATTACH), og radene kopieres med NYE Id-er i mottakeren.
/// Fremmednøkler oversettes underveis:
///  - til tabeller som kopieres: gammel Id → ny Id
///  - til brukere: samme e-post i mottakeren, ellers tomt (eller raden hoppes over)
///  - til kunder og komponenter: finnes de i mottakeren (navn/varenummer), brukes de;
///    ellers kopieres de med.
/// Grunndata som allerede finnes i mottakeren (samme navn) brukes i stedet for å
/// lage duplikater. Alt skjer i én transaksjon.
/// </summary>
public sealed class OrganisasjonKopiService(OrganisasjonBackupService backup, ILogger<OrganisasjonKopiService> logger)
{
    public sealed record Gruppe(string Nokkel, string Navn, string[] Tabeller);

    public static readonly Gruppe[] Grupper =
    [
        new("leverandorer", "Leverandører", ["Leverandorer"]),
        new("rabattgrupper", "Rabattgrupper", ["Rabattgrupper"]),
        new("produktgrupper", "Produktgrupper", ["Produktgrupper"]),
        new("komponenter", "Komponentregister", ["ComponentTypes", "Components", "ComponentLeverandorer", "ComponentProduktgrupper",
            "ComponentDatabladDokumenter", "ComponentFdvDokumenter", "ComponentMontasjebladDokumenter", "PlanlagtePrisendringer"]),
        new("dorfunksjoner", "Dørfunksjoner", ["DorFunksjoner"]),
        new("kravdimensjoner", "Kravdimensjoner", ["RequirementDimensions", "RequirementValues"]),
        new("dorpakker", "Dørpakker", ["Packages", "PackageComponents", "PackageRequirements", "PackageDorFunksjoner"]),
        new("forsider", "Forsider", ["TilbudForsider"]),
    ];

    // Naturlige nøkler: finnes en rad med samme verdier i mottakeren, brukes den.
    private static readonly Dictionary<string, string[]> Naturlig = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Leverandorer"] = ["Navn"],
        ["Rabattgrupper"] = ["Leverandor", "Kode"],
        ["Produktgrupper"] = ["Navn"],
        ["ComponentTypes"] = ["Navn"],
        ["Components"] = ["Navn", "Produktkode"],
        ["DorFunksjoner"] = ["Navn"],
        ["RequirementDimensions"] = ["Navn"],
        ["RequirementValues"] = ["RequirementDimensionId", "Navn"],
        ["Packages"] = ["Navn"],
        ["TilbudForsider"] = ["Navn"],
        ["Kunder"] = ["Navn"],
        ["Inntektskontoer"] = ["Navn"],
        ["PrisKoder"] = ["Kode"],
    };

    // Tabeller som hentes ved behov når en kopiert rad peker på dem.
    private static readonly HashSet<string> VedBehov = new(StringComparer.OrdinalIgnoreCase)
    {
        "Kunder", "Components", "ComponentTypes", "Rabattgrupper", "Leverandorer", "Produktgrupper", "Inntektskontoer", "PrisKoder"
    };

    // Kopieres aldri med et prosjekt (systemtabeller, logger og brukerdata).
    private static readonly HashSet<string> AldriMed = new(StringComparer.OrdinalIgnoreCase)
    {
        "Brukere", "DorKomponentLogger", "LagredePdfer", "TenantHendelser", "Varsler", "EpostVarsler"
    };

    public sealed record Resultat(bool Ok, string Melding, IReadOnlyList<(string Tabell, int Antall)> Tabeller);

    public static async Task<List<(int Id, string Navn, string? Kunde)>> HentProsjekterAsync(Tenant kilde)
    {
        await using var con = new SqliteConnection(kilde.ConnectionString);
        await con.OpenAsync();
        await using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT p.Id, p.Navn, k.Navn FROM Prosjekter p LEFT JOIN Kunder k ON k.Id = p.KundeId ORDER BY p.Navn";
        var liste = new List<(int, string, string?)>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            liste.Add((r.GetInt32(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2)));
        }
        return liste;
    }

    public async Task<Resultat> KopierAsync(Tenant kilde, Tenant mottaker, IReadOnlyCollection<string> grupper, IReadOnlyCollection<int> prosjektIder, string? av)
    {
        if (kilde.Id == mottaker.Id)
        {
            return new(false, "Kilde og mottaker er samme organisasjon.", []);
        }
        var kildeFil = new SqliteConnectionStringBuilder(kilde.ConnectionString).DataSource;
        if (!File.Exists(kildeFil))
        {
            return new(false, "Fant ikke databasen til kilden.", []);
        }

        // Sikkerhetskopi av mottakeren før noe endres.
        try
        {
            await backup.TaAsync(mottaker, BackupType.Manuell, av);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Kunne ikke ta backup før kopiering til {Tenant}", mottaker.Navn);
            return new(false, "Kunne ikke ta sikkerhetskopi av mottakeren først – ingenting er kopiert.", []);
        }

        return await KopierUtenBackupAsync(kilde.ConnectionString, mottaker.ConnectionString, kilde.Navn, grupper, prosjektIder, logger);
    }

    /// Selve kopieringen (uten sikkerhetskopi) – brukes også i testene.
    public static async Task<Resultat> KopierUtenBackupAsync(string kildeCs, string mottakerCs, string kildeNavn,
        IReadOnlyCollection<string> grupper, IReadOnlyCollection<int> prosjektIder, ILogger? logger = null)
    {
        var kildeFil = new SqliteConnectionStringBuilder(kildeCs).DataSource;
        await using var con = new SqliteConnection(mottakerCs);
        await con.OpenAsync();
        await Kjor(con, "PRAGMA foreign_keys = OFF;");
        await using (var att = con.CreateCommand())
        {
            att.CommandText = "ATTACH DATABASE $fil AS kilde;";
            att.Parameters.AddWithValue("$fil", kildeFil);
            await att.ExecuteNonQueryAsync();
        }

        var motor = new Motor(con);
        await using var tx = (SqliteTransaction)await con.BeginTransactionAsync();
        motor.Tx = tx;
        try
        {
            await motor.LesSkjema();
            await motor.LesBrukere();

            foreach (var g in Grupper.Where(g => grupper.Contains(g.Nokkel)))
            {
                foreach (var t in g.Tabeller)
                {
                    await motor.KopierTabell(t, null);
                }
            }

            if (prosjektIder.Count > 0)
            {
                await motor.KopierProsjekter(prosjektIder);
            }

            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            logger?.LogError(ex, "Kopiering fra {Kilde} feilet", kildeNavn);
            return new(false, $"Kopieringen feilet og er rullet tilbake: {ex.Message}", []);
        }
        finally
        {
            await Kjor(con, "DETACH DATABASE kilde;");
        }

        var tabeller = motor.Teller.Where(x => x.Value > 0).OrderBy(x => x.Key).Select(x => (x.Key, x.Value)).ToList();
        return new(true, $"Kopiert {tabeller.Sum(x => x.Value)} rader fra {kildeNavn}.", tabeller);
    }

    private static async Task Kjor(SqliteConnection con, string sql)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    // ------------------------------------------------------------------
    private sealed class Motor(SqliteConnection con)
    {
        public SqliteTransaction? Tx { get; set; }
        public Dictionary<string, int> Teller { get; } = new(StringComparer.OrdinalIgnoreCase);

        private sealed record Kolonne(string Navn, bool IkkeNull, int Pk);
        private sealed record Fk(string Tabell, string Fra);
        private sealed class Skjema
        {
            public List<Kolonne> Kolonner { get; } = [];
            public List<Fk> Fks { get; } = [];
            public bool IdPk => Kolonner.Count(k => k.Pk > 0) == 1 && Kolonner.Any(k => k.Pk > 0 && k.Navn == "Id");
            // 1:1-tabell der Id også er fremmednøkkel til foreldren.
            public Fk? IdFk => Fks.FirstOrDefault(f => f.Fra == "Id");
        }

        private readonly Dictionary<string, Skjema> _skjema = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<long, long>> _map = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<long>> _nye = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<long, long?> _brukerMap = [];

        private SqliteCommand Cmd(string sql)
        {
            var c = con.CreateCommand();
            c.Transaction = Tx;
            c.CommandText = sql;
            return c;
        }

        public async Task LesSkjema()
        {
            var tabeller = new List<string>();
            await using (var c = Cmd("SELECT name FROM main.sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite%' AND name NOT LIKE '__EF%'"))
            await using (var r = await c.ExecuteReaderAsync())
            {
                while (await r.ReadAsync()) tabeller.Add(r.GetString(0));
            }
            var kildeTabeller = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var c = Cmd("SELECT name FROM kilde.sqlite_master WHERE type='table'"))
            await using (var r = await c.ExecuteReaderAsync())
            {
                while (await r.ReadAsync()) kildeTabeller.Add(r.GetString(0));
            }
            foreach (var t in tabeller.Where(kildeTabeller.Contains))
            {
                var s = new Skjema();
                var kildeKol = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                await using (var c = Cmd($"PRAGMA kilde.table_info(\"{t}\")"))
                await using (var r = await c.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync()) kildeKol.Add(r.GetString(1));
                }
                await using (var c = Cmd($"PRAGMA main.table_info(\"{t}\")"))
                await using (var r = await c.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        var navn = r.GetString(1);
                        if (kildeKol.Contains(navn))
                        {
                            s.Kolonner.Add(new Kolonne(navn, r.GetInt32(3) == 1 && r.IsDBNull(4), r.GetInt32(5)));
                        }
                    }
                }
                await using (var c = Cmd($"PRAGMA main.foreign_key_list(\"{t}\")"))
                await using (var r = await c.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync()) s.Fks.Add(new Fk(r.GetString(2), r.GetString(3)));
                }
                _skjema[t] = s;
            }

            // Foreldre før barn, så f.eks. Tilbud kopieres før Arbeidsordre som peker på det.
            var besokt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Besok(string t, HashSet<string> sti)
            {
                if (besokt.Contains(t) || !_skjema.ContainsKey(t) || !sti.Add(t)) return;
                foreach (var fk in _skjema[t].Fks.Where(f => !f.Tabell.Equals(t, StringComparison.OrdinalIgnoreCase)))
                {
                    Besok(fk.Tabell, sti);
                }
                sti.Remove(t);
                if (besokt.Add(t)) _rekkefolge.Add(t);
            }
            foreach (var t in _skjema.Keys.OrderBy(x => x)) Besok(t, []);
        }

        private readonly List<string> _rekkefolge = [];

        public async Task LesBrukere()
        {
            var mal = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            await using (var c = Cmd("SELECT Id, Epost FROM main.Brukere"))
            await using (var r = await c.ExecuteReaderAsync())
            {
                while (await r.ReadAsync()) if (!r.IsDBNull(1)) mal[r.GetString(1).Trim()] = r.GetInt64(0);
            }
            await using (var c = Cmd("SELECT Id, Epost FROM kilde.Brukere"))
            await using (var r = await c.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    _brukerMap[r.GetInt64(0)] = !r.IsDBNull(1) && mal.TryGetValue(r.GetString(1).Trim(), out var id) ? id : null;
                }
            }
        }

        private Dictionary<long, long> Map(string t) => _map.TryGetValue(t, out var m) ? m : _map[t] = [];
        private HashSet<long> Nye(string t) => _nye.TryGetValue(t, out var m) ? m : _nye[t] = [];

        /// Kopierer rader fra kilde.tabell (alle, eller de med Id i utvalget).
        public async Task KopierTabell(string tabell, IReadOnlyCollection<long>? ider, string? hvor = null)
        {
            if (!_skjema.TryGetValue(tabell, out var s)) return;
            var sql = $"SELECT {string.Join(", ", s.Kolonner.Select(k => $"\"{k.Navn}\""))} FROM kilde.\"{tabell}\"";
            if (hvor is not null) sql += " WHERE " + hvor;
            else if (ider is not null)
            {
                if (ider.Count == 0) return;
                sql += $" WHERE Id IN ({string.Join(",", ider)})";
            }
            var rader = new List<object?[]>();
            await using (var c = Cmd(sql))
            await using (var r = await c.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    var rad = new object?[s.Kolonner.Count];
                    for (var i = 0; i < rad.Length; i++) rad[i] = r.IsDBNull(i) ? null : r.GetValue(i);
                    rader.Add(rad);
                }
            }
            // Selvrefererende rader (f.eks. Tilbud.OpprinneligTilbudId): eldste først.
            foreach (var rad in rader)
            {
                await KopierRad(tabell, s, rad);
            }
        }

        private async Task<long?> KopierRad(string tabell, Skjema s, object?[] rad)
        {
            var idIndeks = s.Kolonner.FindIndex(k => k.Navn == "Id");
            long? gammelId = idIndeks >= 0 && rad[idIndeks] is { } v ? Convert.ToInt64(v) : null;
            if (s.IdPk && gammelId is { } g0 && Map(tabell).TryGetValue(g0, out var allerede))
            {
                return allerede;
            }

            // Oversett fremmednøkler.
            var verdier = (object?[])rad.Clone();
            var harNyForelder = false;
            foreach (var fk in s.Fks)
            {
                var i = s.Kolonner.FindIndex(k => k.Navn == fk.Fra);
                if (i < 0 || verdier[i] is null) continue;
                var gammel = Convert.ToInt64(verdier[i]);
                long? ny = await OversettFk(fk.Tabell, gammel);
                if (ny is null)
                {
                    if (s.Kolonner[i].IkkeNull || fk.Fra == "Id") return null; // kan ikke kobles – hopp over raden
                    verdier[i] = null;
                }
                else
                {
                    verdier[i] = ny;
                    if (Nye(fk.Tabell).Contains(ny.Value)) harNyForelder = true;
                }
            }

            // Grunndata som finnes fra før: bruk den.
            if (s.IdPk && s.IdFk is null && Naturlig.TryGetValue(tabell, out var nokkel) && gammelId is { } g1)
            {
                var finnes = await FinnNaturlig(tabell, s, nokkel, verdier);
                if (finnes is { } f)
                {
                    Map(tabell)[g1] = f;
                    return f;
                }
            }

            // Kobling-/undertabeller uten egen Id: bare når en forelder er ny.
            if (!s.IdPk && !harNyForelder) return null;
            if (s.IdFk is not null && !harNyForelder) return null;

            var kolonner = s.Kolonner.Select((k, i) => (k, i)).Where(x => !(s.IdPk && s.IdFk is null && x.k.Navn == "Id")).ToList();
            await using (var c = Cmd($"INSERT OR IGNORE INTO main.\"{tabell}\" ({string.Join(", ", kolonner.Select(x => $"\"{x.k.Navn}\""))}) VALUES ({string.Join(", ", kolonner.Select(x => $"$p{x.i}"))})"))
            {
                foreach (var (_, i) in kolonner) c.Parameters.AddWithValue($"$p{i}", verdier[i] ?? DBNull.Value);
                var antall = await c.ExecuteNonQueryAsync();
                if (antall == 0) return null;
            }
            Teller[tabell] = Teller.GetValueOrDefault(tabell) + 1;
            if (!s.IdPk) return null;
            long nyId;
            if (s.IdFk is not null)
            {
                nyId = Convert.ToInt64(verdier[idIndeks]);
            }
            else
            {
                await using var c2 = Cmd("SELECT last_insert_rowid()");
                nyId = (long)(await c2.ExecuteScalarAsync())!;
            }
            if (gammelId is { } g2) Map(tabell)[g2] = nyId;
            Nye(tabell).Add(nyId);
            return nyId;
        }

        private async Task<long?> OversettFk(string tabell, long gammel)
        {
            if (tabell.Equals("Brukere", StringComparison.OrdinalIgnoreCase))
            {
                return _brukerMap.TryGetValue(gammel, out var b) ? b : null;
            }
            if (Map(tabell).TryGetValue(gammel, out var ny)) return ny;
            if (VedBehov.Contains(tabell))
            {
                await KopierTabell(tabell, [gammel]);
                return Map(tabell).TryGetValue(gammel, out var ny2) ? ny2 : null;
            }
            return null;
        }

        private async Task<long?> FinnNaturlig(string tabell, Skjema s, string[] nokkel, object?[] verdier)
        {
            var deler = new List<string>();
            await using var c = Cmd("");
            var n = 0;
            foreach (var k in nokkel)
            {
                var i = s.Kolonner.FindIndex(x => x.Navn == k);
                if (i < 0) return null;
                if (verdier[i] is null)
                {
                    deler.Add($"\"{k}\" IS NULL");
                }
                else
                {
                    deler.Add($"\"{k}\" = $n{n}");
                    c.Parameters.AddWithValue($"$n{n}", verdier[i]);
                    n++;
                }
            }
            c.CommandText = $"SELECT Id FROM main.\"{tabell}\" WHERE {string.Join(" AND ", deler)} LIMIT 1";
            var svar = await c.ExecuteScalarAsync();
            return svar is null or DBNull ? null : Convert.ToInt64(svar);
        }

        /// Prosjektene og alt som henger på dem (rekursivt via fremmednøkler).
        public async Task KopierProsjekter(IReadOnlyCollection<int> prosjektIder)
        {
            var hentet = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Prosjekter"] = prosjektIder.Select(x => (long)x).ToHashSet()
            };
            await KopierTabell("Prosjekter", hentet["Prosjekter"]);

            var ny = true;
            var runder = 0;
            while (ny && runder++ < 12)
            {
                ny = false;
                foreach (var t in _rekkefolge)
                {
                    var s = _skjema[t];
                    if (AldriMed.Contains(t) || VedBehov.Contains(t) || t.Equals("Prosjekter", StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (var fk in s.Fks.Where(f => hentet.ContainsKey(f.Tabell)))
                    {
                        var foreldre = hentet[fk.Tabell];
                        if (foreldre.Count == 0) continue;
                        var hvor = $"\"{fk.Fra}\" IN ({string.Join(",", foreldre)})";
                        // Finn kilde-Id-ene som ikke er hentet ennå.
                        if (s.IdPk)
                        {
                            var ider = new List<long>();
                            await using (var c = Cmd($"SELECT Id FROM kilde.\"{t}\" WHERE {hvor}"))
                            await using (var r = await c.ExecuteReaderAsync())
                            {
                                while (await r.ReadAsync()) ider.Add(r.GetInt64(0));
                            }
                            var allerede = hentet.TryGetValue(t, out var h) ? h : hentet[t] = [];
                            var nye = ider.Where(i => !allerede.Contains(i)).ToList();
                            if (nye.Count == 0) continue;
                            foreach (var i in nye) allerede.Add(i);
                            await KopierTabell(t, nye);
                            ny = true;
                        }
                        else
                        {
                            var nokkel = $"{t}:{fk.Fra}:{string.Join(",", foreldre)}";
                            if (!_ferdigeKoblinger.Add(nokkel)) continue;
                            await KopierTabell(t, null, hvor);
                            ny = true;
                        }
                    }
                }
            }
        }

        private readonly HashSet<string> _ferdigeKoblinger = [];
    }
}

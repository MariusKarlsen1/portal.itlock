using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public record AiMelding(string Rolle, string Tekst);

// En foreslått skrivehandling AI-assistenten vil utføre, men som ikke er kjørt enda -
// brukeren må trykke "Bekreft" i chatten (se AiAssistent.razor) før UtforHandlingAsync kalles.
public record AiForeslattHandling(string VerktoyNavn, JsonNode? Input, string Beskrivelse);

public record AiSvar(string? Tekst, AiForeslattHandling? ForeslattHandling);

// Assistent mot portalens data (prosjekter, dører, tickets, komponentregister). Kaller Anthropics
// Messages API med søk/oppslag-verktøy (kjøres automatisk) og et lite sett skrivehandlinger
// (opprette prosjekt/ticket, endre komponenter) - skrivehandlinger kjøres ALDRI automatisk, de
// returneres som et forslag brukeren må bekrefte eksplisitt før UtforHandlingAsync faktisk lagrer noe.
public class AiAssistentService(HttpClient http, IConfiguration config, ApplicationDbContext db)
{
    private static readonly HashSet<string> SkrivehandlingNavn = ["opprett_prosjekt", "opprett_ticket", "oppdater_komponent"];

    private const string SystemPrompt =
        "Du er AI-assistenten i portal.itlock, et internt driftssystem for itlock AS (dør/lås-montering). " +
        "Du hjelper ansatte med å finne svar i portalens data - prosjekter, dører, tickets - og kan foreslå " +
        "noen konkrete endringer: opprette prosjekt, opprette ticket, og endre komponenter i vareregisteret " +
        "(aktivere/deaktivere, endre priser, garantitid, navn m.m.). Bruk søkeverktøyene til å slå opp faktisk " +
        "data før du svarer eller foreslår en endring, ikke gjett - f.eks. slå opp riktig komponentId før du " +
        "kaller oppdater_komponent. Svar kort og konkret på norsk. " +
        "Viktig om skrivehandlinger: når brukeren ber om en konkret, entydig endring (f.eks. \"opprett en ticket " +
        "med tittel X\"), skal du kalle verktøyet DIREKTE i samme svar - ikke spør om lov i vanlig tekst først. " +
        "Appen viser automatisk et eget bekreftelseskort til brukeren når du kaller en skrivehandling, så det " +
        "trengs ingen ekstra muntlig bekreftelsesrunde fra deg. Spør kun oppklarende spørsmål i tekst dersom du " +
        "faktisk mangler informasjon du trenger for å fylle ut verktøyet riktig (f.eks. hvilket prosjekt).";

    private const int MaksRunder = 6;

    public async Task<AiSvar> SvarAsync(IReadOnlyList<AiMelding> historikk, string nyttSporsmal, CancellationToken ct = default)
    {
        var apiKey = config["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiSvar("AI-assistenten er ikke satt opp enda - mangler API-nøkkel. Legg inn \"Anthropic:ApiKey\" i konfigurasjonen (miljøvariabel ANTHROPIC__APIKEY).", null);
        }

        var model = config["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";

        var meldinger = new JsonArray();
        foreach (var m in historikk)
        {
            meldinger.Add(new JsonObject { ["role"] = m.Rolle, ["content"] = m.Tekst });
        }
        meldinger.Add(new JsonObject { ["role"] = "user", ["content"] = nyttSporsmal });

        for (var runde = 0; runde < MaksRunder; runde++)
        {
            var request = new JsonObject
            {
                ["model"] = model,
                ["max_tokens"] = 1500,
                ["system"] = SystemPrompt,
                ["tools"] = Verktoy(),
                ["messages"] = JsonNode.Parse(meldinger.ToJsonString())
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
            httpRequest.Headers.Add("x-api-key", apiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");
            httpRequest.Content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json");

            using var response = await http.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                return new AiSvar($"AI-kallet feilet ({(int)response.StatusCode}). Sjekk at API-nøkkelen er gyldig. Detaljer: {responseBody}", null);
            }

            var svarJson = JsonNode.Parse(responseBody);
            var stopReason = svarJson?["stop_reason"]?.GetValue<string>();
            var contentBlokker = svarJson?["content"]?.AsArray() ?? [];

            meldinger.Add(new JsonObject { ["role"] = "assistant", ["content"] = JsonNode.Parse(contentBlokker.ToJsonString()) });

            var tekstSoLangt = string.Concat(contentBlokker
                .Where(b => b?["type"]?.GetValue<string>() == "text")
                .Select(b => b!["text"]!.GetValue<string>()));

            if (stopReason != "tool_use")
            {
                return new AiSvar(string.IsNullOrWhiteSpace(tekstSoLangt) ? "(Fikk ikke noe svar fra AI-assistenten.)" : tekstSoLangt, null);
            }

            var skrivehandlingBlokk = contentBlokker.FirstOrDefault(b =>
                b?["type"]?.GetValue<string>() == "tool_use" && SkrivehandlingNavn.Contains(b["name"]!.GetValue<string>()));

            if (skrivehandlingBlokk is not null)
            {
                var verktoyNavn = skrivehandlingBlokk["name"]!.GetValue<string>();
                var input = skrivehandlingBlokk["input"];
                var beskrivelse = await BeskrivHandling(verktoyNavn, input, ct);
                return new AiSvar(
                    string.IsNullOrWhiteSpace(tekstSoLangt) ? null : tekstSoLangt,
                    new AiForeslattHandling(verktoyNavn, JsonNode.Parse((input ?? new JsonObject()).ToJsonString()), beskrivelse));
            }

            var resultater = new JsonArray();
            foreach (var blokk in contentBlokker)
            {
                if (blokk?["type"]?.GetValue<string>() != "tool_use")
                {
                    continue;
                }

                var verktoyNavn = blokk["name"]!.GetValue<string>();
                var verktoyId = blokk["id"]!.GetValue<string>();
                var resultatJson = await KjorVerktoy(verktoyNavn, blokk["input"], ct);
                resultater.Add(new JsonObject { ["type"] = "tool_result", ["tool_use_id"] = verktoyId, ["content"] = resultatJson });
            }
            meldinger.Add(new JsonObject { ["role"] = "user", ["content"] = resultater });
        }

        return new AiSvar("Klarte ikke å komme frem til et svar innen antall forsøk. Prøv å omformulere spørsmålet.", null);
    }

    private async Task<string> KjorVerktoy(string navn, JsonNode? input, CancellationToken ct)
    {
        try
        {
            return navn switch
            {
                "sok_prosjekter" => await SokProsjekter(input?["sok"]?.GetValue<string>() ?? "", ct),
                "hent_prosjekt_detaljer" => await HentProsjektDetaljer(input?["prosjektId"]?.GetValue<int>() ?? 0, ct),
                "sok_dorer" => await SokDorer(input?["prosjektId"]?.GetValue<int?>(), input?["sok"]?.GetValue<string>(), ct),
                "sok_tickets" => await SokTickets(input?["status"]?.GetValue<string>(), input?["sok"]?.GetValue<string>(), ct),
                "hent_ticket_detaljer" => await HentTicketDetaljer(input?["ticketId"]?.GetValue<int>() ?? 0, ct),
                "hent_portal_statistikk" => await HentStatistikk(ct),
                "sok_komponenter" => await SokKomponenter(input?["sok"]?.GetValue<string>() ?? "", ct),
                _ => JsonSerializer.Serialize(new { feil = $"Ukjent verktøy: {navn}" })
            };
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { feil = ex.Message });
        }
    }

    private async Task<string> SokProsjekter(string sok, CancellationToken ct)
    {
        var q = db.Prosjekter.AsNoTracking().Include(p => p.Kunde).Include(p => p.Dorer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(sok))
        {
            q = q.Where(p => p.Navn.Contains(sok)
                || (p.Kunde != null && p.Kunde.Navn.Contains(sok))
                || (p.Adresse != null && p.Adresse.Contains(sok)));
        }

        var treff = await q.OrderByDescending(p => p.OpprettetDato).Take(15).Select(p => new
        {
            p.Id,
            p.Navn,
            Kunde = p.Kunde != null ? p.Kunde.Navn : null,
            p.Adresse,
            Status = p.Status != null ? p.Status.Value.Visningsnavn() : null,
            AntallDorer = p.Dorer.Count,
            AntallFerdigMontert = p.Dorer.Count(d => d.Status == MontasjeStatus.FerdigMontert)
        }).ToListAsync(ct);

        return JsonSerializer.Serialize(treff);
    }

    private async Task<string> HentProsjektDetaljer(int prosjektId, CancellationToken ct)
    {
        var p = await db.Prosjekter.AsNoTracking()
            .Include(x => x.Kunde)
            .Include(x => x.Dorer)
            .FirstOrDefaultAsync(x => x.Id == prosjektId, ct);

        if (p is null)
        {
            return JsonSerializer.Serialize(new { feil = "Fant ikke prosjektet." });
        }

        var tickets = await db.Tickets.AsNoTracking()
            .Where(t => t.ProsjektId == prosjektId)
            .Select(t => new { t.Id, t.Tittel, Status = t.Status.Visningsnavn() })
            .ToListAsync(ct);

        return JsonSerializer.Serialize(new
        {
            p.Id,
            p.Navn,
            Kunde = p.Kunde?.Navn,
            p.Adresse,
            p.Postnr,
            p.Sted,
            Status = p.Status?.Visningsnavn(),
            p.OverlevertDato,
            Dorer = p.Dorer.Select(d => new { d.Id, d.Dornummer, Status = d.Status.Visningsnavn(), d.Etasje, d.Romnr }),
            Tickets = tickets
        });
    }

    private async Task<string> SokDorer(int? prosjektId, string? sok, CancellationToken ct)
    {
        var q = db.Dorer.AsNoTracking().Include(d => d.Prosjekt).AsQueryable();
        if (prosjektId.HasValue)
        {
            q = q.Where(d => d.ProsjektId == prosjektId.Value);
        }
        if (!string.IsNullOrWhiteSpace(sok))
        {
            q = q.Where(d => d.Dornummer.Contains(sok) || (d.Romnr != null && d.Romnr.Contains(sok)));
        }

        var treff = await q.OrderBy(d => d.Dornummer).Take(30).Select(d => new
        {
            d.Id,
            d.Dornummer,
            Prosjekt = d.Prosjekt!.Navn,
            d.ProsjektId,
            Status = d.Status.Visningsnavn(),
            d.Etasje,
            d.Romnr
        }).ToListAsync(ct);

        return JsonSerializer.Serialize(treff);
    }

    private async Task<string> SokTickets(string? statusTekst, string? sok, CancellationToken ct)
    {
        var q = db.Tickets.AsNoTracking().Include(t => t.Kunde).AsQueryable();
        if (!string.IsNullOrWhiteSpace(statusTekst) && Enum.TryParse<TicketStatus>(statusTekst, true, out var status))
        {
            q = q.Where(t => t.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(sok))
        {
            q = q.Where(t => t.Tittel.Contains(sok) || (t.Kunde != null && t.Kunde.Navn.Contains(sok)));
        }

        var treff = await q.OrderByDescending(t => t.OpprettetDato).Take(20).Select(t => new
        {
            t.Id,
            t.Tittel,
            Status = t.Status.Visningsnavn(),
            Prioritet = t.Prioritet.Visningsnavn(),
            Kunde = t.Kunde != null ? t.Kunde.Navn : null,
            t.OpprettetDato
        }).ToListAsync(ct);

        return JsonSerializer.Serialize(treff);
    }

    private async Task<string> HentTicketDetaljer(int ticketId, CancellationToken ct)
    {
        var t = await db.Tickets.AsNoTracking()
            .Include(x => x.Kunde)
            .Include(x => x.AnsvarligBruker)
            .Include(x => x.Dor)
            .FirstOrDefaultAsync(x => x.Id == ticketId, ct);

        if (t is null)
        {
            return JsonSerializer.Serialize(new { feil = "Fant ikke ticketen." });
        }

        return JsonSerializer.Serialize(new
        {
            t.Id,
            t.Tittel,
            t.Beskrivelse,
            Status = t.Status.Visningsnavn(),
            Prioritet = t.Prioritet.Visningsnavn(),
            Kunde = t.Kunde?.Navn,
            Ansvarlig = t.AnsvarligBruker?.Navn,
            Dor = t.Dor?.Dornummer,
            t.ErReklamasjon,
            t.OpprettetDato,
            t.SlaFrist
        });
    }

    private async Task<string> HentStatistikk(CancellationToken ct)
    {
        var aktiveProsjekter = await db.Prosjekter.CountAsync(p =>
            p.Status == ProsjektStatus.Aktiv || p.Status == ProsjektStatus.Registrert, ct);
        var apneTickets = await db.Tickets.CountAsync(t => t.Status != TicketStatus.Lukket, ct);
        var dorerIkkeMontert = await db.Dorer.CountAsync(d => d.Status == MontasjeStatus.IkkeStartet, ct);
        var dorerFerdig = await db.Dorer.CountAsync(d => d.Status == MontasjeStatus.FerdigMontert, ct);

        return JsonSerializer.Serialize(new
        {
            AktiveProsjekter = aktiveProsjekter,
            ApneTickets = apneTickets,
            DorerIkkeStartet = dorerIkkeMontert,
            DorerFerdigMontert = dorerFerdig
        });
    }

    private async Task<string> SokKomponenter(string sok, CancellationToken ct)
    {
        var q = db.Components.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(sok))
        {
            q = q.Where(c => c.Navn.Contains(sok)
                || (c.Produsent != null && c.Produsent.Contains(sok))
                || (c.Produktkode != null && c.Produktkode.Contains(sok)));
        }

        var treff = await q.OrderBy(c => c.Navn).Take(15).Select(c => new
        {
            c.Id,
            c.Navn,
            c.Produsent,
            c.Leverandor,
            c.Produktkode,
            c.PrisNetto,
            c.PrisVeiledende,
            c.GarantitidManeder,
            c.Aktiv
        }).ToListAsync(ct);

        return JsonSerializer.Serialize(treff);
    }

    // Lager en lesbar norsk beskrivelse av en foreslått skrivehandling, til bekreft-kortet i chatten.
    // Slår opp eksisterende verdier der det er relevant (f.eks. "fra 249 kr til 199 kr").
    private async Task<string> BeskrivHandling(string verktoyNavn, JsonNode? input, CancellationToken ct)
    {
        switch (verktoyNavn)
        {
            case "opprett_prosjekt":
            {
                var navn = input?["navn"]?.GetValue<string>() ?? "(uten navn)";
                var adresse = input?["adresse"]?.GetValue<string>();
                return string.IsNullOrWhiteSpace(adresse)
                    ? $"Opprette nytt prosjekt «{navn}»."
                    : $"Opprette nytt prosjekt «{navn}» ({adresse}).";
            }
            case "opprett_ticket":
            {
                var tittel = input?["tittel"]?.GetValue<string>() ?? "(uten tittel)";
                var prioritet = input?["prioritet"]?.GetValue<string>();
                return string.IsNullOrWhiteSpace(prioritet)
                    ? $"Opprette ny ticket «{tittel}»."
                    : $"Opprette ny ticket «{tittel}» med prioritet {prioritet}.";
            }
            case "oppdater_komponent":
            {
                var komponentId = input?["komponentId"]?.GetValue<int>() ?? 0;
                var entity = await db.Components.AsNoTracking().FirstOrDefaultAsync(c => c.Id == komponentId, ct);
                if (entity is null)
                {
                    return $"Fant ikke komponent med Id {komponentId} - forslaget kan ikke gjennomføres.";
                }

                var endringer = new List<string>();
                if (input?["navn"] is { } navnNode) endringer.Add($"navn til «{navnNode.GetValue<string>()}»");
                if (input?["produsent"] is { } produsentNode) endringer.Add($"produsent til «{produsentNode.GetValue<string>()}»");
                if (input?["leverandor"] is { } leverandorNode) endringer.Add($"leverandør til «{leverandorNode.GetValue<string>()}»");
                if (input?["produktkode"] is { } produktkodeNode) endringer.Add($"produktkode til «{produktkodeNode.GetValue<string>()}»");
                if (input?["prisNetto"] is { } prisNettoNode) endringer.Add($"nettopris fra {entity.PrisNetto:N0} kr til {prisNettoNode.GetValue<decimal>():N0} kr");
                if (input?["prisVeiledende"] is { } prisVeilNode) endringer.Add($"veiledende pris fra {entity.PrisVeiledende:N0} kr til {prisVeilNode.GetValue<decimal>():N0} kr");
                if (input?["garantitidManeder"] is { } garantiNode) endringer.Add($"garantitid til {garantiNode.GetValue<int>()} mnd");
                if (input?["aktiv"] is { } aktivNode) endringer.Add(aktivNode.GetValue<bool>() ? "aktivere varen" : "deaktivere varen");

                var endringTekst = endringer.Count == 0 ? "ingen faktiske endringer" : string.Join(", ", endringer);
                return $"Endre «{entity.Navn}»: {endringTekst}.";
            }
            default:
                return $"Utføre {verktoyNavn}.";
        }
    }

    // Selve utførelsen av en skrivehandling - kalles KUN fra AiAssistent.razor sin "Bekreft"-knapp,
    // aldri automatisk fra AI-loopen i SvarAsync.
    public async Task<string> UtforHandlingAsync(string verktoyNavn, JsonNode? input, CancellationToken ct = default)
    {
        try
        {
            return verktoyNavn switch
            {
                "opprett_prosjekt" => await OpprettProsjekt(input, ct),
                "opprett_ticket" => await OpprettTicket(input, ct),
                "oppdater_komponent" => await OppdaterKomponent(input, ct),
                _ => $"Ukjent handling: {verktoyNavn}"
            };
        }
        catch (Exception ex)
        {
            return $"Feil under utføring: {ex.Message}";
        }
    }

    private async Task<string> OpprettProsjekt(JsonNode? input, CancellationToken ct)
    {
        var navn = input?["navn"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(navn))
        {
            return "Mangler prosjektnavn - fikk ikke opprettet.";
        }

        var prosjekt = new Prosjekt
        {
            Navn = navn.Trim(),
            Adresse = input?["adresse"]?.GetValue<string>()
        };
        db.Prosjekter.Add(prosjekt);
        await db.SaveChangesAsync(ct);

        return $"✅ Opprettet prosjekt «{prosjekt.Navn}» (Id {prosjekt.Id}).";
    }

    private async Task<string> OpprettTicket(JsonNode? input, CancellationToken ct)
    {
        var tittel = input?["tittel"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(tittel))
        {
            return "Mangler tittel - fikk ikke opprettet ticket.";
        }

        var prioritet = TicketPrioritet.Normal;
        var prioritetTekst = input?["prioritet"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(prioritetTekst))
        {
            Enum.TryParse(prioritetTekst, true, out prioritet);
        }

        var ticket = new Ticket
        {
            Tittel = tittel.Trim(),
            Beskrivelse = input?["beskrivelse"]?.GetValue<string>(),
            Prioritet = prioritet,
            SlaFrist = TicketSlaHelper.BeregnFrist(prioritet, DateTime.Now)
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);

        return $"✅ Opprettet ticket «{ticket.Tittel}» (#{ticket.Id}).";
    }

    private async Task<string> OppdaterKomponent(JsonNode? input, CancellationToken ct)
    {
        var komponentId = input?["komponentId"]?.GetValue<int>() ?? 0;
        var entity = await db.Components.FirstOrDefaultAsync(c => c.Id == komponentId, ct);
        if (entity is null)
        {
            return $"Fant ikke komponent med Id {komponentId}.";
        }

        var nyNetto = input?["prisNetto"] is { } pn ? pn.GetValue<decimal>() : entity.PrisNetto;
        var nyVeil = input?["prisVeiledende"] is { } pv ? pv.GetValue<decimal>() : entity.PrisVeiledende;
        if (nyNetto != entity.PrisNetto || nyVeil != entity.PrisVeiledende)
        {
            PrisHistorikkLogger.Logg(db, entity, nyNetto, nyVeil, "AI-assistent");
        }

        if (input?["navn"] is { } navnNode) entity.Navn = navnNode.GetValue<string>();
        if (input?["produsent"] is { } produsentNode) entity.Produsent = produsentNode.GetValue<string>();
        if (input?["leverandor"] is { } leverandorNode) entity.Leverandor = leverandorNode.GetValue<string>();
        if (input?["produktkode"] is { } produktkodeNode) entity.Produktkode = produktkodeNode.GetValue<string>();
        if (input?["garantitidManeder"] is { } garantiNode) entity.GarantitidManeder = garantiNode.GetValue<int>();
        if (input?["aktiv"] is { } aktivNode) entity.Aktiv = aktivNode.GetValue<bool>();
        entity.PrisNetto = nyNetto;
        entity.PrisVeiledende = nyVeil;

        await db.SaveChangesAsync(ct);

        return $"✅ Oppdaterte «{entity.Navn}».";
    }

    private static JsonArray Verktoy() =>
    [
        ToolDef("sok_prosjekter", "Søk etter prosjekter på navn, kundenavn eller adresse. Gir en kort liste med treff.",
            Objekt(("sok", "string", "Søketekst, f.eks. kundenavn eller adressedel", true))),
        ToolDef("hent_prosjekt_detaljer", "Hent alle detaljer om ett prosjekt: dører, status, tickets knyttet til det.",
            Objekt(("prosjektId", "integer", "Prosjektets Id", true))),
        ToolDef("sok_dorer", "Søk etter dører på dørnummer eller romnummer, evt. avgrenset til ett prosjekt.",
            Objekt(("prosjektId", "integer", "Valgfritt: avgrens til dette prosjektet", false),
                   ("sok", "string", "Valgfritt: søketekst på dørnummer/romnr", false))),
        ToolDef("sok_tickets", "Søk etter tickets/saker, evt. filtrert på status (Ny, UnderBehandling, VenterGodkjenning, Lukket).",
            Objekt(("status", "string", "Valgfritt: en av Ny, UnderBehandling, VenterGodkjenning, Lukket", false),
                   ("sok", "string", "Valgfritt: søketekst på tittel/kundenavn", false))),
        ToolDef("hent_ticket_detaljer", "Hent alle detaljer om én ticket.",
            Objekt(("ticketId", "integer", "Ticketens Id", true))),
        ToolDef("hent_portal_statistikk", "Hent overordnede nøkkeltall: aktive prosjekter, åpne tickets, dører som gjenstår.",
            new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() }),
        ToolDef("sok_komponenter", "Søk etter komponenter/varer i vareregisteret på navn, produsent eller produktkode.",
            Objekt(("sok", "string", "Søketekst", true))),
        ToolDef("opprett_prosjekt", "Foreslå å opprette et nytt prosjekt. Krever bekreftelse fra brukeren før det faktisk opprettes.",
            Objekt(("navn", "string", "Prosjektnavn", true),
                   ("adresse", "string", "Valgfritt: adresse", false))),
        ToolDef("opprett_ticket", "Foreslå å opprette en ny ticket/sak. Krever bekreftelse fra brukeren før den faktisk opprettes.",
            Objekt(("tittel", "string", "Tittel på ticketen", true),
                   ("beskrivelse", "string", "Valgfritt: beskrivelse", false),
                   ("prioritet", "string", "Valgfritt: en av Lav, Normal, Hoy, Kritisk", false))),
        ToolDef("oppdater_komponent", "Foreslå å endre en komponent/vare i vareregisteret - aktiver/deaktiver, endre pris, garantitid, navn, produsent, leverandør eller produktkode. Slå opp komponentId med sok_komponenter først. Krever bekreftelse fra brukeren før det faktisk lagres.",
            Objekt(("komponentId", "integer", "Komponentens Id (finn med sok_komponenter)", true),
                   ("navn", "string", "Valgfritt: nytt varenavn", false),
                   ("produsent", "string", "Valgfritt: ny produsent", false),
                   ("leverandor", "string", "Valgfritt: ny leverandør", false),
                   ("produktkode", "string", "Valgfritt: ny produktkode", false),
                   ("prisNetto", "number", "Valgfritt: ny nettopris", false),
                   ("prisVeiledende", "number", "Valgfritt: ny veiledende pris", false),
                   ("garantitidManeder", "integer", "Valgfritt: ny garantitid i måneder", false),
                   ("aktiv", "boolean", "Valgfritt: sett komponenten aktiv/inaktiv", false)))
    ];

    private static JsonObject ToolDef(string navn, string beskrivelse, JsonObject schema) => new()
    {
        ["name"] = navn,
        ["description"] = beskrivelse,
        ["input_schema"] = schema
    };

    private static JsonObject Objekt(params (string Navn, string Type, string Beskrivelse, bool Pakrevd)[] felter)
    {
        var props = new JsonObject();
        var pakrevde = new JsonArray();
        foreach (var f in felter)
        {
            props[f.Navn] = new JsonObject { ["type"] = f.Type, ["description"] = f.Beskrivelse };
            if (f.Pakrevd)
            {
                pakrevde.Add(f.Navn);
            }
        }

        var obj = new JsonObject { ["type"] = "object", ["properties"] = props };
        if (pakrevde.Count > 0)
        {
            obj["required"] = pakrevde;
        }
        return obj;
    }
}

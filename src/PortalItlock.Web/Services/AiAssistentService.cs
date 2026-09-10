using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public record AiMelding(string Rolle, string Tekst);

// Read-only Q&A-assistent mot portalens data (prosjekter, dører, tickets). Kaller Anthropics
// Messages API med et lite sett søk/oppslag-verktøy - gjør aldri endringer i databasen selv,
// kun SELECT-spørringer, siden dette er tiltenkt som et hjelpemiddel for å svare på spørsmål,
// ikke en agent som utfører handlinger.
public class AiAssistentService(HttpClient http, IConfiguration config, ApplicationDbContext db)
{
    private const string SystemPrompt =
        "Du er AI-assistenten i portal.itlock, et internt driftssystem for itlock AS (dør/lås-montering). " +
        "Du hjelper ansatte med å finne svar i portalens data - prosjekter, dører, tickets. " +
        "Bruk verktøyene til å slå opp faktisk data før du svarer, ikke gjett. " +
        "Svar kort og konkret på norsk. Du kan ikke gjøre endringer i systemet, kun slå opp informasjon.";

    private const int MaksRunder = 6;

    public async Task<string> SvarAsync(IReadOnlyList<AiMelding> historikk, string nyttSporsmal, CancellationToken ct = default)
    {
        var apiKey = config["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "AI-assistenten er ikke satt opp enda - mangler API-nøkkel. Legg inn \"Anthropic:ApiKey\" i konfigurasjonen (miljøvariabel ANTHROPIC__APIKEY).";
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
                return $"AI-kallet feilet ({(int)response.StatusCode}). Sjekk at API-nøkkelen er gyldig. Detaljer: {responseBody}";
            }

            var svarJson = JsonNode.Parse(responseBody);
            var stopReason = svarJson?["stop_reason"]?.GetValue<string>();
            var contentBlokker = svarJson?["content"]?.AsArray() ?? [];

            meldinger.Add(new JsonObject { ["role"] = "assistant", ["content"] = JsonNode.Parse(contentBlokker.ToJsonString()) });

            if (stopReason != "tool_use")
            {
                var tekst = string.Concat(contentBlokker
                    .Where(b => b?["type"]?.GetValue<string>() == "text")
                    .Select(b => b!["text"]!.GetValue<string>()));
                return string.IsNullOrWhiteSpace(tekst) ? "(Fikk ikke noe svar fra AI-assistenten.)" : tekst;
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

        return "Klarte ikke å komme frem til et svar innen antall forsøk. Prøv å omformulere spørsmålet.";
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
            new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() })
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

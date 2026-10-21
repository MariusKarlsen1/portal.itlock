using System.Threading.Channels;

namespace PortalItlock.Web.Services;

// Hendelser som skal gi e-postvarsel (se EpostVarsler). ApplicationDbContext
// legger dem her når en arbeidsordre tildeles eller en forespørsel kommer inn,
// uansett hvilken side som lagret. EpostVarselSender sender dem i bakgrunnen,
// så lagringen aldri venter på e-post.
public static class EpostVarselKo
{
    public sealed record Hendelse(string ConnectionString, string Nokkel, int Id, int? BrukerId);

    private static readonly Channel<Hendelse> Kanal = Channel.CreateBounded<Hendelse>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest });

    public static ChannelReader<Hendelse> Leser => Kanal.Reader;

    public static void Legg(Hendelse h) => Kanal.Writer.TryWrite(h);
}

namespace PortalItlock.Web.Models;

public class DoorEnvironmentDocument
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public required string FileName { get; set; }
    public int Rekkefolge { get; set; }

    // De opprinnelige dørmiljøene ligger som statiske filer under
    // wwwroot/dormiljo og har Data = null. Dokumenter som lastes opp i
    // portalen lagres i databasen i stedet, fordi filsystemet på Railway er
    // flyktig og opplastinger ellers ville forsvunnet ved neste deploy.
    public byte[]? Data { get; set; }
    public string? ContentType { get; set; }

    public bool ErOpplastet => Data is { Length: > 0 };
}

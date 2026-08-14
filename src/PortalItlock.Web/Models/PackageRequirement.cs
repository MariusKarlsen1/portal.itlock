namespace PortalItlock.Web.Models;

public class PackageRequirement
{
    public int PackageId { get; set; }
    public Package? Package { get; set; }

    public int RequirementValueId { get; set; }
    public RequirementValue? RequirementValue { get; set; }
}

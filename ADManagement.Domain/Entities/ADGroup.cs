namespace ADManagement.Domain.Entities;

/// <summary>
/// Represents an Active Directory group
/// </summary>
public class ADGroup
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string GroupScope { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new();
    public DateTime? WhenCreated { get; set; }
    public DateTime? WhenChanged { get; set; }
}

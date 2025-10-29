namespace ADManagement.Domain.Entities;

/// <summary>
/// Represents an Active Directory Organizational Unit
/// </summary>
public class OrganizationalUnit
{
    public string Name { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime? WhenCreated { get; set; }
    public DateTime? WhenChanged { get; set; }
}

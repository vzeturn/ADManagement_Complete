namespace ADManagement.Application.DTOs;

/// <summary>
/// Data Transfer Object for Organizational Unit
/// </summary>
public class OrganizationalUnitDto
{
    public string Name { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime? WhenCreated { get; set; }
    public DateTime? WhenChanged { get; set; }
    
    public string WhenCreatedFormatted => WhenCreated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown";
}
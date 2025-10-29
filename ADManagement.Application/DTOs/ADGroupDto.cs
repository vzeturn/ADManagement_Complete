namespace ADManagement.Application.DTOs;

/// <summary>
/// Data Transfer Object for AD Group
/// </summary>
public class ADGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string GroupScope { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public List<string> Members { get; set; } = new();
    public DateTime? WhenCreated { get; set; }
    public DateTime? WhenChanged { get; set; }
    
    public string WhenCreatedFormatted => WhenCreated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown";
    public string WhenChangedFormatted => WhenChanged?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown";
}
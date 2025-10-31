using System.ComponentModel.DataAnnotations;

namespace ADManagement.Application.DTOs;

/// <summary>
/// Data Transfer Object for Active Directory Group
/// Used for transferring group data between layers
/// </summary>
public class ADGroupDto
{
    #region Core Identity Properties

    /// <summary>
    /// Group name (CN - Common Name)
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// SAM Account Name (pre-Windows 2000 name)
    /// </summary>
    [MaxLength(256)]
    public string SamAccountName { get; set; } = string.Empty;

    /// <summary>
    /// Display Name
    /// </summary>
    [MaxLength(256)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Distinguished Name (full LDAP path)
    /// </summary>
    [MaxLength(1024)]
    public string DistinguishedName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the group
    /// </summary>
    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;

    #endregion

    #region Group Type Properties

    /// <summary>
    /// Group Scope: Global, Universal, or DomainLocal
    /// </summary>
    [MaxLength(50)]
    public string GroupScope { get; set; } = string.Empty;

    /// <summary>
    /// Group Type: Security or Distribution
    /// </summary>
    [MaxLength(50)]
    public string GroupType { get; set; } = string.Empty;

    /// <summary>
    /// Group Category (alternative name for GroupType)
    /// </summary>
    [MaxLength(50)]
    public string GroupCategory { get; set; } = string.Empty;

    #endregion

    #region Contact Information

    /// <summary>
    /// Email address for the group
    /// </summary>
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Additional email addresses (proxy addresses)
    /// </summary>
    public List<string> ProxyAddresses { get; set; } = new();

    #endregion

    #region Management Properties

    /// <summary>
    /// Distinguished Name of the user/group that manages this group
    /// </summary>
    [MaxLength(1024)]
    public string ManagedBy { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the manager
    /// </summary>
    [MaxLength(256)]
    public string ManagerDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Notes or additional information
    /// </summary>
    [MaxLength(2048)]
    public string Info { get; set; } = string.Empty;

    #endregion

    #region Membership Properties

    /// <summary>
    /// List of member Distinguished Names
    /// </summary>
    public List<string> Members { get; set; } = new();

    /// <summary>
    /// Number of direct members
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// List of groups this group is a member of
    /// </summary>
    public List<string> MemberOf { get; set; } = new();

    #endregion

    #region Timestamps

    /// <summary>
    /// Date and time when the group was created
    /// </summary>
    public DateTime? WhenCreated { get; set; }

    /// <summary>
    /// Date and time when the group was last modified
    /// </summary>
    public DateTime? WhenChanged { get; set; }

    #endregion

    #region Additional Properties

    /// <summary>
    /// Object GUID (unique identifier)
    /// </summary>
    public string ObjectGuid { get; set; } = string.Empty;

    /// <summary>
    /// Object SID (Security Identifier)
    /// </summary>
    public string ObjectSid { get; set; } = string.Empty;

    /// <summary>
    /// Organizational Unit path
    /// </summary>
    [MaxLength(1024)]
    public string OrganizationalUnit { get; set; } = string.Empty;

    /// <summary>
    /// LDAP path to the group object
    /// </summary>
    [MaxLength(1024)]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the group is a system/critical group
    /// </summary>
    public bool IsSystemCritical { get; set; }

    #endregion

    #region Helper Properties

    /// <summary>
    /// Checks if the group is a Security group
    /// </summary>
    public bool IsSecurityGroup =>
        GroupType.Equals("Security", StringComparison.OrdinalIgnoreCase) ||
        GroupCategory.Equals("Security", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the group is a Distribution group
    /// </summary>
    public bool IsDistributionGroup =>
        GroupType.Equals("Distribution", StringComparison.OrdinalIgnoreCase) ||
        GroupCategory.Equals("Distribution", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Formatted display string for group type
    /// </summary>
    public string GroupTypeDisplay => IsSecurityGroup ? "Security" : "Distribution";

    /// <summary>
    /// Formatted display string for group scope
    /// </summary>
    public string GroupScopeDisplay => GroupScope switch
    {
        "Global" => "Global",
        "Universal" => "Universal",
        "DomainLocal" => "Domain Local",
        "Local" => "Domain Local",
        _ => GroupScope
    };

    /// <summary>
    /// Combined group info for display
    /// </summary>
    public string GroupInfo => $"{Name} ({GroupScopeDisplay} {GroupTypeDisplay})";

    #endregion

    #region Methods

    /// <summary>
    /// Gets the common name from the Distinguished Name
    /// </summary>
    public string GetCommonName()
    {
        if (string.IsNullOrWhiteSpace(DistinguishedName))
            return Name;

        var parts = DistinguishedName.Split(',');
        if (parts.Length == 0)
            return Name;

        var cnPart = parts[0];
        if (cnPart.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            return cnPart.Substring(3);

        return Name;
    }

    /// <summary>
    /// Returns a formatted string representation of the group
    /// </summary>
    public override string ToString()
    {
        return GroupInfo;
    }

    #endregion
}
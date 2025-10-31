namespace ADManagement.Domain.Entities;

/// <summary>
/// Represents an Active Directory group
/// Complete entity with all required properties for group management
/// </summary>
public class ADGroup
{
    #region Core Identity Properties

    /// <summary>
    /// Group name (CN - Common Name)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// SAM Account Name (pre-Windows 2000 name)
    /// </summary>
    public string SamAccountName { get; set; } = string.Empty;

    /// <summary>
    /// Display Name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Distinguished Name (full LDAP path)
    /// Example: CN=Domain Admins,CN=Users,DC=contoso,DC=com
    /// </summary>
    public string DistinguishedName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the group
    /// </summary>
    public string Description { get; set; } = string.Empty;

    #endregion

    #region Group Type Properties

    /// <summary>
    /// Group Scope: Global, Universal, or DomainLocal
    /// </summary>
    public string GroupScope { get; set; } = string.Empty;

    /// <summary>
    /// Group Type: Security or Distribution
    /// </summary>
    public string GroupType { get; set; } = string.Empty;

    /// <summary>
    /// Group Category (alternative name for GroupType)
    /// </summary>
    public string GroupCategory { get; set; } = string.Empty;

    #endregion

    #region Contact Information

    /// <summary>
    /// Email address for the group
    /// </summary>
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
    public string ManagedBy { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the manager
    /// </summary>
    public string ManagerDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Notes or additional information
    /// </summary>
    public string Info { get; set; } = string.Empty;

    #endregion

    #region Membership Properties

    /// <summary>
    /// List of member Distinguished Names
    /// Users and groups that are members of this group
    /// </summary>
    public List<string> Members { get; set; } = new();

    /// <summary>
    /// Number of direct members
    /// </summary>
    public int MemberCount => Members?.Count ?? 0;

    /// <summary>
    /// List of groups this group is a member of (Distinguished Names)
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
    public string OrganizationalUnit { get; set; } = string.Empty;

    /// <summary>
    /// LDAP path to the group object
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the group is a system/critical group
    /// </summary>
    public bool IsSystemCritical { get; set; }

    /// <summary>
    /// Additional attributes as key-value pairs
    /// </summary>
    public Dictionary<string, object> AdditionalAttributes { get; set; } = new();

    #endregion

    #region Helper Methods

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
    /// Checks if the group is a Global group
    /// </summary>
    public bool IsGlobalScope =>
        GroupScope.Equals("Global", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the group is a Universal group
    /// </summary>
    public bool IsUniversalScope =>
        GroupScope.Equals("Universal", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the group is a Domain Local group
    /// </summary>
    public bool IsDomainLocalScope =>
        GroupScope.Equals("DomainLocal", StringComparison.OrdinalIgnoreCase) ||
        GroupScope.Equals("Local", StringComparison.OrdinalIgnoreCase);

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
        return $"{Name} ({GroupScope} {GroupType})";
    }

    #endregion
}
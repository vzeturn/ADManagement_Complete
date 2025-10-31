using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ADManagement.Application.DTOs;

/// <summary>
/// Comprehensive Data Transfer Object for Active Directory User
/// Contains all standard AD user attributes with validation and helper methods
/// </summary>
public class ADUserDto
{
    #region Core Identity Information

    /// <summary>
    /// SAM Account Name (pre-Windows 2000 logon name)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string SamAccountName { get; set; } = string.Empty;

    /// <summary>
    /// User Principal Name (username@domain.com format)
    /// </summary>
    [EmailAddress]
    public string UserPrincipalName { get; set; } = string.Empty;

    /// <summary>
    /// Display Name (full name as shown in AD)
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Distinguished Name (full LDAP path)
    /// </summary>
    public string DistinguishedName { get; set; } = string.Empty;

    /// <summary>
    /// Canonical Name (domain/OU/username format)
    /// </summary>
    public string CanonicalName { get; set; } = string.Empty;

    /// <summary>
    /// Object GUID (unique identifier)
    /// </summary>
    public Guid? ObjectGuid { get; set; }

    /// <summary>
    /// Object SID (Security Identifier)
    /// </summary>
    public string ObjectSid { get; set; } = string.Empty;

    #endregion

    #region Personal Information

    /// <summary>
    /// Given Name (First Name)
    /// </summary>
    [MaxLength(64)]
    public string GivenName { get; set; } = string.Empty;

    /// <summary>
    /// Surname (Last Name)
    /// </summary>
    [MaxLength(64)]
    public string Surname { get; set; } = string.Empty;

    /// <summary>
    /// Middle Name or Initial
    /// </summary>
    [MaxLength(64)]
    public string MiddleName { get; set; } = string.Empty;

    /// <summary>
    /// Initials (e.g., "J.D.")
    /// </summary>
    [MaxLength(6)]
    public string Initials { get; set; } = string.Empty;

    /// <summary>
    /// Common Name (CN)
    /// </summary>
    public string CommonName { get; set; } = string.Empty;

    /// <summary>
    /// Name (Full Name)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description field
    /// </summary>
    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Employee ID
    /// </summary>
    [MaxLength(16)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// Employee Number
    /// </summary>
    [MaxLength(16)]
    public string EmployeeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Employee Type (e.g., Full-time, Contractor, Intern)
    /// </summary>
    [MaxLength(256)]
    public string EmployeeType { get; set; } = string.Empty;

    #endregion

    #region Contact Information

    /// <summary>
    /// Primary Email Address
    /// </summary>
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Additional Email Addresses (proxyAddresses)
    /// </summary>
    public List<string> ProxyAddresses { get; set; } = new();

    /// <summary>
    /// Office/Business Phone Number
    /// </summary>
    [Phone]
    [MaxLength(64)]
    public string TelephoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Home Phone Number
    /// </summary>
    [Phone]
    [MaxLength(64)]
    public string HomePhone { get; set; } = string.Empty;

    /// <summary>
    /// Mobile Phone Number
    /// </summary>
    [Phone]
    [MaxLength(64)]
    public string Mobile { get; set; } = string.Empty;

    /// <summary>
    /// Pager Number
    /// </summary>
    [Phone]
    [MaxLength(64)]
    public string Pager { get; set; } = string.Empty;

    /// <summary>
    /// Fax Number
    /// </summary>
    [Phone]
    [MaxLength(64)]
    public string Fax { get; set; } = string.Empty;

    /// <summary>
    /// IP Phone Number
    /// </summary>
    [MaxLength(64)]
    public string IpPhone { get; set; } = string.Empty;

    /// <summary>
    /// Personal Web Page URL
    /// </summary>
    [Url]
    [MaxLength(2048)]
    public string WebPage { get; set; } = string.Empty;

    /// <summary>
    /// Additional URLs
    /// </summary>
    public List<string> Url { get; set; } = new();

    #endregion

    #region Organization Information

    /// <summary>
    /// Department Name
    /// </summary>
    [MaxLength(256)]
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Job Title
    /// </summary>
    [MaxLength(128)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Company Name
    /// </summary>
    [MaxLength(256)]
    public string Company { get; set; } = string.Empty;

    /// <summary>
    /// Manager's Distinguished Name
    /// </summary>
    public string Manager { get; set; } = string.Empty;

    /// <summary>
    /// Manager's Display Name (computed)
    /// </summary>
    public string ManagerDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Direct Reports (list of DNs)
    /// </summary>
    public List<string> DirectReports { get; set; } = new();

    /// <summary>
    /// Division/Business Unit
    /// </summary>
    [MaxLength(256)]
    public string Division { get; set; } = string.Empty;

    /// <summary>
    /// Office Location
    /// </summary>
    [MaxLength(128)]
    public string Office { get; set; } = string.Empty;

    /// <summary>
    /// Physical Delivery Office Name
    /// </summary>
    [MaxLength(128)]
    public string PhysicalDeliveryOfficeName { get; set; } = string.Empty;

    #endregion

    #region Address Information

    /// <summary>
    /// Street Address (Line 1)
    /// </summary>
    [MaxLength(1024)]
    public string StreetAddress { get; set; } = string.Empty;

    /// <summary>
    /// P.O. Box
    /// </summary>
    [MaxLength(40)]
    public string PostOfficeBox { get; set; } = string.Empty;

    /// <summary>
    /// City/Locality
    /// </summary>
    [MaxLength(128)]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State/Province
    /// </summary>
    [MaxLength(128)]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// ZIP/Postal Code
    /// </summary>
    [MaxLength(40)]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country/Region Code (2-letter ISO code)
    /// </summary>
    [MaxLength(2)]
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Country/Region Name
    /// </summary>
    [MaxLength(128)]
    public string Country { get; set; } = string.Empty;

    #endregion

    #region Account Status Information

    /// <summary>
    /// Is account enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Is account locked out
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Account lockout time
    /// </summary>
    public DateTime? LockoutTime { get; set; }

    /// <summary>
    /// Is password expired
    /// </summary>
    public bool PasswordExpired { get; set; }

    /// <summary>
    /// User must change password at next logon
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>
    /// Password never expires
    /// </summary>
    public bool PasswordNeverExpires { get; set; }

    /// <summary>
    /// User cannot change password
    /// </summary>
    public bool CannotChangePassword { get; set; }

    /// <summary>
    /// Account is sensitive and cannot be delegated
    /// </summary>
    public bool AccountNotDelegated { get; set; }

    /// <summary>
    /// Use DES encryption types for this account
    /// </summary>
    public bool UseDESKeyOnly { get; set; }

    /// <summary>
    /// Don't require Kerberos pre-authentication
    /// </summary>
    public bool DoesNotRequirePreAuth { get; set; }

    /// <summary>
    /// Trusted for delegation
    /// </summary>
    public bool TrustedForDelegation { get; set; }

    /// <summary>
    /// Trusted to authenticate for delegation
    /// </summary>
    public bool TrustedToAuthForDelegation { get; set; }

    /// <summary>
    /// SmartCard required for interactive logon
    /// </summary>
    public bool SmartCardLogonRequired { get; set; }

    /// <summary>
    /// Account status (Enabled, Disabled, Locked, etc.)
    /// </summary>
    public string AccountStatus { get; set; } = string.Empty;

    /// <summary>
    /// User Account Control flags (raw value)
    /// </summary>
    public int? UserAccountControl { get; set; }

    #endregion

    #region Timestamp Information

    /// <summary>
    /// When the account was created
    /// </summary>
    public DateTime? WhenCreated { get; set; }

    /// <summary>
    /// When the account was last modified
    /// </summary>
    public DateTime? WhenChanged { get; set; }

    /// <summary>
    /// Last logon timestamp (replicated)
    /// </summary>
    public DateTime? LastLogonTimestamp { get; set; }

    /// <summary>
    /// Last logon (not replicated, most accurate)
    /// </summary>
    public DateTime? LastLogon { get; set; }

    /// <summary>
    /// Last logoff
    /// </summary>
    public DateTime? LastLogoff { get; set; }

    /// <summary>
    /// Bad password time
    /// </summary>
    public DateTime? BadPasswordTime { get; set; }

    /// <summary>
    /// Password last set timestamp
    /// </summary>
    public DateTime? PasswordLastSet { get; set; }

    /// <summary>
    /// Account expiration date
    /// </summary>
    public DateTime? AccountExpirationDate { get; set; }

    /// <summary>
    /// Account expires (raw value)
    /// </summary>
    public long? AccountExpires { get; set; }

    #endregion

    #region Logon Information

    /// <summary>
    /// Logon count
    /// </summary>
    public int? LogonCount { get; set; }

    /// <summary>
    /// Bad password count
    /// </summary>
    public int? BadPasswordCount { get; set; }

    /// <summary>
    /// Logon hours (48 bytes representing 168 hours)
    /// </summary>
    public byte[]? LogonHours { get; set; }

    /// <summary>
    /// Workstations user can log on to
    /// </summary>
    public string UserWorkstations { get; set; } = string.Empty;

    /// <summary>
    /// Logon script path
    /// </summary>
    [MaxLength(512)]
    public string ScriptPath { get; set; } = string.Empty;

    /// <summary>
    /// Profile path
    /// </summary>
    [MaxLength(512)]
    public string ProfilePath { get; set; } = string.Empty;

    /// <summary>
    /// Home directory path
    /// </summary>
    [MaxLength(512)]
    public string HomeDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Home drive letter (e.g., "H:")
    /// </summary>
    [MaxLength(3)]
    public string HomeDrive { get; set; } = string.Empty;

    #endregion

    #region Group Membership

    /// <summary>
    /// Primary group ID
    /// </summary>
    public int? PrimaryGroupId { get; set; }

    /// <summary>
    /// Primary group DN
    /// </summary>
    public string PrimaryGroup { get; set; } = string.Empty;

    /// <summary>
    /// Member of groups (list of DNs)
    /// </summary>
    public List<string> MemberOf { get; set; } = new();

    /// <summary>
    /// Member of groups (display names)
    /// </summary>
    public List<string> MemberOfNames { get; set; } = new();

    #endregion

    #region Exchange/Mail Attributes

    /// <summary>
    /// Mail Nickname (Exchange alias)
    /// </summary>
    [MaxLength(64)]
    public string MailNickname { get; set; } = string.Empty;

    /// <summary>
    /// Target Address (for mail forwarding)
    /// </summary>
    [MaxLength(1024)]
    public string TargetAddress { get; set; } = string.Empty;

    /// <summary>
    /// Legacy Exchange DN
    /// </summary>
    [MaxLength(1024)]
    public string LegacyExchangeDN { get; set; } = string.Empty;

    /// <summary>
    /// MS Exchange version
    /// </summary>
    public long? MsExchVersion { get; set; }

    /// <summary>
    /// Mailbox database
    /// </summary>
    public string MailboxDatabase { get; set; } = string.Empty;

    /// <summary>
    /// Hide from address lists
    /// </summary>
    public bool HideFromAddressLists { get; set; }

    #endregion

    #region Custom Attributes (extensionAttribute1-15)

    public string ExtensionAttribute1 { get; set; } = string.Empty;
    public string ExtensionAttribute2 { get; set; } = string.Empty;
    public string ExtensionAttribute3 { get; set; } = string.Empty;
    public string ExtensionAttribute4 { get; set; } = string.Empty;
    public string ExtensionAttribute5 { get; set; } = string.Empty;
    public string ExtensionAttribute6 { get; set; } = string.Empty;
    public string ExtensionAttribute7 { get; set; } = string.Empty;
    public string ExtensionAttribute8 { get; set; } = string.Empty;
    public string ExtensionAttribute9 { get; set; } = string.Empty;
    public string ExtensionAttribute10 { get; set; } = string.Empty;
    public string ExtensionAttribute11 { get; set; } = string.Empty;
    public string ExtensionAttribute12 { get; set; } = string.Empty;
    public string ExtensionAttribute13 { get; set; } = string.Empty;
    public string ExtensionAttribute14 { get; set; } = string.Empty;
    public string ExtensionAttribute15 { get; set; } = string.Empty;

    #endregion

    #region Computed/Helper Properties

    /// <summary>
    /// Full name (computed from GivenName and Surname)
    /// </summary>
    [JsonIgnore]
    public string FullName => 
        string.IsNullOrWhiteSpace(MiddleName) 
            ? $"{GivenName} {Surname}".Trim()
            : $"{GivenName} {MiddleName} {Surname}".Trim();

    /// <summary>
    /// First name (alias for GivenName)
    /// </summary>
    [JsonIgnore]
    public string FirstName
    {
        get => GivenName;
        set => GivenName = value;
    }

    /// <summary>
    /// Last name (alias for Surname)
    /// </summary>
    [JsonIgnore]
    public string LastName
    {
        get => Surname;
        set => Surname = value;
    }

    /// <summary>
    /// Username (alias for SamAccountName)
    /// </summary>
    [JsonIgnore]
    public string Username
    {
        get => SamAccountName;
        set => SamAccountName = value;
    }

    /// <summary>
    /// Phone number (alias for TelephoneNumber)
    /// </summary>
    [JsonIgnore]
    public string PhoneNumber
    {
        get => TelephoneNumber;
        set => TelephoneNumber = value;
    }

    /// <summary>
    /// Mobile number (alias for Mobile)
    /// </summary>
    [JsonIgnore]
    public string MobileNumber
    {
        get => Mobile;
        set => Mobile = value;
    }

    /// <summary>
    /// Fax number (alias for Fax)
    /// </summary>
    [JsonIgnore]
    public string FaxNumber
    {
        get => Fax;
        set => Fax = value;
    }

    /// <summary>
    /// Is account active (enabled and not expired)
    /// </summary>
    [JsonIgnore]
    public bool IsActive => IsEnabled && !IsAccountExpired && !IsLocked;

    /// <summary>
    /// Is account expired
    /// </summary>
    [JsonIgnore]
    public bool IsAccountExpired => 
        AccountExpirationDate.HasValue && AccountExpirationDate.Value < DateTime.Now;

    /// <summary>
    /// Days until password expires
    /// </summary>
    [JsonIgnore]
    public int? DaysUntilPasswordExpires
    {
        get
        {
            if (PasswordNeverExpires || !PasswordLastSet.HasValue)
                return null;

            // Assuming 90-day password policy (should be configurable)
            var expirationDate = PasswordLastSet.Value.AddDays(90);
            var daysRemaining = (expirationDate - DateTime.Now).Days;
            return daysRemaining;
        }
    }

    /// <summary>
    /// Days since last logon
    /// </summary>
    [JsonIgnore]
    public int? DaysSinceLastLogon => 
        LastLogon.HasValue ? (int)(DateTime.Now - LastLogon.Value).TotalDays : null;

    /// <summary>
    /// Account age in days
    /// </summary>
    [JsonIgnore]
    public int? AccountAgeInDays => 
        WhenCreated.HasValue ? (int)(DateTime.Now - WhenCreated.Value).TotalDays : null;

    /// <summary>
    /// Number of groups user is member of
    /// </summary>
    [JsonIgnore]
    public int GroupCount => MemberOf?.Count ?? 0;

    /// <summary>
    /// Has manager assigned
    /// </summary>
    [JsonIgnore]
    public bool HasManager => !string.IsNullOrWhiteSpace(Manager);

    /// <summary>
    /// Has direct reports
    /// </summary>
    [JsonIgnore]
    public bool HasDirectReports => DirectReports?.Any() ?? false;

    /// <summary>
    /// Number of direct reports
    /// </summary>
    [JsonIgnore]
    public int DirectReportCount => DirectReports?.Count ?? 0;

    #endregion

    #region Formatted Display Properties

    /// <summary>
    /// Last logon formatted for display
    /// </summary>
    [JsonIgnore]
    public string LastLogonFormatted => 
        LastLogon?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never";

    /// <summary>
    /// Last logon timestamp formatted for display
    /// </summary>
    [JsonIgnore]
    public string LastLogonTimestampFormatted => 
        LastLogonTimestamp?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never";

    /// <summary>
    /// Password last set formatted for display
    /// </summary>
    [JsonIgnore]
    public string PasswordLastSetFormatted => 
        PasswordLastSet?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never set";

    /// <summary>
    /// Account expires formatted for display
    /// </summary>
    [JsonIgnore]
    public string AccountExpiresFormatted => 
        AccountExpirationDate?.ToString("yyyy-MM-dd") ?? "Never";

    /// <summary>
    /// Account created formatted for display
    /// </summary>
    [JsonIgnore]
    public string WhenCreatedFormatted => 
        WhenCreated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown";

    /// <summary>
    /// Account modified formatted for display
    /// </summary>
    [JsonIgnore]
    public string WhenChangedFormatted => 
        WhenChanged?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown";

    /// <summary>
    /// Lockout time formatted for display
    /// </summary>
    [JsonIgnore]
    public string LockoutTimeFormatted => 
        LockoutTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Not locked";

    /// <summary>
    /// Bad password time formatted for display
    /// </summary>
    [JsonIgnore]
    public string BadPasswordTimeFormatted => 
        BadPasswordTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never";

    /// <summary>
    /// Status badge (emoji + text for UI)
    /// </summary>
    [JsonIgnore]
    public string StatusBadge
    {
        get
        {
            if (IsLocked) return "🔒 Locked";
            if (!IsEnabled) return "❌ Disabled";
            if (IsAccountExpired) return "⏰ Expired";
            if (PasswordExpired) return "⚠️ Password Expired";
            if (MustChangePassword) return "🔑 Must Change Password";
            return "✅ Active";
        }
    }

    /// <summary>
    /// Full address formatted for display
    /// </summary>
    [JsonIgnore]
    public string FullAddress
    {
        get
        {
            var parts = new[]
            {
                StreetAddress,
                City,
                State,
                PostalCode,
                Country
            }.Where(s => !string.IsNullOrWhiteSpace(s));

            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Organization path (Company > Department > Title)
    /// </summary>
    [JsonIgnore]
    public string OrganizationPath
    {
        get
        {
            var parts = new[]
            {
                Company,
                Department,
                Title
            }.Where(s => !string.IsNullOrWhiteSpace(s));

            return string.Join(" > ", parts);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets a summary of the user for display
    /// </summary>
    public string GetSummary()
    {
        return $"{DisplayName} ({SamAccountName}) - {Title ?? "No Title"} in {Department ?? "No Department"}";
    }

    /// <summary>
    /// Gets all contact methods for the user
    /// </summary>
    public Dictionary<string, string> GetContactMethods()
    {
        var contacts = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(Email))
            contacts["Email"] = Email;
        if (!string.IsNullOrWhiteSpace(TelephoneNumber))
            contacts["Phone"] = TelephoneNumber;
        if (!string.IsNullOrWhiteSpace(Mobile))
            contacts["Mobile"] = Mobile;
        if (!string.IsNullOrWhiteSpace(Fax))
            contacts["Fax"] = Fax;
        if (!string.IsNullOrWhiteSpace(IpPhone))
            contacts["IP Phone"] = IpPhone;
        if (!string.IsNullOrWhiteSpace(Pager))
            contacts["Pager"] = Pager;

        return contacts;
    }

    /// <summary>
    /// Gets all issues/warnings for this account
    /// </summary>
    public List<string> GetAccountIssues()
    {
        var issues = new List<string>();

        if (!IsEnabled)
            issues.Add("Account is disabled");
        if (IsLocked)
            issues.Add("Account is locked out");
        if (IsAccountExpired)
            issues.Add("Account has expired");
        if (PasswordExpired)
            issues.Add("Password has expired");
        if (MustChangePassword)
            issues.Add("Must change password at next logon");

        var daysSinceLastLogon = DaysSinceLastLogon;
        if (daysSinceLastLogon.HasValue && daysSinceLastLogon.Value > 90)
            issues.Add($"No logon in {daysSinceLastLogon.Value} days");

        var daysUntilPasswordExpires = DaysUntilPasswordExpires;
        if (daysUntilPasswordExpires.HasValue && daysUntilPasswordExpires.Value <= 14 && daysUntilPasswordExpires.Value > 0)
            issues.Add($"Password expires in {daysUntilPasswordExpires.Value} days");

        if (BadPasswordCount > 0)
            issues.Add($"{BadPasswordCount} failed login attempt(s)");

        return issues;
    }

    /// <summary>
    /// Validates if all required fields are populated
    /// </summary>
    public List<string> GetMissingRequiredFields()
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(SamAccountName))
            missing.Add("Username (SAM Account Name)");
        if (string.IsNullOrWhiteSpace(DisplayName))
            missing.Add("Display Name");
        if (string.IsNullOrWhiteSpace(GivenName))
            missing.Add("First Name");
        if (string.IsNullOrWhiteSpace(Surname))
            missing.Add("Last Name");

        return missing;
    }

    /// <summary>
    /// Creates a clone of this DTO
    /// </summary>
    public ADUserDto Clone()
    {
        return (ADUserDto)this.MemberwiseClone();
    }

    /// <summary>
    /// Converts to dictionary for logging or export
    /// </summary>
    public Dictionary<string, object?> ToDictionary()
    {
        return new Dictionary<string, object?>
        {
            ["Username"] = SamAccountName,
            ["Display Name"] = DisplayName,
            ["Email"] = Email,
            ["First Name"] = GivenName,
            ["Last Name"] = Surname,
            ["Title"] = Title,
            ["Department"] = Department,
            ["Company"] = Company,
            ["Office"] = Office,
            ["Manager"] = ManagerDisplayName,
            ["Phone"] = TelephoneNumber,
            ["Mobile"] = Mobile,
            ["Status"] = StatusBadge,
            ["Last Logon"] = LastLogonFormatted,
            ["Account Created"] = WhenCreatedFormatted,
            ["Groups"] = GroupCount,
            ["Direct Reports"] = DirectReportCount
        };
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
        return $"{DisplayName} ({SamAccountName})";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ADUserDto other)
            return false;

        return ObjectGuid.HasValue && other.ObjectGuid.HasValue
            ? ObjectGuid.Value == other.ObjectGuid.Value
            : SamAccountName.Equals(other.SamAccountName, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return ObjectGuid?.GetHashCode() ?? SamAccountName.GetHashCode();
    }

    #endregion
}
namespace ADManagement.Domain.Entities;

/// <summary>
/// Represents an Active Directory user with all relevant properties
/// </summary>
public class ADUser
{
    // Basic Information
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    
    // Organization Information
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Office { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    
    // Contact Information
    public string PhoneNumber { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string FaxNumber { get; set; } = string.Empty;
    
    // Address Information
    public string StreetAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    
    // Account Information
    public string DistinguishedName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsLockedOut { get; set; }
    public bool PasswordNeverExpires { get; set; }
    public bool MustChangePasswordOnNextLogon { get; set; }
    
    // Timestamps
    public DateTime? LastLogon { get; set; }
    public DateTime? LastPasswordSet { get; set; }
    public DateTime? AccountExpires { get; set; }
    public DateTime? WhenCreated { get; set; }
    public DateTime? WhenChanged { get; set; }
    
    // Additional Information
    public string Description { get; set; } = string.Empty;
    public List<string> MemberOf { get; set; } = new();
    
    /// <summary>
    /// Gets the full name of the user
    /// </summary>
    public string FullName => !string.IsNullOrWhiteSpace(DisplayName) 
        ? DisplayName 
        : $"{FirstName} {LastName}".Trim();
    
    /// <summary>
    /// Gets the account status as a readable string
    /// </summary>
    public string AccountStatus => IsEnabled ? (IsLockedOut ? "Locked" : "Active") : "Disabled";
}

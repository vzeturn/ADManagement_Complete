namespace ADManagement.Application.DTOs;

/// <summary>
/// Request model for creating a new Active Directory user
/// </summary>
public class CreateUserRequest
{
    // Required fields
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    
    // Organizational Unit
    public string? OrganizationalUnit { get; set; }
    
    // Optional fields
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? Office { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Description { get; set; }
    
    // Account options
    public bool MustChangePasswordOnNextLogon { get; set; } = true;
    public bool AccountEnabled { get; set; } = true;
    public bool PasswordNeverExpires { get; set; } = false;
    
    // Initial groups to add user to
    public List<string> InitialGroups { get; set; } = new();
}

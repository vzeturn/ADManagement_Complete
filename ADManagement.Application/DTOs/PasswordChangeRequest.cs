namespace ADManagement.Application.DTOs;

/// <summary>
/// Request model for password change operation
/// </summary>
public class PasswordChangeRequest
{
    public string Username { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
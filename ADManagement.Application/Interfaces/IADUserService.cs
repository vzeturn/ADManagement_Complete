using ADManagement.Application.DTOs;
using ADManagement.Domain.Common;
using ADManagement.Domain.Entities;

namespace ADManagement.Application.Interfaces;

/// <summary>
/// Service interface for AD User operations
/// </summary>
public interface IADUserService
{
    /// <summary>
    /// Tests the connection to Active Directory
    /// </summary>
    Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all users from Active Directory
    /// </summary>
    Task<Result<IEnumerable<ADUserDto>>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets users from a specific Organizational Unit
    /// </summary>
    Task<Result<IEnumerable<ADUserDto>>> GetUsersByOUAsync(string ouPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a user by username
    /// </summary>
    Task<Result<ADUserDto>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a user by email address
    /// </summary>
    Task<Result<ADUserDto>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches for users matching the search term
    /// </summary>
    Task<Result<IEnumerable<ADUserDto>>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Changes a user's password
    /// </summary>
    Task<Result> ChangePasswordAsync(PasswordChangeRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Enables a user account
    /// </summary>
    Task<Result> EnableUserAsync(string username, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Disables a user account
    /// </summary>
    Task<Result> DisableUserAsync(string username, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unlocks a user account
    /// </summary>
    Task<Result> UnlockUserAsync(string username, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new user in Active Directory
    /// </summary>
    Task<Result<ADUserDto>> CreateUserAsync(DTOs.CreateUserRequest request, CancellationToken cancellationToken = default);

    // ✅ Add this
    Task<Result> UpdateUserAsync(ADUserDto user, CancellationToken cancellationToken = default);


}
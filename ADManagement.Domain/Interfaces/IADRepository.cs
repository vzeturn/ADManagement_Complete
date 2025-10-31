using ADManagement.Domain.Common;
using ADManagement.Domain.Entities;

namespace ADManagement.Domain.Interfaces;

/// <summary>
/// Repository interface for Active Directory operations
/// </summary>
public interface IADRepository
{
    #region Connection
    
    /// <summary>
    /// Tests the connection to Active Directory
    /// </summary>
    Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default);
    
    #endregion
    
    #region User Operations
    
    /// <summary>
    /// Gets all users from Active Directory
    /// </summary>
    Task<Result<IEnumerable<ADUser>>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets users from a specific Organizational Unit
    /// </summary>
    Task<Result<IEnumerable<ADUser>>> GetUsersByOUAsync(string ouPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a user by username
    /// </summary>
    Task<Result<ADUser>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a user by email address
    /// </summary>
    Task<Result<ADUser>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches for users matching the search term
    /// </summary>
    Task<Result<IEnumerable<ADUser>>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Changes a user's password
    /// </summary>
    Task<Result> ChangePasswordAsync(string username, string newPassword, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Enables or disables a user account
    /// </summary>
    Task<Result> SetUserStatusAsync(string username, bool enabled, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unlocks a user account
    /// </summary>
    Task<Result> UnlockUserAsync(string username, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Group Operations
    
    /// <summary>
    /// Gets all groups from Active Directory
    /// </summary>
    Task<Result<IEnumerable<ADGroup>>> GetAllGroupsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets groups that a user is a member of
    /// </summary>
    Task<Result<IEnumerable<string>>> GetUserGroupsAsync(string username, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a user to a group
    /// </summary>
    Task<Result> AddUserToGroupAsync(string username, string groupName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a user from a group
    /// </summary>
    Task<Result> RemoveUserFromGroupAsync(string username, string groupName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches for groups matching the search term
    /// </summary>
    Task<Result<IEnumerable<ADGroup>>> SearchGroupsAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Organizational Unit Operations
    
    /// <summary>
    /// Gets all Organizational Units
    /// </summary>
    Task<Result<IEnumerable<OrganizationalUnit>>> GetAllOUsAsync(CancellationToken cancellationToken = default);
    
    #endregion
}

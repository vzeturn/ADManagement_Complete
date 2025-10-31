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
    Task<Result> ChangePasswordAsync(string username, string newPassword, bool mustChangeAtNextLogon, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Enables or disables a user account
    /// </summary>
    Task<Result> SetUserStatusAsync(string username, bool enabled, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unlocks a user account
    /// </summary>
    Task<Result> UnlockUserAsync(string username, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new user in Active Directory
    /// </summary>
    Task<Result<ADUser>> CreateUserAsync(
        string username,
        string firstName,
        string lastName,
        string password,
        string? organizationalUnit,
        string? displayName,
        string? email,
        string? department,
        string? title,
        string? company,
        string? office,
        string? phoneNumber,
        string? description,
        bool mustChangePasswordOnNextLogon,
        bool accountEnabled,
        bool passwordNeverExpires,
        IEnumerable<string>? initialGroups,
        CancellationToken cancellationToken = default);
    Task<Result> UpdateUserAsync(ADUser user, CancellationToken cancellationToken = default);

    #endregion

    #region Group Operations


    // ⭐ Add Group methods
    Task<Result<IEnumerable<ADGroup>>> GetAllGroupsAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ADGroup>>> SearchGroupsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Result<ADGroup>> GetGroupByNameAsync(string groupName, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ADGroup>>> GetUserGroupsAsync(string username, CancellationToken cancellationToken = default);
    Task<Result> AddUserToGroupAsync(string username, string groupName, CancellationToken cancellationToken = default);
    Task<Result> RemoveUserFromGroupAsync(string username, string groupName, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ADUser>>> GetGroupMembersAsync(string groupName, CancellationToken cancellationToken = default);
    Task<Result<ADGroup>> CreateGroupAsync(string groupName, string description, string groupScope, string groupType, string organizationalUnit, CancellationToken cancellationToken = default);
    Task<Result> DeleteGroupAsync(string groupName, CancellationToken cancellationToken = default);
    Task<Result> UpdateGroupAsync(ADGroup group, CancellationToken cancellationToken = default);

    #endregion

    #region Organizational Unit Operations

    /// <summary>
    /// Gets all Organizational Units
    /// </summary>
    Task<Result<IEnumerable<OrganizationalUnit>>> GetAllOUsAsync(CancellationToken cancellationToken = default);
    
    #endregion
}

using ADManagement.Domain.Common;
using ADManagement.Domain.Entities;
using System.Runtime.CompilerServices;

namespace ADManagement.Domain.Interfaces;

/// <summary>
/// Repository interface for Active Directory operations
/// ✨ OPTIMIZED with streaming support
/// </summary>
public interface IADRepository
{
    #region Connection

    Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default);

    #endregion

    #region User Operations

    Task<Result<IEnumerable<ADUser>>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ✨ NEW: Stream users asynchronously to avoid loading all in memory
    /// </summary>
    IAsyncEnumerable<ADUser> StreamUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ✨ NEW: Stream users from specific OU
    /// </summary>
    IAsyncEnumerable<ADUser> StreamUsersByOUAsync(string ouPath, CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<ADUser>>> GetUsersByOUAsync(string ouPath, CancellationToken cancellationToken = default);
    Task<Result<ADUser>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<Result<ADUser>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ADUser>>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(string username, string newPassword, bool mustChangeAtNextLogon, CancellationToken cancellationToken = default);
    Task<Result> SetUserStatusAsync(string username, bool enabled, CancellationToken cancellationToken = default);
    Task<Result> UnlockUserAsync(string username, CancellationToken cancellationToken = default);
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

    Task<Result<IEnumerable<OrganizationalUnit>>> GetAllOUsAsync(CancellationToken cancellationToken = default);

    #endregion
}
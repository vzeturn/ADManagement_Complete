using ADManagement.Application.DTOs;
using ADManagement.Domain.Common;

namespace ADManagement.Application.Interfaces;

/// <summary>
/// Service interface for Active Directory Group operations
/// </summary>
public interface IADGroupService
{
    /// <summary>
    /// Gets all groups from Active Directory
    /// </summary>
    Task<Result<IEnumerable<ADGroupDto>>> GetAllGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for groups matching the search term (fuzzy search)
    /// </summary>
    /// <param name="searchTerm">Search term for group name, description, or DN</param>
    Task<Result<IEnumerable<ADGroupDto>>> SearchGroupsAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific group by name
    /// </summary>
    Task<Result<ADGroupDto>> GetGroupByNameAsync(string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets groups that a user is a member of
    /// </summary>
    Task<Result<IEnumerable<ADGroupDto>>> GetUserGroupsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user to a group
    /// </summary>
    Task<Result> AddUserToGroupAsync(string username, string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a user from a group
    /// </summary>
    Task<Result> RemoveUserFromGroupAsync(string username, string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all members of a group
    /// </summary>
    Task<Result<IEnumerable<ADUserDto>>> GetGroupMembersAsync(string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new group in Active Directory
    /// </summary>
    Task<Result<ADGroupDto>> CreateGroupAsync(
        string groupName,
        string description,
        string groupScope,
        string groupType,
        string organizationalUnit = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a group from Active Directory
    /// </summary>
    Task<Result> DeleteGroupAsync(string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates group properties
    /// </summary>
    Task<Result> UpdateGroupAsync(ADGroupDto group, CancellationToken cancellationToken = default);
}

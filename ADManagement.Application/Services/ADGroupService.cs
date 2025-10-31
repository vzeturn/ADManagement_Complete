using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.Application.Mappings;
using ADManagement.Domain.Common;
using ADManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ADManagement.Application.Services;

/// <summary>
/// Service implementation for Active Directory Group operations
/// </summary>
public class ADGroupService : IADGroupService
{
    private readonly IADRepository _repository;
    private readonly ILogger<ADGroupService> _logger;

    public ADGroupService(
        IADRepository repository,
        ILogger<ADGroupService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<ADGroupDto>>> GetAllGroupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting all groups from Active Directory");

            var result = await _repository.GetAllGroupsAsync(cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogWarning("Failed to get groups: {Message}", result.Message);
                return Result.Failure<IEnumerable<ADGroupDto>>(result.Message);
            }

            var groupDtos = result.Value.Select(ADGroupMapper.ToDto).ToList();

            _logger.LogInformation("Successfully retrieved {Count} groups", groupDtos.Count);
            return Result.Success<IEnumerable<ADGroupDto>>(groupDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all groups");
            return Result.Failure<IEnumerable<ADGroupDto>>($"Error getting groups: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ADGroupDto>>> SearchGroupsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _logger.LogInformation("Search term is empty, returning all groups");
                return await GetAllGroupsAsync(cancellationToken);
            }

            _logger.LogInformation("Searching groups with term: {SearchTerm}", searchTerm);

            var result = await _repository.SearchGroupsAsync(searchTerm, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogWarning("Failed to search groups: {Message}", result.Message);
                return Result.Failure<IEnumerable<ADGroupDto>>(result.Message);
            }

            var groupDtos = result.Value.Select(ADGroupMapper.ToDto).ToList();

            _logger.LogInformation("Found {Count} groups matching '{SearchTerm}'", groupDtos.Count, searchTerm);
            return Result.Success<IEnumerable<ADGroupDto>>(groupDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching groups with term: {SearchTerm}", searchTerm);
            return Result.Failure<IEnumerable<ADGroupDto>>($"Error searching groups: {ex.Message}");
        }
    }

    public async Task<Result<ADGroupDto>> GetGroupByNameAsync(string groupName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return Result.Failure<ADGroupDto>("Group name is required");
            }

            _logger.LogInformation("Getting group by name: {GroupName}", groupName);

            var result = await _repository.GetGroupByNameAsync(groupName, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogWarning("Failed to get group {GroupName}: {Message}", groupName, result.Message);
                return Result.Failure<ADGroupDto>(result.Message);
            }

            var groupDto = ADGroupMapper.ToDto(result.Value);

            _logger.LogInformation("Successfully retrieved group: {GroupName}", groupName);
            return Result<ADGroupDto>.Success(groupDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group by name: {GroupName}", groupName);
            return Result.Failure<ADGroupDto>($"Error getting group: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ADGroupDto>>> GetUserGroupsAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Result.Failure<IEnumerable<ADGroupDto>>("Username is required");
            }

            _logger.LogInformation("Getting groups for user: {Username}", username);

            var result = await _repository.GetUserGroupsAsync(username, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogWarning("Failed to get groups for user {Username}: {Message}", username, result.Message);
                return Result.Failure<IEnumerable<ADGroupDto>>(result.Message);
            }

            var groupDtos = result.Value.Select(ADGroupMapper.ToDto).ToList();

            _logger.LogInformation("User {Username} is member of {Count} groups", username, groupDtos.Count);
            return Result.Success<IEnumerable<ADGroupDto>>(groupDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups for user: {Username}", username);
            return Result.Failure<IEnumerable<ADGroupDto>>($"Error getting user groups: {ex.Message}");
        }
    }

    public async Task<Result> AddUserToGroupAsync(string username, string groupName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Result.Failure("Username is required");
            }

            if (string.IsNullOrWhiteSpace(groupName))
            {
                return Result.Failure("Group name is required");
            }

            _logger.LogInformation("Adding user {Username} to group {GroupName}", username, groupName);

            var result = await _repository.AddUserToGroupAsync(username, groupName, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully added user {Username} to group {GroupName}", username, groupName);
            }
            else
            {
                _logger.LogWarning("Failed to add user {Username} to group {GroupName}: {Message}",
                    username, groupName, result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {Username} to group {GroupName}", username, groupName);
            return Result.Failure($"Error adding user to group: {ex.Message}");
        }
    }

    public async Task<Result> RemoveUserFromGroupAsync(string username, string groupName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Result.Failure("Username is required");
            }

            if (string.IsNullOrWhiteSpace(groupName))
            {
                return Result.Failure("Group name is required");
            }

            _logger.LogInformation("Removing user {Username} from group {GroupName}", username, groupName);

            var result = await _repository.RemoveUserFromGroupAsync(username, groupName, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully removed user {Username} from group {GroupName}", username, groupName);
            }
            else
            {
                _logger.LogWarning("Failed to remove user {Username} from group {GroupName}: {Message}",
                    username, groupName, result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {Username} from group {GroupName}", username, groupName);
            return Result.Failure($"Error removing user from group: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ADUserDto>>> GetGroupMembersAsync(string groupName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return Result.Failure<IEnumerable<ADUserDto>>("Group name is required");
            }

            _logger.LogInformation("Getting members of group: {GroupName}", groupName);

            var result = await _repository.GetGroupMembersAsync(groupName, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogWarning("Failed to get members of group {GroupName}: {Message}", groupName, result.Message);
                return Result.Failure<IEnumerable<ADUserDto>>(result.Message);
            }

            var userDtos = result.Value.Select(ADUserDtoMapper.ToDto).ToList();

            _logger.LogInformation("Group {GroupName} has {Count} members", groupName, userDtos.Count);
            return Result.Success<IEnumerable<ADUserDto>>(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting members of group: {GroupName}", groupName);
            return Result.Failure<IEnumerable<ADUserDto>>($"Error getting group members: {ex.Message}");
        }
    }

    public async Task<Result<ADGroupDto>> CreateGroupAsync(
        string groupName,
        string description,
        string groupScope,
        string groupType,
        string organizationalUnit = "",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return Result.Failure<ADGroupDto>("Group name is required");
            }

            _logger.LogInformation("Creating group: {GroupName}", groupName);

            var result = await _repository.CreateGroupAsync(
                groupName, description, groupScope, groupType, organizationalUnit, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogWarning("Failed to create group {GroupName}: {Message}", groupName, result.Message);
                return Result.Failure<ADGroupDto>(result.Message);
            }

            var groupDto = ADGroupMapper.ToDto(result.Value);

            _logger.LogInformation("Successfully created group: {GroupName}", groupName);
            return Result<ADGroupDto>.Success(groupDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group: {GroupName}", groupName);
            return Result.Failure<ADGroupDto>($"Error creating group: {ex.Message}");
        }
    }

    public async Task<Result> DeleteGroupAsync(string groupName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return Result.Failure("Group name is required");
            }

            _logger.LogInformation("Deleting group: {GroupName}", groupName);

            var result = await _repository.DeleteGroupAsync(groupName, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully deleted group: {GroupName}", groupName);
            }
            else
            {
                _logger.LogWarning("Failed to delete group {GroupName}: {Message}", groupName, result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group: {GroupName}", groupName);
            return Result.Failure($"Error deleting group: {ex.Message}");
        }
    }

    public async Task<Result> UpdateGroupAsync(ADGroupDto group, CancellationToken cancellationToken = default)
    {
        try
        {
            if (group == null)
            {
                return Result.Failure("Group data is required");
            }

            if (string.IsNullOrWhiteSpace(group.Name))
            {
                return Result.Failure("Group name is required");
            }

            _logger.LogInformation("Updating group: {GroupName}", group.Name);

            var domainGroup = ADGroupMapper.ToEntity(group);
            var result = await _repository.UpdateGroupAsync(domainGroup, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully updated group: {GroupName}", group.Name);
            }
            else
            {
                _logger.LogWarning("Failed to update group {GroupName}: {Message}", group.Name, result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group: {GroupName}", group?.Name);
            return Result.Failure($"Error updating group: {ex.Message}");
        }
    }
}

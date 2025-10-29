using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.Application.Mappings;
using ADManagement.Application.Validators;
using ADManagement.Domain.Common;
using ADManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ADManagement.Application.Services;

/// <summary>
/// Service implementation for AD User operations
/// </summary>
public class ADUserService : IADUserService
{
    private readonly IADRepository _repository;
    private readonly ILogger<ADUserService> _logger;
    private readonly PasswordChangeRequestValidator _passwordValidator;
    
    public ADUserService(
        IADRepository repository,
        ILogger<ADUserService> logger)
    {
        _repository = repository;
        _logger = logger;
        _passwordValidator = new PasswordChangeRequestValidator();
    }
    
    public async Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing Active Directory connection");
        return await _repository.TestConnectionAsync(cancellationToken);
    }
    
    public async Task<Result<IEnumerable<ADUserDto>>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all users");
        
        var result = await _repository.GetAllUsersAsync(cancellationToken);
        
        if (!result.IsSuccess || result.Value == null)
        {
            return Result.Failure<IEnumerable<ADUserDto>>(result.Message, result.Errors);
        }
        
        var dtos = result.Value.ToDto();
        return Result.Success(dtos, result.Message);
    }
    
    public async Task<Result<IEnumerable<ADUserDto>>> GetUsersByOUAsync(string ouPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting users from OU: {OUPath}", ouPath);
        
        if (string.IsNullOrWhiteSpace(ouPath))
        {
            return Result.Failure<IEnumerable<ADUserDto>>("OU path cannot be empty");
        }
        
        var result = await _repository.GetUsersByOUAsync(ouPath, cancellationToken);
        
        if (!result.IsSuccess || result.Value == null)
        {
            return Result.Failure<IEnumerable<ADUserDto>>(result.Message, result.Errors);
        }
        
        var dtos = result.Value.ToDto();
        return Result.Success(dtos, result.Message);
    }
    
    public async Task<Result<ADUserDto>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user: {Username}", username);
        
        if (string.IsNullOrWhiteSpace(username))
        {
            return Result.Failure<ADUserDto>("Username cannot be empty");
        }
        
        var result = await _repository.GetUserByUsernameAsync(username, cancellationToken);
        
        if (!result.IsSuccess || result.Value == null)
        {
            return Result.Failure<ADUserDto>(result.Message, result.Errors);
        }
        
        var dto = result.Value.ToDto();
        return Result.Success(dto, result.Message);
    }
    
    public async Task<Result<ADUserDto>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user by email: {Email}", email);
        
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure<ADUserDto>("Email cannot be empty");
        }
        
        var result = await _repository.GetUserByEmailAsync(email, cancellationToken);
        
        if (!result.IsSuccess || result.Value == null)
        {
            return Result.Failure<ADUserDto>(result.Message, result.Errors);
        }
        
        var dto = result.Value.ToDto();
        return Result.Success(dto, result.Message);
    }
    
    public async Task<Result<IEnumerable<ADUserDto>>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching users: {SearchTerm}", searchTerm);
        
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Result.Failure<IEnumerable<ADUserDto>>("Search term cannot be empty");
        }
        
        var result = await _repository.SearchUsersAsync(searchTerm, cancellationToken);
        
        if (!result.IsSuccess || result.Value == null)
        {
            return Result.Failure<IEnumerable<ADUserDto>>(result.Message, result.Errors);
        }
        
        var dtos = result.Value.ToDto();
        return Result.Success(dtos, result.Message);
    }
    
    public async Task<Result> ChangePasswordAsync(PasswordChangeRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Changing password for user: {Username}", request.Username);
        
        // Validate request
        var validationResult = await _passwordValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result.Failure("Password change validation failed", errors);
        }
        
        return await _repository.ChangePasswordAsync(request.Username, request.NewPassword, cancellationToken);
    }
    
    public async Task<Result> EnableUserAsync(string username, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enabling user: {Username}", username);
        
        if (string.IsNullOrWhiteSpace(username))
        {
            return Result.Failure("Username cannot be empty");
        }
        
        return await _repository.SetUserStatusAsync(username, true, cancellationToken);
    }
    
    public async Task<Result> DisableUserAsync(string username, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Disabling user: {Username}", username);
        
        if (string.IsNullOrWhiteSpace(username))
        {
            return Result.Failure("Username cannot be empty");
        }
        
        return await _repository.SetUserStatusAsync(username, false, cancellationToken);
    }
    
    public async Task<Result> UnlockUserAsync(string username, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unlocking user: {Username}", username);
        
        if (string.IsNullOrWhiteSpace(username))
        {
            return Result.Failure("Username cannot be empty");
        }
        
        return await _repository.UnlockUserAsync(username, cancellationToken);
    }
    
    public async Task<Result<IEnumerable<string>>> GetUserGroupsAsync(string username, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting groups for user: {Username}", username);
        
        if (string.IsNullOrWhiteSpace(username))
        {
            return Result.Failure<IEnumerable<string>>("Username cannot be empty");
        }
        
        return await _repository.GetUserGroupsAsync(username, cancellationToken);
    }
    
    public async Task<Result> AddUserToGroupAsync(string username, string groupName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding user {Username} to group {GroupName}", username, groupName);
        
        if (string.IsNullOrWhiteSpace(username))
        {
            return Result.Failure("Username cannot be empty");
        }
        
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return Result.Failure("Group name cannot be empty");
        }
        
        return await _repository.AddUserToGroupAsync(username, groupName, cancellationToken);
    }
    
    public async Task<Result> RemoveUserFromGroupAsync(string username, string groupName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing user {Username} from group {GroupName}", username, groupName);
        
        if (string.IsNullOrWhiteSpace(username))
        {
            return Result.Failure("Username cannot be empty");
        }
        
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return Result.Failure("Group name cannot be empty");
        }
        
        return await _repository.RemoveUserFromGroupAsync(username, groupName, cancellationToken);
    }
}
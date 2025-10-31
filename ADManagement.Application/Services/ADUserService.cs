using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.Application.Mappings;
using ADManagement.Application.Validators;
using ADManagement.Domain.Common;
using ADManagement.Domain.Entities;
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
        
        return await _repository.ChangePasswordAsync(request.Username, request.NewPassword, request.MustChangeAtNextLogon, cancellationToken);
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
    
    public async Task<Result<ADUserDto>> CreateUserAsync(DTOs.CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new user: {Username}", request.Username);
        
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return Result.Failure<ADUserDto>("Username is required");
        }
        
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return Result.Failure<ADUserDto>("First name and last name are required");
        }
        
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result.Failure<ADUserDto>("Password is required");
        }
        
        var result = await _repository.CreateUserAsync(
            request.Username,
            request.FirstName,
            request.LastName,
            request.Password,
            request.OrganizationalUnit,
            request.DisplayName,
            request.Email,
            request.Department,
            request.Title,
            request.Company,
            request.Office,
            request.PhoneNumber,
            request.Description,
            request.MustChangePasswordOnNextLogon,
            request.AccountEnabled,
            request.PasswordNeverExpires,
            request.InitialGroups,
            cancellationToken);
        
        if (!result.IsSuccess || result.Value == null)
        {
            return Result.Failure<ADUserDto>(result.Message, result.Errors);
        }
        
        var dto = result.Value.ToDto();
        return Result.Success(dto, result.Message);
    }
    public async Task<Result> UpdateUserAsync(ADUserDto user, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating user {Username}", user.SamAccountName);

            // Gọi repository để update AD
            var updateResult = await _repository.UpdateUserAsync(ADUserDtoMapper.ToEntity(user), cancellationToken);

            if (!updateResult.IsSuccess)
            {
                _logger.LogWarning("Failed to update user {Username}: {Message}", user.SamAccountName, updateResult.Message);
                return Result.Failure(updateResult.Message);
            }

            _logger.LogInformation("User {Username} updated successfully", user.SamAccountName);
            return Result.Success($"User {user.SamAccountName} updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {Username}", user.SamAccountName);
            return Result.Failure($"Error updating user: {ex.Message}");
        }
    }

}
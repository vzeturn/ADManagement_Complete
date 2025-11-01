using ADManagement.Application.DTOs;
using ADManagement.Domain.Common;
using System.Runtime.CompilerServices;

namespace ADManagement.Application.Interfaces;

/// <summary>
/// ✨ OPTIMIZED Service interface for AD User operations
/// </summary>
public interface IADUserService
{
    Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ADUserDto>>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ✨ NEW: Stream users asynchronously
    /// </summary>
    IAsyncEnumerable<ADUserDto> StreamUsersAsync(CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<ADUserDto>>> GetUsersByOUAsync(string ouPath, CancellationToken cancellationToken = default);
    Task<Result<ADUserDto>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<Result<ADUserDto>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ADUserDto>>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(PasswordChangeRequest request, CancellationToken cancellationToken = default);
    Task<Result> EnableUserAsync(string username, CancellationToken cancellationToken = default);
    Task<Result> DisableUserAsync(string username, CancellationToken cancellationToken = default);
    Task<Result> UnlockUserAsync(string username, CancellationToken cancellationToken = default);
    Task<Result<ADUserDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateUserAsync(ADUserDto user, CancellationToken cancellationToken = default);
}
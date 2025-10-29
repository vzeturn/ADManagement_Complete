using ADManagement.Domain.Common;

namespace ADManagement.Application.Interfaces;

/// <summary>
/// Service interface for export operations
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports all users to Excel
    /// </summary>
    Task<Result<string>> ExportAllUsersAsync(string? outputPath = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Exports users from a specific OU to Excel
    /// </summary>
    Task<Result<string>> ExportUsersByOUAsync(string ouPath, string? outputPath = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Smart update - updates existing Excel file with new data
    /// </summary>
    Task<Result<string>> SmartUpdateUsersAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Exports all groups to Excel
    /// </summary>
    Task<Result<string>> ExportAllGroupsAsync(string? outputPath = null, CancellationToken cancellationToken = default);
}
using ADManagement.Application.Configuration;
using ADManagement.Application.Interfaces;
using ADManagement.Application.Mappings;
using ADManagement.Domain.Common;
using ADManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ADManagement.Application.Services;

/// <summary>
/// Service implementation for export operations
/// </summary>
public class ExportService : IExportService
{
    private readonly IADRepository _adRepository;
    private readonly IExcelExporter _excelExporter;
    private readonly ExportConfiguration _config;
    private readonly ILogger<ExportService> _logger;
    
    public ExportService(
        IADRepository adRepository,
        IExcelExporter excelExporter,
        ExportConfiguration config,
        ILogger<ExportService> logger)
    {
        _adRepository = adRepository;
        _excelExporter = excelExporter;
        _config = config;
        _logger = logger;
    }
    
    public async Task<Result<string>> ExportAllUsersAsync(string? outputPath = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting export of all users");
            
            // Get all users from AD
            var usersResult = await _adRepository.GetAllUsersAsync(cancellationToken);
            
            if (!usersResult.IsSuccess || usersResult.Value == null)
            {
                _logger.LogWarning("Failed to retrieve users: {Message}", usersResult.Message);
                return Result.Failure<string>(usersResult.Message, usersResult.Errors);
            }
            
            var users = usersResult.Value.ToList();
            
            // Filter disabled accounts if configured
            if (!_config.IncludeDisabledAccounts)
            {
                users = users.Where(u => u.IsEnabled).ToList();
                _logger.LogInformation("Filtered out {Count} disabled accounts", 
                    usersResult.Value.Count() - users.Count);
            }
            
            // Get output path
            var filePath = _config.GetExportFilePath(outputPath);
            
            // Export to Excel
            var exportResult = await _excelExporter.ExportUsersAsync(users, filePath, cancellationToken);
            
            if (!exportResult.IsSuccess)
            {
                return Result.Failure<string>(exportResult.Message, exportResult.Errors);
            }
            
            _logger.LogInformation("Successfully exported {Count} users to {FilePath}", users.Count, filePath);
            return Result.Success(filePath, $"Exported {users.Count} users successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users");
            return Result.Failure<string>($"Export failed: {ex.Message}");
        }
    }
    
    public async Task<Result<string>> ExportUsersByOUAsync(string ouPath, string? outputPath = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting export of users from OU: {OUPath}", ouPath);
            
            if (string.IsNullOrWhiteSpace(ouPath))
            {
                return Result.Failure<string>("OU path cannot be empty");
            }
            
            // Get users from specific OU
            var usersResult = await _adRepository.GetUsersByOUAsync(ouPath, cancellationToken);
            
            if (!usersResult.IsSuccess || usersResult.Value == null)
            {
                _logger.LogWarning("Failed to retrieve users from OU: {Message}", usersResult.Message);
                return Result.Failure<string>(usersResult.Message, usersResult.Errors);
            }
            
            var users = usersResult.Value.ToList();
            
            // Filter disabled accounts if configured
            if (!_config.IncludeDisabledAccounts)
            {
                users = users.Where(u => u.IsEnabled).ToList();
            }
            
            // Get output path
            var filePath = _config.GetExportFilePath(outputPath);
            
            // Export to Excel
            var exportResult = await _excelExporter.ExportUsersAsync(users, filePath, cancellationToken);
            
            if (!exportResult.IsSuccess)
            {
                return Result.Failure<string>(exportResult.Message, exportResult.Errors);
            }
            
            _logger.LogInformation("Successfully exported {Count} users from OU to {FilePath}", users.Count, filePath);
            return Result.Success(filePath, $"Exported {users.Count} users from OU successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users from OU");
            return Result.Failure<string>($"Export failed: {ex.Message}");
        }
    }
    
    public async Task<Result<string>> SmartUpdateUsersAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting smart update of file: {FilePath}", filePath);
            
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Result.Failure<string>("File path cannot be empty");
            }
            
            if (!File.Exists(filePath))
            {
                return Result.Failure<string>("File does not exist");
            }
            
            // Get all users from AD
            var usersResult = await _adRepository.GetAllUsersAsync(cancellationToken);
            
            if (!usersResult.IsSuccess || usersResult.Value == null)
            {
                _logger.LogWarning("Failed to retrieve users: {Message}", usersResult.Message);
                return Result.Failure<string>(usersResult.Message, usersResult.Errors);
            }
            
            var users = usersResult.Value.ToList();
            
            // Filter disabled accounts if configured
            if (!_config.IncludeDisabledAccounts)
            {
                users = users.Where(u => u.IsEnabled).ToList();
            }
            
            // Perform smart update
            var updateResult = await _excelExporter.SmartUpdateUsersAsync(users, filePath, cancellationToken);
            
            if (!updateResult.IsSuccess)
            {
                return Result.Failure<string>(updateResult.Message, updateResult.Errors);
            }
            
            _logger.LogInformation("Successfully performed smart update on {FilePath}", filePath);
            return Result.Success(filePath, updateResult.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing smart update");
            return Result.Failure<string>($"Smart update failed: {ex.Message}");
        }
    }
    
    public async Task<Result<string>> ExportAllGroupsAsync(string? outputPath = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting export of all groups");
            
            // Get all groups from AD
            var groupsResult = await _adRepository.GetAllGroupsAsync(cancellationToken);
            
            if (!groupsResult.IsSuccess || groupsResult.Value == null)
            {
                _logger.LogWarning("Failed to retrieve groups: {Message}", groupsResult.Message);
                return Result.Failure<string>(groupsResult.Message, groupsResult.Errors);
            }
            
            var groups = groupsResult.Value.ToList();
            
            // Get output path (modify filename for groups)
            var filePath = outputPath ?? _config.GetExportFilePath(null);
            filePath = filePath.Replace("ADExport", "ADGroups");
            
            // Export to Excel
            var exportResult = await _excelExporter.ExportGroupsAsync(groups, filePath, cancellationToken);
            
            if (!exportResult.IsSuccess)
            {
                return Result.Failure<string>(exportResult.Message, exportResult.Errors);
            }
            
            _logger.LogInformation("Successfully exported {Count} groups to {FilePath}", groups.Count, filePath);
            return Result.Success(filePath, $"Exported {groups.Count} groups successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting groups");
            return Result.Failure<string>($"Export failed: {ex.Message}");
        }
    }
}
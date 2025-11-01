using ADManagement.Application.Configuration;
using ADManagement.Application.Interfaces;
using ADManagement.Domain.Common;
using ADManagement.Domain.Entities;
using ADManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ADManagement.Application.Services;

/// <summary>
/// ✨ OPTIMIZED Service implementation for export operations
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

    /// <summary>
    /// ✨ OPTIMIZED: Export with streaming for memory efficiency
    /// </summary>
    public async Task<Result<string>> ExportAllUsersAsync(
        string? outputPath = null,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting streaming export of all users");

            var filePath = _config.GetExportFilePath(outputPath);

            // Stream users directly to Excel
            var users = _adRepository.StreamUsersAsync(cancellationToken);

            // Filter disabled accounts if configured
            IAsyncEnumerable<ADUser> filteredUsers = users;

            if (!_config.IncludeDisabledAccounts)
            {
                filteredUsers = FilterDisabledUsersAsync(users);
            }

            // Export with streaming
            var exportResult = await _excelExporter.ExportUsersStreamAsync(
                filteredUsers,
                filePath,
                progress,
                cancellationToken);

            return exportResult.IsSuccess
                ? Result.Success(filePath, exportResult.Message)
                : Result.Failure<string>(exportResult.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming export failed");
            return Result.Failure<string>($"Export failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> ExportUsersByOUAsync(
        string ouPath,
        string? outputPath = null,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting export of users from OU: {OUPath}", ouPath);

            if (string.IsNullOrWhiteSpace(ouPath))
            {
                return Result.Failure<string>("OU path cannot be empty");
            }

            var filePath = _config.GetExportFilePath(outputPath);

            // Stream users from OU
            var users = _adRepository.StreamUsersByOUAsync(ouPath, cancellationToken);

            // Filter disabled accounts if configured
            IAsyncEnumerable<ADUser> filteredUsers = users;

            if (!_config.IncludeDisabledAccounts)
            {
                filteredUsers = FilterDisabledUsersAsync(users);
            }

            // Export with streaming
            var exportResult = await _excelExporter.ExportUsersStreamAsync(
                filteredUsers,
                filePath,
                progress,
                cancellationToken);

            return exportResult.IsSuccess
                ? Result.Success(filePath, exportResult.Message)
                : Result.Failure<string>(exportResult.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export from OU failed");
            return Result.Failure<string>($"Export failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> SmartUpdateUsersAsync(
        string filePath,
        CancellationToken cancellationToken = default)
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

            return updateResult.IsSuccess
                ? Result.Success(filePath, updateResult.Message)
                : Result.Failure<string>(updateResult.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smart update failed");
            return Result.Failure<string>($"Smart update failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> ExportAllGroupsAsync(
        string? outputPath = null,
        CancellationToken cancellationToken = default)
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

            return exportResult.IsSuccess
                ? Result.Success(filePath, exportResult.Message)
                : Result.Failure<string>(exportResult.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export groups failed");
            return Result.Failure<string>($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// ✨ NEW: Filter disabled users from stream
    /// </summary>
    private async IAsyncEnumerable<ADUser> FilterDisabledUsersAsync(
        IAsyncEnumerable<ADUser> users)
    {
        await foreach (var user in users)
        {
            if (user.IsEnabled)
            {
                yield return user;
            }
        }
    }
}
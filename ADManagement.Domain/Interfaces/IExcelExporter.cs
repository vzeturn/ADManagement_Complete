using ADManagement.Domain.Common;
using ADManagement.Domain.Entities;

namespace ADManagement.Domain.Interfaces;

/// <summary>
/// ✨ OPTIMIZED Interface for exporting data to Excel
/// </summary>
public interface IExcelExporter
{
    /// <summary>
    /// Exports users to a new Excel file
    /// </summary>
    Task<Result> ExportUsersAsync(
        IEnumerable<ADUser> users,
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ✨ NEW: Exports users from async stream (memory efficient)
    /// </summary>
    Task<Result> ExportUsersStreamAsync(
        IAsyncEnumerable<ADUser> users,
        string filePath,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing Excel file with new user data (smart update)
    /// </summary>
    Task<Result> SmartUpdateUsersAsync(
        IEnumerable<ADUser> users,
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports groups to a new Excel file
    /// </summary>
    Task<Result> ExportGroupsAsync(
        IEnumerable<ADGroup> groups,
        string filePath,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// ✨ NEW: Progress information for export operations
/// </summary>
public class ExportProgress
{
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public TimeSpan ElapsedTime { get; set; }
    public double PercentComplete => TotalCount > 0
        ? (double)ProcessedCount / TotalCount * 100
        : 0;
}
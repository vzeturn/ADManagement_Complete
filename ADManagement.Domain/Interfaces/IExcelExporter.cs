using ADManagement.Domain.Common;
using ADManagement.Domain.Entities;

namespace ADManagement.Domain.Interfaces;

/// <summary>
/// Interface for exporting data to Excel
/// </summary>
public interface IExcelExporter
{
    /// <summary>
    /// Exports users to a new Excel file
    /// </summary>
    Task<Result> ExportUsersAsync(IEnumerable<ADUser> users, string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing Excel file with new user data (smart update)
    /// </summary>
    Task<Result> SmartUpdateUsersAsync(IEnumerable<ADUser> users, string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Exports groups to a new Excel file
    /// </summary>
    Task<Result> ExportGroupsAsync(IEnumerable<ADGroup> groups, string filePath, CancellationToken cancellationToken = default);
}

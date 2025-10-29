using ADManagement.Application.Configuration;
using ADManagement.Domain.Common;
using ADManagement.Domain.Entities;
using ADManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace ADManagement.Infrastructure.Exporters;

/// <summary>
/// Excel exporter implementation using EPPlus
/// </summary>
public class ExcelExporter : IExcelExporter
{
    private readonly ExportConfiguration _config;
    private readonly ILogger<ExcelExporter> _logger;
    
    public ExcelExporter(ExportConfiguration config, ILogger<ExcelExporter> logger)
    {
        _config = config;
        _logger = logger;
        
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }
    
    public async Task<Result> ExportUsersAsync(IEnumerable<ADUser> users, string filePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Exporting {Count} users to {FilePath}", users.Count(), filePath);
                
                var userList = users.ToList();
                
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("AD Users");
                
                // Define headers
                var headers = new[]
                {
                    "Username", "Display Name", "Email", "First Name", "Last Name",
                    "Department", "Title", "Company", "Office", "Manager",
                    "Phone Number", "Mobile Number", "Fax Number",
                    "Street Address", "City", "State", "Postal Code", "Country",
                    "Distinguished Name", "Account Status", "Is Enabled", "Is Locked Out",
                    "Password Never Expires", "Must Change Password On Next Logon",
                    "Last Logon", "Last Password Set", "Account Expires",
                    "When Created", "When Changed", "Description", "Groups"
                };
                
                // Write headers
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                }
                
                // Style headers
                using (var range = worksheet.Cells[1, 1, 1, headers.Length])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    range.Style.Font.Color.SetColor(Color.White);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                
                // Write data
                int row = 2;
                foreach (var user in userList)
                {
                    worksheet.Cells[row, 1].Value = user.Username;
                    worksheet.Cells[row, 2].Value = user.DisplayName;
                    worksheet.Cells[row, 3].Value = user.Email;
                    worksheet.Cells[row, 4].Value = user.FirstName;
                    worksheet.Cells[row, 5].Value = user.LastName;
                    worksheet.Cells[row, 6].Value = user.Department;
                    worksheet.Cells[row, 7].Value = user.Title;
                    worksheet.Cells[row, 8].Value = user.Company;
                    worksheet.Cells[row, 9].Value = user.Office;
                    worksheet.Cells[row, 10].Value = user.Manager;
                    worksheet.Cells[row, 11].Value = user.PhoneNumber;
                    worksheet.Cells[row, 12].Value = user.MobileNumber;
                    worksheet.Cells[row, 13].Value = user.FaxNumber;
                    worksheet.Cells[row, 14].Value = user.StreetAddress;
                    worksheet.Cells[row, 15].Value = user.City;
                    worksheet.Cells[row, 16].Value = user.State;
                    worksheet.Cells[row, 17].Value = user.PostalCode;
                    worksheet.Cells[row, 18].Value = user.Country;
                    worksheet.Cells[row, 19].Value = user.DistinguishedName;
                    worksheet.Cells[row, 20].Value = user.AccountStatus;
                    worksheet.Cells[row, 21].Value = user.IsEnabled ? "Yes" : "No";
                    worksheet.Cells[row, 22].Value = user.IsLockedOut ? "Yes" : "No";
                    worksheet.Cells[row, 23].Value = user.PasswordNeverExpires ? "Yes" : "No";
                    worksheet.Cells[row, 24].Value = user.MustChangePasswordOnNextLogon ? "Yes" : "No";
                    worksheet.Cells[row, 25].Value = user.LastLogon?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    worksheet.Cells[row, 26].Value = user.LastPasswordSet?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    worksheet.Cells[row, 27].Value = user.AccountExpires?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    worksheet.Cells[row, 28].Value = user.WhenCreated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    worksheet.Cells[row, 29].Value = user.WhenChanged?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    worksheet.Cells[row, 30].Value = user.Description;
                    worksheet.Cells[row, 31].Value = string.Join(", ", user.MemberOf);
                    
                    row++;
                }
                
                // Auto-fit columns
                if (_config.AutoFitColumns)
                {
                    worksheet.Cells.AutoFitColumns();
                }
                
                // Create table
                if (_config.CreateTable)
                {
                    var tableRange = worksheet.Cells[1, 1, row - 1, headers.Length];
                    var table = worksheet.Tables.Add(tableRange, "ADUsersTable");
                    table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium2;
                }
                
                // Add filters
                worksheet.Cells[1, 1, 1, headers.Length].AutoFilter = true;
                
                // Freeze header row
                worksheet.View.FreezePanes(2, 1);
                
                // Save file
                var fileInfo = new FileInfo(filePath);
                package.SaveAs(fileInfo);
                
                _logger.LogInformation("Successfully exported {Count} users to {FilePath}", userList.Count, filePath);
                return Result.Success($"Exported {userList.Count} users successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users to {FilePath}", filePath);
                return Result.Failure($"Export failed: {ex.Message}");
            }
        }, cancellationToken);
    }
    
    public async Task<Result> SmartUpdateUsersAsync(IEnumerable<ADUser> users, string filePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Performing smart update on {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File does not exist, creating new file: {FilePath}", filePath);
                    return ExportUsersAsync(users, filePath, cancellationToken).Result;
                }
                
                var userList = users.ToList();
                var fileInfo = new FileInfo(filePath);
                
                using var package = new ExcelPackage(fileInfo);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                
                if (worksheet == null)
                {
                    _logger.LogWarning("No worksheets found, creating new export");
                    return ExportUsersAsync(users, filePath, cancellationToken).Result;
                }
                
                // Create a dictionary of existing users (username -> row)
                var existingUsers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int lastRow = worksheet.Dimension?.End.Row ?? 1;
                
                for (int row = 2; row <= lastRow; row++)
                {
                    var username = worksheet.Cells[row, 1].Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        existingUsers[username] = row;
                    }
                }
                
                int updatedCount = 0;
                int addedCount = 0;
                int currentRow = lastRow + 1;
                
                foreach (var user in userList)
                {
                    int row;
                    
                    if (existingUsers.TryGetValue(user.Username, out row))
                    {
                        // Update existing user
                        updatedCount++;
                    }
                    else
                    {
                        // Add new user
                        row = currentRow++;
                        addedCount++;
                    }
                    
                    // Update/Add user data
                    worksheet.Cells[row, 1].Value = user.Username;
                    worksheet.Cells[row, 2].Value = user.DisplayName;
                    worksheet.Cells[row, 3].Value = user.Email;
                    worksheet.Cells[row, 4].Value = user.FirstName;
                    worksheet.Cells[row, 5].Value = user.LastName;
                    worksheet.Cells[row, 6].Value = user.Department;
                    worksheet.Cells[row, 7].Value = user.Title;
                    worksheet.Cells[row, 8].Value = user.Company;
                    worksheet.Cells[row, 9].Value = user.Office;
                    worksheet.Cells[row, 10].Value = user.Manager;
                    worksheet.Cells[row, 11].Value = user.PhoneNumber;
                    worksheet.Cells[row, 12].Value = user.MobileNumber;
                    worksheet.Cells[row, 13].Value = user.FaxNumber;
                    worksheet.Cells[row, 14].Value = user.StreetAddress;
                    worksheet.Cells[row, 15].Value = user.City;
                    worksheet.Cells[row, 16].Value = user.State;
                    worksheet.Cells[row, 17].Value = user.PostalCode;
                    worksheet.Cells[row, 18].Value = user.Country;
                    worksheet.Cells[row, 19].Value = user.DistinguishedName;
                    worksheet.Cells[row, 20].Value = user.AccountStatus;
                    worksheet.Cells[row, 21].Value = user.IsEnabled ? "Yes" : "No";
                    worksheet.Cells[row, 22].Value = user.IsLockedOut ? "Yes" : "No";
                    worksheet.Cells[row, 23].Value = user.PasswordNeverExpires ? "Yes" : "No";
                    worksheet.Cells[row, 24].Value = user.MustChangePasswordOnNextLogon ? "Yes" : "No";
                    worksheet.Cells[row, 25].Value = user.LastLogon?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    worksheet.Cells[row, 26].Value = user.LastPasswordSet?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    worksheet.Cells[row, 27].Value = user.AccountExpires?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    worksheet.Cells[row, 28].Value = user.WhenCreated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    worksheet.Cells[row, 29].Value = user.WhenChanged?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    worksheet.Cells[row, 30].Value = user.Description;
                    worksheet.Cells[row, 31].Value = string.Join(", ", user.MemberOf);
                }
                
                // Auto-fit columns
                if (_config.AutoFitColumns)
                {
                    worksheet.Cells.AutoFitColumns();
                }
                
                // Save file
                package.Save();
                
                _logger.LogInformation("Smart update completed: {Updated} updated, {Added} added", updatedCount, addedCount);
                return Result.Success($"Smart update completed: {updatedCount} updated, {addedCount} added");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing smart update on {FilePath}", filePath);
                return Result.Failure($"Smart update failed: {ex.Message}");
            }
        }, cancellationToken);
    }
    
    public async Task<Result> ExportGroupsAsync(IEnumerable<ADGroup> groups, string filePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Exporting {Count} groups to {FilePath}", groups.Count(), filePath);
                
                var groupList = groups.ToList();
                
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("AD Groups");
                
                // Define headers
                var headers = new[]
                {
                    "Name", "Display Name", "Description", "Distinguished Name",
                    "Group Scope", "Group Type", "Member Count", "Members",
                    "When Created", "When Changed"
                };
                
                // Write headers
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                }
                
                // Style headers
                using (var range = worksheet.Cells[1, 1, 1, headers.Length])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    range.Style.Font.Color.SetColor(Color.White);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                
                // Write data
                int row = 2;
                foreach (var group in groupList)
                {
                    worksheet.Cells[row, 1].Value = group.Name;
                    worksheet.Cells[row, 2].Value = group.DisplayName;
                    worksheet.Cells[row, 3].Value = group.Description;
                    worksheet.Cells[row, 4].Value = group.DistinguishedName;
                    worksheet.Cells[row, 5].Value = group.GroupScope;
                    worksheet.Cells[row, 6].Value = group.GroupType;
                    worksheet.Cells[row, 7].Value = group.Members.Count;
                    worksheet.Cells[row, 8].Value = string.Join(", ", group.Members);
                    worksheet.Cells[row, 9].Value = group.WhenCreated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    worksheet.Cells[row, 10].Value = group.WhenChanged?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    
                    row++;
                }
                
                // Auto-fit columns
                if (_config.AutoFitColumns)
                {
                    worksheet.Cells.AutoFitColumns();
                }
                
                // Create table
                if (_config.CreateTable)
                {
                    var tableRange = worksheet.Cells[1, 1, row - 1, headers.Length];
                    var table = worksheet.Tables.Add(tableRange, "ADGroupsTable");
                    table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium2;
                }
                
                // Add filters
                worksheet.Cells[1, 1, 1, headers.Length].AutoFilter = true;
                
                // Freeze header row
                worksheet.View.FreezePanes(2, 1);
                
                // Save file
                var fileInfo = new FileInfo(filePath);
                package.SaveAs(fileInfo);
                
                _logger.LogInformation("Successfully exported {Count} groups to {FilePath}", groupList.Count, filePath);
                return Result.Success($"Exported {groupList.Count} groups successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting groups to {FilePath}", filePath);
                return Result.Failure($"Export failed: {ex.Message}");
            }
        }, cancellationToken);
    }
}

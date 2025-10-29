namespace ADManagement.Application.Configuration;

/// <summary>
/// Configuration for Excel export operations
/// </summary>
public class ExportConfiguration
{
    public const string SectionName = "ExportConfiguration";
    
    /// <summary>
    /// Output directory for export files
    /// </summary>
    public string OutputDirectory { get; set; } = "./Exports";
    
    /// <summary>
    /// Filename pattern (use {0} for timestamp)
    /// </summary>
    public string FilenamePattern { get; set; } = "ADExport_{0:yyyyMMdd_HHmmss}.xlsx";
    
    /// <summary>
    /// Include disabled accounts in export
    /// </summary>
    public bool IncludeDisabledAccounts { get; set; } = true;
    
    /// <summary>
    /// Include system accounts in export
    /// </summary>
    public bool IncludeSystemAccounts { get; set; } = false;
    
    /// <summary>
    /// Maximum rows per sheet (Excel limit: 1,048,576)
    /// </summary>
    public int MaxRowsPerSheet { get; set; } = 1_000_000;
    
    /// <summary>
    /// Auto-fit columns in Excel
    /// </summary>
    public bool AutoFitColumns { get; set; } = true;
    
    /// <summary>
    /// Apply formatting to Excel (headers, colors, etc.)
    /// </summary>
    public bool ApplyFormatting { get; set; } = true;
    
    /// <summary>
    /// Create Excel table with filters
    /// </summary>
    public bool CreateTable { get; set; } = true;
    
    /// <summary>
    /// Gets the full output path for a new export file
    /// </summary>
    public string GetExportFilePath(string? customPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            return customPath;
        }
        
        // Ensure output directory exists
        if (!Directory.Exists(OutputDirectory))
        {
            Directory.CreateDirectory(OutputDirectory);
        }
        
        var filename = string.Format(FilenamePattern, DateTime.Now);
        return Path.Combine(OutputDirectory, filename);
    }
}
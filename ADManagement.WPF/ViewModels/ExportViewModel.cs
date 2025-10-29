using ADManagement.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ADManagement.WPF.ViewModels;

public partial class ExportViewModel : ObservableObject
{
    private readonly IExportService _exportService;
    private readonly ILogger<ExportViewModel> _logger;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private string _statusMessage = "Ready to export";

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private bool _includeDisabledAccounts = true;

    [ObservableProperty]
    private bool _openFileAfterExport = true;

    public ExportViewModel(IExportService exportService, ILogger<ExportViewModel> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task ExportUsersAsync()
    {
        try
        {
            IsExporting = true;
            StatusMessage = "Exporting users...";

            string? filePath = null;

            if (!string.IsNullOrWhiteSpace(OutputPath))
            {
                filePath = OutputPath;
            }

            var result = await _exportService.ExportAllUsersAsync(filePath);

            if (result.IsSuccess)
            {
                StatusMessage = $"Export completed: {result.Value}";
                _logger.LogInformation("Users exported successfully to {Path}", result.Value);

                if (OpenFileAfterExport && !string.IsNullOrWhiteSpace(result.Value))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = result.Value,
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                StatusMessage = $"Export failed: {result.Message}";
                _logger.LogWarning("Export failed: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    private async Task ExportGroupsAsync()
    {
        try
        {
            IsExporting = true;
            StatusMessage = "Exporting groups...";

            string? filePath = null;

            if (!string.IsNullOrWhiteSpace(OutputPath))
            {
                filePath = OutputPath;
            }

            var result = await _exportService.ExportAllGroupsAsync(filePath);

            if (result.IsSuccess)
            {
                StatusMessage = $"Export completed: {result.Value}";
                _logger.LogInformation("Groups exported successfully to {Path}", result.Value);

                if (OpenFileAfterExport && !string.IsNullOrWhiteSpace(result.Value))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = result.Value,
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                StatusMessage = $"Export failed: {result.Message}";
                _logger.LogWarning("Export failed: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting groups");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    private void BrowseOutputPath()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
            DefaultExt = ".xlsx",
            FileName = $"ADExport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            OutputPath = dialog.FileName;
        }
    }
}

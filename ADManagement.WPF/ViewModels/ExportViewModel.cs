using ADManagement.Application.Interfaces;
using ADManagement.Domain.Interfaces;
using ADManagement.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ADManagement.WPF.ViewModels;

/// <summary>
/// ✨ OPTIMIZED ViewModel for Export View with progress feedback
/// </summary>
public partial class ExportViewModel : ObservableObject
{
    private readonly IExportService _exportService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ExportViewModel> _logger;

    // ✨ NEW: Cancellation support
    private CancellationTokenSource? _exportCancellation;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private string _ouPath = string.Empty;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private string _statusMessage = "Ready to export";

    [ObservableProperty]
    private string? _lastExportedFile;

    // ✨ NEW: Progress tracking
    [ObservableProperty]
    private double _exportProgress;

    [ObservableProperty]
    private string _exportProgressMessage = string.Empty;

    [ObservableProperty]
    private int _exportedCount;

    [ObservableProperty]
    private string _exportTimeElapsed = string.Empty;

    public ExportViewModel(
        IExportService exportService,
        IDialogService dialogService,
        ILogger<ExportViewModel> logger)
    {
        _exportService = exportService;
        _dialogService = dialogService;
        _logger = logger;

        // Set default output path
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        OutputPath = Path.Combine(desktopPath, "ADExport.xlsx");
    }

    [RelayCommand]
    private void BrowseOutputPath()
    {
        var filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
        var defaultFileName = "ADExport.xlsx";

        var path = _dialogService.ShowSaveFileDialog(filter, defaultFileName);
        if (!string.IsNullOrWhiteSpace(path))
        {
            OutputPath = path;
        }
    }

    /// <summary>
    /// ✨ OPTIMIZED: Export with progress feedback
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAllUsersAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            _dialogService.ShowWarning("Please specify an output path.", "Output Path Required");
            return;
        }

        // Cancel previous export
        _exportCancellation?.Cancel();
        _exportCancellation = new CancellationTokenSource();
        var cancellationToken = _exportCancellation.Token;

        try
        {
            IsExporting = true;
            ExportProgress = 0;
            ExportedCount = 0;
            ExportProgressMessage = "Starting export...";
            StatusMessage = "Exporting all users...";

            _logger.LogInformation("Starting export of all users to: {OutputPath}", OutputPath);

            // ✨ NEW: Progress reporter
            var progress = new Progress<ExportProgress>(p =>
            {
                ExportProgress = p.PercentComplete;
                ExportedCount = p.ProcessedCount;
                ExportProgressMessage = $"Exporting... {p.ProcessedCount:N0} users";
                ExportTimeElapsed = $"Time: {p.ElapsedTime.TotalSeconds:F0}s";
            });

            var result = await _exportService.ExportAllUsersAsync(
                OutputPath,
                progress,
                cancellationToken);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value))
            {
                LastExportedFile = result.Value;
                StatusMessage = $"Export completed: {Path.GetFileName(result.Value)}";
                ExportProgressMessage = "Export completed!";

                _dialogService.ShowSuccess(
                    $"Successfully exported users to:\n{result.Value}",
                    "Export Successful");

                _logger.LogInformation("Successfully exported users to: {FilePath}", result.Value);
            }
            else
            {
                StatusMessage = "Export failed";
                ExportProgressMessage = "Export failed";
                _dialogService.ShowError(result.Message, "Export Failed");
                _logger.LogWarning("Export failed: {Message}", result.Message);
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Export cancelled";
            ExportProgressMessage = "Cancelled";
            _logger.LogInformation("Export cancelled by user");
        }
        catch (Exception ex)
        {
            StatusMessage = "Error during export";
            ExportProgressMessage = "Error";
            _dialogService.ShowError($"Error exporting users: {ex.Message}", "Error");
            _logger.LogError(ex, "Error exporting users");
        }
        finally
        {
            IsExporting = false;
            ExportProgress = 0;
            ExportTimeElapsed = string.Empty;
        }
    }

    /// <summary>
    /// ✨ NEW: Cancel export operation
    /// </summary>
    [RelayCommand]
    private void CancelExport()
    {
        _exportCancellation?.Cancel();
        ExportProgressMessage = "Cancelling...";
    }

    private bool CanExport() => !IsExporting;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportGroupsAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            _dialogService.ShowWarning("Please specify an output path.", "Output Path Required");
            return;
        }

        try
        {
            IsExporting = true;
            StatusMessage = "Exporting groups...";

            _logger.LogInformation("Starting export of groups");

            var result = await _exportService.ExportAllGroupsAsync(OutputPath);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value))
            {
                LastExportedFile = result.Value;
                StatusMessage = $"Export completed: {Path.GetFileName(result.Value)}";

                _dialogService.ShowSuccess(
                    $"Successfully exported groups to:\n{result.Value}",
                    "Export Successful");

                _logger.LogInformation("Successfully exported groups to: {FilePath}", result.Value);
            }
            else
            {
                StatusMessage = "Export failed";
                _dialogService.ShowError(result.Message, "Export Failed");
                _logger.LogWarning("Export groups failed: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error during export";
            _dialogService.ShowError($"Error exporting groups: {ex.Message}", "Error");
            _logger.LogError(ex, "Error exporting groups");
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    private void OpenLastExportedFile()
    {
        if (string.IsNullOrWhiteSpace(LastExportedFile))
        {
            _dialogService.ShowInformation("No file has been exported yet.", "No File");
            return;
        }

        if (!File.Exists(LastExportedFile))
        {
            _dialogService.ShowWarning("The exported file no longer exists.", "File Not Found");
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = LastExportedFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error opening file: {ex.Message}", "Error");
            _logger.LogError(ex, "Error opening exported file: {FilePath}", LastExportedFile);
        }
    }
}
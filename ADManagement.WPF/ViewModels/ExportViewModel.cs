using ADManagement.Application.Interfaces;
using ADManagement.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ADManagement.WPF.ViewModels;

/// <summary>
/// ViewModel for Export View
/// </summary>
public partial class ExportViewModel : ObservableObject
{
    private readonly IExportService _exportService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ExportViewModel> _logger;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private bool _includeDisabledAccounts = true;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _lastExportedFile = string.Empty;

    public ExportViewModel(
        IExportService exportService,
        IDialogService dialogService,
        ILogger<ExportViewModel> logger)
    {
        _exportService = exportService;
        _dialogService = dialogService;
        _logger = logger;

        // Set default output path
        OutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ADExport.xlsx");
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

    [RelayCommand]
    private async Task ExportAllUsersAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            _dialogService.ShowWarning("Please specify an output path.", "Output Path Required");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Exporting all users...";

            _logger.LogInformation("Starting export of all users to: {OutputPath}", OutputPath);

            var result = await _exportService.ExportAllUsersAsync(OutputPath);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value))
            {
                LastExportedFile = result.Value;
                StatusMessage = $"Export completed: {Path.GetFileName(result.Value)}";
                
                _dialogService.ShowSuccess(
                    $"Successfully exported users to:\n{result.Value}", 
                    "Export Successful");
                
                _logger.LogInformation("Successfully exported users to: {FilePath}", result.Value);
            }
            else
            {
                StatusMessage = "Export failed";
                _dialogService.ShowError(result.Message, "Export Failed");
                _logger.LogWarning("Export failed: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error during export";
            _dialogService.ShowError($"Error exporting users: {ex.Message}", "Error");
            _logger.LogError(ex, "Error exporting users");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportGroupsAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            _dialogService.ShowWarning("Please specify an output path.", "Output Path Required");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Exporting all groups...";

            // Change filename to Groups
            var groupsPath = OutputPath.Replace("ADExport", "ADGroups");

            _logger.LogInformation("Starting export of all groups to: {OutputPath}", groupsPath);

            var result = await _exportService.ExportAllGroupsAsync(groupsPath);

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
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenExportFolder()
    {
        if (string.IsNullOrWhiteSpace(LastExportedFile) || !File.Exists(LastExportedFile))
        {
            _dialogService.ShowWarning("No exported file available.", "No File");
            return;
        }

        try
        {
            var folderPath = Path.GetDirectoryName(LastExportedFile);
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", folderPath);
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error opening folder: {ex.Message}", "Error");
            _logger.LogError(ex, "Error opening export folder");
        }
    }

    [RelayCommand]
    private void OpenExportedFile()
    {
        if (string.IsNullOrWhiteSpace(LastExportedFile) || !File.Exists(LastExportedFile))
        {
            _dialogService.ShowWarning("No exported file available.", "No File");
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
            _logger.LogError(ex, "Error opening exported file");
        }
    }
}
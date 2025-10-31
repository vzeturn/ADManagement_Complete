using ADManagement.Application.Configuration;
using ADManagement.Application.Interfaces;
using ADManagement.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ADManagement.WPF.ViewModels;

/// <summary>
/// ViewModel for Settings View
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ADConfiguration _adConfig;
    private readonly ExportConfiguration _exportConfig;
    private readonly IADUserService _userService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private string _domain;

    [ObservableProperty]
    private string _ldapServer;

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private bool _useSSL;

    [ObservableProperty]
    private int _pageSize;

    [ObservableProperty]
    private int _timeoutSeconds;

    [ObservableProperty]
    private string _exportDirectory;

    [ObservableProperty]
    private bool _includeDisabledAccounts;

    [ObservableProperty]
    private bool _autoFitColumns;

    [ObservableProperty]
    private bool _applyFormatting;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _connectionStatus = "Not Tested";

    public SettingsViewModel(
        ADConfiguration adConfig,
        ExportConfiguration exportConfig,
        IADUserService userService,
        IDialogService dialogService,
        ILogger<SettingsViewModel> logger)
    {
        _adConfig = adConfig;
        _exportConfig = exportConfig;
        _userService = userService;
        _dialogService = dialogService;
        _logger = logger;

        // Load current configuration
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        // AD Configuration
        Domain = _adConfig.Domain;
        LdapServer = _adConfig.LdapServer;
        Port = _adConfig.Port;
        UseSSL = _adConfig.UseSSL;
        PageSize = _adConfig.PageSize;
        TimeoutSeconds = _adConfig.TimeoutSeconds;

        // Export Configuration
        ExportDirectory = _exportConfig.OutputDirectory;
        IncludeDisabledAccounts = _exportConfig.IncludeDisabledAccounts;
        AutoFitColumns = _exportConfig.AutoFitColumns;
        ApplyFormatting = _exportConfig.ApplyFormatting;
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Testing connection to Active Directory...";
            ConnectionStatus = "Testing...";

            _logger.LogInformation("Testing Active Directory connection");

            var result = await _userService.TestConnectionAsync();

            if (result.IsSuccess)
            {
                ConnectionStatus = "✓ Connected";
                StatusMessage = "Connection successful";
                _dialogService.ShowSuccess("Successfully connected to Active Directory!", "Connection Test");
                _logger.LogInformation("Connection test successful");
            }
            else
            {
                ConnectionStatus = "✗ Failed";
                StatusMessage = "Connection failed";
                _dialogService.ShowError($"Connection failed:\n{result.Message}", "Connection Test");
                _logger.LogWarning("Connection test failed: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            ConnectionStatus = "✗ Error";
            StatusMessage = "Connection test error";
            _dialogService.ShowError($"Error testing connection: {ex.Message}", "Error");
            _logger.LogError(ex, "Error testing connection");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SaveConfiguration()
    {
        try
        {
            // Note: In a real application, you would save these to appsettings.json or user secrets
            // For this demo, we're just updating the in-memory configuration
            
            _adConfig.Domain = Domain;
            _adConfig.LdapServer = LdapServer;
            _adConfig.Port = Port;
            _adConfig.UseSSL = UseSSL;
            _adConfig.PageSize = PageSize;
            _adConfig.TimeoutSeconds = TimeoutSeconds;

            _exportConfig.OutputDirectory = ExportDirectory;
            _exportConfig.IncludeDisabledAccounts = IncludeDisabledAccounts;
            _exportConfig.AutoFitColumns = AutoFitColumns;
            _exportConfig.ApplyFormatting = ApplyFormatting;

            // Validate configuration
            _adConfig.Validate();

            _dialogService.ShowSuccess(
                "Configuration saved successfully.\n\nNote: These changes are saved in memory for this session only.\nTo persist changes, update appsettings.json.", 
                "Configuration Saved");

            _logger.LogInformation("Configuration saved successfully");
            StatusMessage = "Configuration saved";
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error saving configuration: {ex.Message}", "Error");
            _logger.LogError(ex, "Error saving configuration");
            StatusMessage = "Error saving configuration";
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        if (!_dialogService.ShowConfirmation(
            "Are you sure you want to reset all settings to default values?",
            "Reset to Defaults"))
        {
            return;
        }

        // Reset AD Configuration
        Domain = "corp.contoso.com";
        LdapServer = string.Empty;
        Port = 389;
        UseSSL = false;
        PageSize = 1000;
        TimeoutSeconds = 30;

        // Reset Export Configuration
        ExportDirectory = "./Exports";
        IncludeDisabledAccounts = true;
        AutoFitColumns = true;
        ApplyFormatting = true;

        _dialogService.ShowInformation("Settings have been reset to default values.", "Reset Complete");
        StatusMessage = "Settings reset to defaults";
    }

    [RelayCommand]
    private void BrowseExportDirectory()
    {
        var path = _dialogService.ShowFolderBrowserDialog("Select Export Directory");
        if (!string.IsNullOrWhiteSpace(path))
        {
            ExportDirectory = path;
        }
    }

    [RelayCommand]
    private void ViewLogs()
    {
        var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        
        if (Directory.Exists(logsPath))
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", logsPath);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error opening logs folder: {ex.Message}", "Error");
            }
        }
        else
        {
            _dialogService.ShowWarning("Logs folder does not exist yet.", "No Logs");
        }
    }
}
using ADManagement.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ADManagement.WPF.ViewModels;

/// <summary>
/// ViewModel for Main Window
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<MainWindowViewModel> _logger;

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private string _title = "AD Management System";

    public MainWindowViewModel(
        INavigationService navigationService,
        IDialogService dialogService,
        ILogger<MainWindowViewModel> logger)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;
        _logger = logger;

        // Subscribe to navigation changes
        _navigationService.NavigationChanged += OnNavigationChanged;

        // Navigate to Users view by default
        _navigationService.NavigateTo<UsersViewModel>();
    }

    private void OnNavigationChanged(object? sender, object viewModel)
    {
        CurrentViewModel = viewModel;
    }

    [RelayCommand]
    private void NavigateToUsers()
    {
        _logger.LogInformation("Navigating to Users view");
        _navigationService.NavigateTo<UsersViewModel>();
    }

    [RelayCommand]
    private void NavigateToGroups()
    {
        _logger.LogInformation("Navigating to Groups view");
        _navigationService.NavigateTo<GroupsViewModel>();
    }

    [RelayCommand]
    private void NavigateToExport()
    {
        _logger.LogInformation("Navigating to Export view");
        _navigationService.NavigateTo<ExportViewModel>();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _logger.LogInformation("Navigating to Settings view");
        _navigationService.NavigateTo<SettingsViewModel>();
    }

    [RelayCommand]
    private void ShowAbout()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        
        var message = $"AD Management System v{versionString}\n\n" +
                     "A modern Active Directory management tool featuring:\n" +
                     "• Secure Windows-integrated authentication\n" +
                     "• Efficient user and group management\n" +
                     "• Advanced search and export capabilities\n" +
                     "• Real-time connection diagnostics\n\n" +
                     "Built with .NET 9 and modern WPF architecture\n" +
                     "using MVVM, Dependency Injection and Clean Architecture.\n\n" +
                     "Author: Tran Vu\n" +
                     "Enhanced with AI assistance from GitHub Copilot\n\n" +
                     "© 2024 Tran Vu. All rights reserved.\n" +
                     "For support: https://github.com/vzeturn/ADManagement_Complete";
        
        _dialogService.ShowInformation(message, "About AD Management");
    }
}
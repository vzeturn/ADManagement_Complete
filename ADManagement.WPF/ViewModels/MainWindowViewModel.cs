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
        ILogger<MainWindowViewModel> logger,
        UsersViewModel usersViewModel)
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
        var message = "AD Management System v1.0\n\n" +
                     "A comprehensive Active Directory management tool\n" +
                     "built with .NET 9.0 and WPF.\n\n" +
                     "© 2024 All rights reserved.";
        
        _dialogService.ShowInformation(message, "About");
    }
}
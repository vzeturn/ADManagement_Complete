using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace ADManagement.WPF.ViewModels;

/// <summary>
/// ViewModel for Users View
/// </summary>
public partial class UsersViewModel : ObservableObject
{
    private readonly IADUserService _userService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<UsersViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<ADUserDto> _users = new();

    [ObservableProperty]
    private ADUserDto? _selectedUser;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public UsersViewModel(
        IADUserService userService,
        IDialogService dialogService,
        ILogger<UsersViewModel> logger)
    {
        _userService = userService;
        _dialogService = dialogService;
        _logger = logger;

        // Load users on initialization
        _ = LoadUsersAsync();
    }

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading users...";

            _logger.LogInformation("Loading all users from Active Directory");

            var result = await _userService.GetAllUsersAsync();

            if (result.IsSuccess && result.Value != null)
            {
                Users.Clear();
                foreach (var user in result.Value)
                {
                    Users.Add(user);
                }

                StatusMessage = $"Loaded {Users.Count} users";
                _logger.LogInformation("Successfully loaded {Count} users", Users.Count);
            }
            else
            {
                StatusMessage = "Failed to load users";
                _dialogService.ShowError(result.Message, "Load Users Failed");
                _logger.LogWarning("Failed to load users: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error loading users";
            _dialogService.ShowError($"Error loading users: {ex.Message}", "Error");
            _logger.LogError(ex, "Error loading users");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchUsersAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadUsersAsync();
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = $"Searching for '{SearchText}'...";

            _logger.LogInformation("Searching users with term: {SearchTerm}", SearchText);

            var result = await _userService.SearchUsersAsync(SearchText);

            if (result.IsSuccess && result.Value != null)
            {
                Users.Clear();
                foreach (var user in result.Value)
                {
                    Users.Add(user);
                }

                StatusMessage = $"Found {Users.Count} user(s)";
                _logger.LogInformation("Search found {Count} users", Users.Count);
            }
            else
            {
                StatusMessage = "Search failed";
                _dialogService.ShowError(result.Message, "Search Failed");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error searching users";
            _dialogService.ShowError($"Error searching users: {ex.Message}", "Error");
            _logger.LogError(ex, "Error searching users");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        SearchText = string.Empty;
        await LoadUsersAsync();
    }

    [RelayCommand]
    private async Task EnableUserAsync()
    {
        if (SelectedUser == null)
        {
            _dialogService.ShowWarning("Please select a user first.", "No User Selected");
            return;
        }

        if (SelectedUser.IsEnabled)
        {
            _dialogService.ShowInformation("User is already enabled.", "Information");
            return;
        }

        if (!_dialogService.ShowConfirmation(
            $"Are you sure you want to enable user '{SelectedUser.Username}'?",
            "Confirm Enable User"))
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = $"Enabling user {SelectedUser.Username}...";

            var result = await _userService.EnableUserAsync(SelectedUser.Username);

            if (result.IsSuccess)
            {
                _dialogService.ShowSuccess(result.Message, "Success");
                await LoadUsersAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message, "Enable Failed");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error enabling user: {ex.Message}", "Error");
            _logger.LogError(ex, "Error enabling user: {Username}", SelectedUser.Username);
        }
        finally
        {
            IsLoading = false;
            StatusMessage = "Ready";
        }
    }

    [RelayCommand]
    private async Task DisableUserAsync()
    {
        if (SelectedUser == null)
        {
            _dialogService.ShowWarning("Please select a user first.", "No User Selected");
            return;
        }

        if (!SelectedUser.IsEnabled)
        {
            _dialogService.ShowInformation("User is already disabled.", "Information");
            return;
        }

        if (!_dialogService.ShowConfirmation(
            $"Are you sure you want to disable user '{SelectedUser.Username}'?",
            "Confirm Disable User"))
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = $"Disabling user {SelectedUser.Username}...";

            var result = await _userService.DisableUserAsync(SelectedUser.Username);

            if (result.IsSuccess)
            {
                _dialogService.ShowSuccess(result.Message, "Success");
                await LoadUsersAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message, "Disable Failed");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error disabling user: {ex.Message}", "Error");
            _logger.LogError(ex, "Error disabling user: {Username}", SelectedUser.Username);
        }
        finally
        {
            IsLoading = false;
            StatusMessage = "Ready";
        }
    }

    [RelayCommand]
    private async Task UnlockUserAsync()
    {
        if (SelectedUser == null)
        {
            _dialogService.ShowWarning("Please select a user first.", "No User Selected");
            return;
        }

        if (!SelectedUser.IsLockedOut)
        {
            _dialogService.ShowInformation("User is not locked out.", "Information");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = $"Unlocking user {SelectedUser.Username}...";

            var result = await _userService.UnlockUserAsync(SelectedUser.Username);

            if (result.IsSuccess)
            {
                _dialogService.ShowSuccess(result.Message, "Success");
                await LoadUsersAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message, "Unlock Failed");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error unlocking user: {ex.Message}", "Error");
            _logger.LogError(ex, "Error unlocking user: {Username}", SelectedUser.Username);
        }
        finally
        {
            IsLoading = false;
            StatusMessage = "Ready";
        }
    }

    [RelayCommand]
    private void ViewUserDetails()
    {
        if (SelectedUser == null)
        {
            _dialogService.ShowWarning("Please select a user first.", "No User Selected");
            return;
        }

        var details = $"Username: {SelectedUser.Username}\n" +
                     $"Display Name: {SelectedUser.DisplayName}\n" +
                     $"Email: {SelectedUser.Email}\n" +
                     $"Department: {SelectedUser.Department}\n" +
                     $"Title: {SelectedUser.Title}\n" +
                     $"Status: {SelectedUser.AccountStatus}\n" +
                     $"Last Logon: {SelectedUser.LastLogonFormatted}\n" +
                     $"Groups: {SelectedUser.MemberOf.Count}";

        _dialogService.ShowInformation(details, "User Details");
    }
}
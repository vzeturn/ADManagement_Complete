using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.WPF.Services;
using ADManagement.WPF.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Linq;

namespace ADManagement.WPF.ViewModels;

/// <summary>
/// ViewModel for Users View
/// </summary>
public partial class UsersViewModel : ObservableObject
{
    private readonly IADUserService _userService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<UsersViewModel> _logger;

    public UsersViewModel(
        IADUserService userService,
        IDialogService dialogService,
        ILogger<UsersViewModel> logger)
    {
        _userService = userService;
        _dialogService = dialogService;
        _logger = logger;

        Users = new ObservableCollection<ADUserDto>();
    }

    #region Properties

    [ObservableProperty]
    private ObservableCollection<ADUserDto> _users;

    [ObservableProperty]
    private ADUserDto? _selectedUser;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _includeDisabledAccounts = true;

    #endregion

    #region Lifecycle

    public async Task InitializeAsync()
    {
        await LoadUsersAsync();
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading users...";
            _logger.LogInformation("Loading users");

            var result = await _userService.GetAllUsersAsync();

            if (result.IsSuccess && result.Value != null)
            {
                var users = result.Value;

                if (!IncludeDisabledAccounts)
                {
                    users = users.Where(u => u.IsEnabled);
                }

                Users.Clear();
                foreach (var user in users.OrderBy(u => u.DisplayName))
                {
                    Users.Add(user);
                }

                StatusMessage = $"Loaded {Users.Count} user(s)";
                _logger.LogInformation("Loaded {Count} users", Users.Count);
            }
            else
            {
                _dialogService.ShowError(result.Message, "Load Failed");
                StatusMessage = "Failed to load users";
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error loading users: {ex.Message}", "Error");
            _logger.LogError(ex, "Error loading users");
            StatusMessage = "Error loading users";
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
                var users = result.Value;

                if (!IncludeDisabledAccounts)
                {
                    users = users.Where(u => u.IsEnabled);
                }

                Users.Clear();
                foreach (var user in users.OrderBy(u => u.DisplayName))
                {
                    Users.Add(user);
                }

                StatusMessage = $"Found {Users.Count} user(s)";
                _logger.LogInformation("Found {Count} users", Users.Count);
            }
            else
            {
                _dialogService.ShowError(result.Message, "Search Failed");
                StatusMessage = "Search failed";
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error searching users: {ex.Message}", "Error");
            _logger.LogError(ex, "Error searching users");
            StatusMessage = "Search error";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CreateUser()
    {
        var dialog = new CreateUserDialog();
        var result = dialog.ShowDialog();

        if (result == true)
        {
            // Reload users after creating new user
            _ = LoadUsersAsync();
        }
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

        if (!SelectedUser.IsLocked)
        {
            _dialogService.ShowInformation("User account is not locked.", "Information");
            return;
        }

        if (!_dialogService.ShowConfirmation(
            $"Are you sure you want to unlock user '{SelectedUser.Username}'?",
            "Confirm Unlock User"))
        {
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

    /// <summary>
    /// Command to open User Detail Window (triggered by double-click)
    /// </summary>
    [RelayCommand]
    private void OpenUserDetail()
    {
        if (SelectedUser == null)
        {
            _dialogService.ShowWarning("Please select a user first.", "No User Selected");
            return;
        }

        try
        {
            _logger.LogInformation("Opening user detail for {Username}", SelectedUser.Username);

            var detailWindow = new UserDetailWindow(SelectedUser.SamAccountName);
            detailWindow.ShowDialog();

            // Reload users after closing detail window to reflect any changes
            _ = LoadUsersAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error opening user details: {ex.Message}", "Error");
            _logger.LogError(ex, "Error opening user detail for {Username}", SelectedUser.Username);
        }
    }

    #endregion

    #region Private Methods

    partial void OnIncludeDisabledAccountsChanged(bool value)
    {
        _ = LoadUsersAsync();
    }

    #endregion
}

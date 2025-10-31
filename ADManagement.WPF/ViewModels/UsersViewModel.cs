using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Threading;

namespace ADManagement.WPF.ViewModels;

/// <summary>
/// ViewModel for Users View
/// </summary>
public partial class UsersViewModel : ObservableObject
{
    private readonly IADUserService _userService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<UsersViewModel> _logger;

    private readonly DispatcherTimer _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
    private CancellationTokenSource? _searchCts;

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
        INavigationService navigationService,
        ILogger<UsersViewModel> logger)
    {
        _userService = userService;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _logger = logger;

        _searchDebounceTimer.Tick += async (s, e) =>
        {
            _searchDebounceTimer.Stop();
            await SearchUsersAsync();
        };

        // Load users on initialization
        //_ = LoadUsersAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
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
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

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

            var result = await _userService.SearchUsersAsync(SearchText, token);

            if (token.IsCancellationRequested) return;

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
        catch (OperationCanceledException)
        {
            // swallow; a newer search superseded this one
        }
        catch (Exception ex)
        {
            StatusMessage = "Error searching users";
            _dialogService.ShowError($"Error searching users: {ex.Message}", "Error");
            _logger.LogError(ex, "Error searching users");
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                IsLoading = false;
            }
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

        if (!SelectedUser.IsLocked)
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

        _navigationService.NavigateTo<UserDetailsViewModel>(SelectedUser);
    }

    [RelayCommand]
    private void OpenDetails(ADUserDto? user)
    {
        var target = user ?? SelectedUser;
        if (target == null)
        {
            _dialogService.ShowWarning("Please select a user first.", "No User Selected");
            return;
        }
        _navigationService.NavigateTo<UserDetailsViewModel>(target);
    }

    [RelayCommand]
    private void CreateNewUser()
    {
        var services = App.Services;
        if (services == null) return;
        
        var dialog = services.GetRequiredService<Views.CreateUserDialog>();
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        var result = dialog.ShowDialog();
        if (result == true)
        {
            // Refresh users list after creation
            _ = LoadUsersAsync();
        }
    }

    [RelayCommand]
    private async Task ResetPasswordAsync(ADUserDto? user)
    {
        var target = user ?? SelectedUser;
        if (target == null)
        {
            _dialogService.ShowWarning("Please select a user first.", "No User Selected");
            return;
        }

        var pwd1 = _dialogService.ShowInputDialog($"Enter new password for '{target.Username}':", "Reset Password");
        if (pwd1 == null)
        {
            return; // cancelled
        }
        var pwd2 = _dialogService.ShowInputDialog("Confirm new password:", "Reset Password");
        if (pwd2 == null)
        {
            return; // cancelled
        }
        if (!string.Equals(pwd1, pwd2))
        {
            _dialogService.ShowError("Passwords do not match. Please try again.", "Mismatch");
            return;
        }

        if (!_dialogService.ShowConfirmation($"Are you sure you want to reset the password for '{target.Username}'?", "Confirm Reset Password"))
        {
            return;
        }

        var mustChange = _dialogService.ShowConfirmation($"Force user '{target.Username}' to change password at next logon?", "Password Policy");

        try
        {
            IsLoading = true;
            StatusMessage = $"Resetting password for {target.Username}...";

            var req = new ADManagement.Application.DTOs.PasswordChangeRequest
            {
                Username = target.Username,
                NewPassword = pwd1,
                ConfirmPassword = pwd2,
                MustChangeAtNextLogon = mustChange
            };

            var result = await _userService.ChangePasswordAsync(req);
            if (result.IsSuccess)
            {
                _dialogService.ShowSuccess(result.Message, "Success");
            }
            else
            {
                _dialogService.ShowError(result.Message, "Reset Failed");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error resetting password: {ex.Message}", "Error");
            _logger.LogError(ex, "Error resetting password for user: {Username}", target.Username);
        }
        finally
        {
            IsLoading = false;
            StatusMessage = "Ready";
        }
    }

}
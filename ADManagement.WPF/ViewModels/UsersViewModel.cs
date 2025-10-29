using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace ADManagement.WPF.ViewModels;

public partial class UsersViewModel : ObservableObject
{
    private readonly IADUserService _userService;
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
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _totalUsers;

    [ObservableProperty]
    private int _enabledUsers;

    [ObservableProperty]
    private int _disabledUsers;

    public UsersViewModel(IADUserService userService, ILogger<UsersViewModel> logger)
    {
        _userService = userService;
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

            var result = await _userService.GetAllUsersAsync();

            if (result.IsSuccess && result.Value != null)
            {
                Users.Clear();
                foreach (var user in result.Value)
                {
                    Users.Add(user);
                }

                TotalUsers = Users.Count;
                EnabledUsers = Users.Count(u => u.IsEnabled);
                DisabledUsers = Users.Count(u => !u.IsEnabled);

                StatusMessage = $"Loaded {TotalUsers} users";
                _logger.LogInformation("Loaded {Count} users", TotalUsers);
            }
            else
            {
                StatusMessage = $"Error: {result.Message}";
                _logger.LogWarning("Failed to load users: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users");
            StatusMessage = $"Error: {ex.Message}";
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

            var result = await _userService.SearchUsersAsync(SearchText);

            if (result.IsSuccess && result.Value != null)
            {
                Users.Clear();
                foreach (var user in result.Value)
                {
                    Users.Add(user);
                }

                StatusMessage = $"Found {Users.Count} users matching '{SearchText}'";
                _logger.LogInformation("Search '{SearchText}' returned {Count} results", SearchText, Users.Count);
            }
            else
            {
                StatusMessage = $"Error: {result.Message}";
                _logger.LogWarning("Search failed: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            StatusMessage = $"Error: {ex.Message}";
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

    [RelayCommand(CanExecute = nameof(CanExecuteUserCommand))]
    private async Task EnableUserAsync()
    {
        if (SelectedUser == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = $"Enabling user {SelectedUser.Username}...";

            var result = await _userService.EnableUserAsync(SelectedUser.Username);

            if (result.IsSuccess)
            {
                StatusMessage = $"User {SelectedUser.Username} enabled successfully";
                await LoadUsersAsync();
            }
            else
            {
                StatusMessage = $"Error: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling user");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteUserCommand))]
    private async Task DisableUserAsync()
    {
        if (SelectedUser == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = $"Disabling user {SelectedUser.Username}...";

            var result = await _userService.DisableUserAsync(SelectedUser.Username);

            if (result.IsSuccess)
            {
                StatusMessage = $"User {SelectedUser.Username} disabled successfully";
                await LoadUsersAsync();
            }
            else
            {
                StatusMessage = $"Error: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling user");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteUserCommand))]
    private async Task UnlockUserAsync()
    {
        if (SelectedUser == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = $"Unlocking user {SelectedUser.Username}...";

            var result = await _userService.UnlockUserAsync(SelectedUser.Username);

            if (result.IsSuccess)
            {
                StatusMessage = $"User {SelectedUser.Username} unlocked successfully";
                await LoadUsersAsync();
            }
            else
            {
                StatusMessage = $"Error: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanExecuteUserCommand()
    {
        return SelectedUser != null && !IsLoading;
    }
}

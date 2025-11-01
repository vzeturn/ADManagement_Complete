using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.Domain.Interfaces;
using ADManagement.WPF.Services;
using ADManagement.WPF.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace ADManagement.WPF.ViewModels;

/// <summary>
/// ✨ OPTIMIZED ViewModel for Users View
/// - Streaming load
/// - Batch UI updates
/// - Progress feedback
/// - Cancellation support
/// - Debounced search
/// </summary>
public partial class UsersViewModel : ObservableObject
{
    private readonly IADUserService _userService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<UsersViewModel> _logger;

    // ✨ NEW: Cancellation support
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _searchCancellation;

    public UsersViewModel(
        IADUserService userService,
        IDialogService dialogService,
        ILogger<UsersViewModel> logger)
    {
        _userService = userService;
        _dialogService = dialogService;
        _logger = logger;

        Users = new ObservableCollection<ADUserDto>();

        // ✨ NEW: Setup collection view for filtering
        UsersView = CollectionViewSource.GetDefaultView(Users);
        UsersView.Filter = FilterUsers;
    }

    #region Properties

    [ObservableProperty]
    private ObservableCollection<ADUserDto> _users;

    [ObservableProperty]
    private ICollectionView? _usersView;

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

    // ✨ NEW: Progress tracking
    [ObservableProperty]
    private double _loadingProgress;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _loadedCount;

    [ObservableProperty]
    private string _loadingMessage = string.Empty;

    #endregion

    #region Lifecycle

    public async Task InitializeAsync()
    {
        await LoadUsersAsync();
    }

    #endregion

    #region Commands

    /// <summary>
    /// ✨ OPTIMIZED: Load users with streaming and progress feedback
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanLoadUsers))]
    private async Task LoadUsersAsync()
    {
        // Cancel any existing operation
        _loadCancellation?.Cancel();
        _loadCancellation = new CancellationTokenSource();
        var cancellationToken = _loadCancellation.Token;

        try
        {
            IsLoading = true;
            LoadingProgress = 0;
            LoadedCount = 0;
            TotalCount = 0;
            LoadingMessage = "Initializing...";
            StatusMessage = "Loading users...";

            _logger.LogInformation("Starting to load users (optimized)");

            // Clear existing
            Users.Clear();

            // ✨ NEW: Progress reporter
            var progress = new Progress<LoadProgress>(p =>
            {
                LoadingProgress = p.PercentComplete;
                LoadedCount = p.ProcessedCount;
                TotalCount = p.TotalCount > 0 ? p.TotalCount : LoadedCount;
                LoadingMessage = $"Loading... {LoadedCount:N0} users";
            });

            var tempList = new List<ADUserDto>();
            var updateInterval = 500; // Update UI every 500 items

            // ✨ NEW: Stream users for memory efficiency
            await foreach (var user in _userService.StreamUsersAsync(cancellationToken))
            {
                // Filter if needed
                if (!IncludeDisabledAccounts && !user.IsEnabled)
                    continue;

                tempList.Add(user);

                // ✨ OPTIMIZATION: Batch update UI
                if (tempList.Count >= updateInterval)
                {
                    await UpdateUIAsync(tempList);
                    tempList.Clear();

                    ((IProgress<LoadProgress>)progress).Report(new LoadProgress
                    {
                        ProcessedCount = LoadedCount,
                        CurrentPhase = "Loading"
                    });
                }
            }

            // Final batch
            if (tempList.Count > 0)
            {
                await UpdateUIAsync(tempList);
            }

            StatusMessage = $"Loaded {LoadedCount:N0} users successfully";
            LoadingMessage = string.Empty;

            _logger.LogInformation("Successfully loaded {Count} users", LoadedCount);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Loading cancelled";
            LoadingMessage = "Cancelled";
            _logger.LogInformation("User loading cancelled");
        }
        catch (Exception ex)
        {
            StatusMessage = "Error loading users";
            LoadingMessage = "Error";
            _dialogService.ShowError($"Error loading users: {ex.Message}", "Error");
            _logger.LogError(ex, "Error loading users");
        }
        finally
        {
            IsLoading = false;
            LoadingProgress = 0;
        }
    }

    /// <summary>
    /// ✨ NEW: Batch UI update to avoid UI freezing
    /// </summary>
    private async Task UpdateUIAsync(List<ADUserDto> newUsers)
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // ✨ OPTIMIZATION: Suspend collection synchronization for batch add
            BindingOperations.DisableCollectionSynchronization(Users);

            try
            {
                foreach (var user in newUsers)
                {
                    Users.Add(user);
                }
            }
            finally
            {
                // Re-enable synchronization
                BindingOperations.EnableCollectionSynchronization(Users, new object());
                UsersView?.Refresh();
            }

            LoadedCount = Users.Count;

        }, DispatcherPriority.Background); // ✨ IMPORTANT: Background priority prevents UI freezing
    }

    /// <summary>
    /// ✨ NEW: Cancel loading operation
    /// </summary>
    [RelayCommand]
    private void CancelLoading()
    {
        _loadCancellation?.Cancel();
        LoadingMessage = "Cancelling...";
        StatusMessage = "Cancelling...";
    }

    private bool CanLoadUsers() => !IsLoading;

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        await LoadUsersAsync();
    }

    private bool CanRefresh() => !IsLoading;

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

            // Reload users after closing detail window
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

    /// <summary>
    /// ✨ NEW: Debounced search on text change
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        // Cancel previous search
        _searchCancellation?.Cancel();
        _searchCancellation = new CancellationTokenSource();

        // Debounce search
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, _searchCancellation.Token); // 300ms debounce

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    UsersView?.Refresh();
                });
            }
            catch (OperationCanceledException)
            {
                // Search cancelled, ignore
            }
        }, _searchCancellation.Token);
    }

    /// <summary>
    /// ✨ NEW: Filter function for collection view
    /// </summary>
    private bool FilterUsers(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        if (obj is ADUserDto user)
        {
            var searchLower = SearchText.ToLowerInvariant();
            return user.Username?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) == true ||
                   user.DisplayName?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) == true ||
                   user.Email?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) == true ||
                   user.Department?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) == true;
        }

        return false;
    }

    partial void OnIncludeDisabledAccountsChanged(bool value)
    {
        _ = LoadUsersAsync();
    }

    #endregion
}

/// <summary>
/// ✨ NEW: Progress information for loading
/// </summary>
public class LoadProgress
{
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public double PercentComplete => TotalCount > 0
        ? (double)ProcessedCount / TotalCount * 100
        : 0;
}
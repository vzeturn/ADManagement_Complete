using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.WPF.Services;
using ADManagement.WPF.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ADManagement.WPF.ViewModels;

/// <summary>
/// ViewModel for User Detail Window with multiple tabs
/// </summary>
public partial class UserDetailWindowViewModel : ObservableObject
{
    private readonly IADUserService _userService;
    private readonly IADGroupService _groupService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<UserDetailWindowViewModel> _logger;

    private string _username = string.Empty;
    private ADUserDto? _originalUser;

    public UserDetailWindowViewModel(
        IADUserService userService,
        IADGroupService groupService,
        IDialogService dialogService,
        ILogger<UserDetailWindowViewModel> logger)
    {
        _userService = userService;
        _groupService = groupService;
        _dialogService = dialogService;
        _logger = logger;

        // Initialize collections
        FilteredGroups = new ObservableCollection<string>();
        _groupsView = CollectionViewSource.GetDefaultView(FilteredGroups);
        _groupsView.Filter = GroupFilter;
    }

    #region Properties

    [ObservableProperty]
    private ADUserDto? _user;

    [ObservableProperty]
    private string _displayTitle = "User Properties";

    [ObservableProperty]
    private string _userInfo = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = string.Empty;

    [ObservableProperty]
    private bool _hasChanges;

    [ObservableProperty]
    private string _accountStatusText = string.Empty;

    [ObservableProperty]
    private string _accountStatusColor = "#4CAF50";

    [ObservableProperty]
    private bool _isAccountDisabled;

    [ObservableProperty]
    private string _managerDisplayName = string.Empty;

    // Groups
    [ObservableProperty]
    private ObservableCollection<string> _filteredGroups;

    [ObservableProperty]
    private string? _selectedGroup;

    [ObservableProperty]
    private bool _isGroupSelected;

    [ObservableProperty]
    private string _groupSearchText = string.Empty;

    private readonly ICollectionView _groupsView;
    private List<string> _allGroups = new();

    public event EventHandler<bool>? CloseRequested;

    #endregion

    #region Public Methods

    public void SetUsername(string username)
    {
        _username = username;
    }

    public async Task LoadUserDataAsync()
    {
        if (string.IsNullOrWhiteSpace(_username))
        {
            _dialogService.ShowError("Username is required.", "Error");
            return;
        }

        try
        {
            IsLoading = true;
            LoadingMessage = $"Loading user data for {_username}...";

            // Load user details
            var result = await _userService.GetUserByUsernameAsync(_username);
            if (!result.IsSuccess || result.Value == null)
            {
                _dialogService.ShowError(result.Message, "Error Loading User");
                CloseRequested?.Invoke(this, false);
                return;
            }

            User = result.Value;
            _originalUser = CloneUser(result.Value);

            // Update display
            UpdateDisplay();

            // Load groups
            await LoadUserGroupsAsync();

            HasChanges = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user data for {Username}", _username);
            _dialogService.ShowError($"Error loading user data: {ex.Message}", "Error");
            CloseRequested?.Invoke(this, false);
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = string.Empty;
        }
    }

    #endregion

    #region Private Methods

    private void UpdateDisplay()
    {
        if (User == null) return;

        DisplayTitle = $"{User.DisplayName} Properties";
        UserInfo = $"{User.SamAccountName} ({User.Email})";

        // Account status
        if (User.IsLocked)
        {
            AccountStatusText = "🔒 Locked";
            AccountStatusColor = "#F44336";
        }
        else if (!User.IsEnabled)
        {
            AccountStatusText = "⛔ Disabled";
            AccountStatusColor = "#FF9800";
        }
        else
        {
            AccountStatusText = "✅ Enabled";
            AccountStatusColor = "#4CAF50";
        }

        IsAccountDisabled = !User.IsEnabled;

        // Manager display name
        if (!string.IsNullOrWhiteSpace(User.Manager))
        {
            ManagerDisplayName = ExtractCnFromDn(User.Manager);
        }

        // Subscribe to property changes
        User.PropertyChanged += User_PropertyChanged;
    }

    private void User_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Check if any property has changed
        if (User != null && _originalUser != null)
        {
            HasChanges = !AreUsersEqual(User, _originalUser);
        }
    }

    private async Task LoadUserGroupsAsync()
    {
        if (User == null) return;

        try
        {
            LoadingMessage = "Loading groups...";

            _allGroups = User.MemberOf.ToList();

            FilteredGroups.Clear();
            foreach (var group in _allGroups)
            {
                FilteredGroups.Add(ExtractCnFromDn(group));
            }

            _groupsView.Refresh();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading groups for user {Username}", _username);
            _dialogService.ShowError($"Error loading groups: {ex.Message}", "Error");
        }
    }

    partial void OnGroupSearchTextChanged(string value)
    {
        _groupsView.Refresh();
    }

    partial void OnSelectedGroupChanged(string? value)
    {
        IsGroupSelected = !string.IsNullOrWhiteSpace(value);
    }

    private bool GroupFilter(object item)
    {
        if (string.IsNullOrWhiteSpace(GroupSearchText))
            return true;

        if (item is string groupName)
        {
            return groupName.Contains(GroupSearchText, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private string ExtractCnFromDn(string dn)
    {
        if (string.IsNullOrWhiteSpace(dn)) return dn;

        try
        {
            var parts = dn.Split(',');
            if (parts.Length == 0) return dn;

            var cnPart = parts[0];
            if (cnPart.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            {
                return cnPart.Substring(3);
            }

            return cnPart;
        }
        catch
        {
            return dn;
        }
    }

    private ADUserDto CloneUser(ADUserDto user)
    {
        // Simple clone - in production, use proper cloning method
        return new ADUserDto
        {
            SamAccountName = user.SamAccountName,
            UserPrincipalName = user.UserPrincipalName,
            DisplayName = user.DisplayName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Description = user.Description,
            Department = user.Department,
            Title = user.Title,
            Company = user.Company,
            Office = user.Office,
            Manager = user.Manager,
            EmployeeId = user.EmployeeId,
            EmployeeType = user.EmployeeType,
            StreetAddress = user.StreetAddress,
            City = user.City,
            State = user.State,
            PostalCode = user.PostalCode,
            Country = user.Country,
            PostOfficeBox = user.PostOfficeBox,
            TelephoneNumber = user.TelephoneNumber,
            HomePhone = user.HomePhone,
            Mobile = user.Mobile,
            Pager = user.Pager,
            Fax = user.Fax,
            IpPhone = user.IpPhone,
            WebPage = user.WebPage,
            ProfilePath = user.ProfilePath,
            ScriptPath = user.ScriptPath,
            HomeDirectory = user.HomeDirectory,
            HomeDrive = user.HomeDrive,
            IsEnabled = user.IsEnabled,
            IsLocked = user.IsLocked,
            MustChangePassword = user.MustChangePassword,
            CannotChangePassword = user.CannotChangePassword,
            PasswordNeverExpires = user.PasswordNeverExpires,
            LastLogon = user.LastLogon,
            PasswordLastSet = user.PasswordLastSet,
            AccountExpires = user.AccountExpires,
            //Info = user.Info,
            MemberOf = new List<string>(user.MemberOf),
            DirectReports = new List<string>(user.DirectReports)
        };
    }

    private bool AreUsersEqual(ADUserDto user1, ADUserDto user2)
    {
        // Compare key properties
        return user1.FirstName == user2.FirstName &&
               user1.LastName == user2.LastName &&
               user1.DisplayName == user2.DisplayName &&
               user1.Email == user2.Email &&
               user1.Description == user2.Description &&
               user1.Department == user2.Department &&
               user1.Title == user2.Title &&
               user1.Company == user2.Company &&
               user1.Office == user2.Office &&
               user1.StreetAddress == user2.StreetAddress &&
               user1.City == user2.City &&
               user1.State == user2.State &&
               user1.PostalCode == user2.PostalCode &&
               user1.Country == user2.Country &&
               user1.TelephoneNumber == user2.TelephoneNumber &&
               user1.Mobile == user2.Mobile &&
               user1.MustChangePassword == user2.MustChangePassword &&
               user1.CannotChangePassword == user2.CannotChangePassword &&
               user1.PasswordNeverExpires == user2.PasswordNeverExpires;
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (User == null) return;

        try
        {
            IsLoading = true;
            LoadingMessage = "Saving changes...";

            // Save user changes via service
            var result = await _userService.UpdateUserAsync(User);

            if (result.IsSuccess)
            {
                _dialogService.ShowSuccess("User updated successfully.", "Success");
                _originalUser = CloneUser(User);
                HasChanges = false;
            }
            else
            {
                _dialogService.ShowError(result.Message, "Update Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {Username}", _username);
            _dialogService.ShowError($"Error updating user: {ex.Message}", "Error");
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task OkAsync()
    {
        if (HasChanges)
        {
            await ApplyAsync();
        }

        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private async Task UnlockAccountAsync()
    {
        if (User == null || !User.IsLocked) return;

        if (!_dialogService.ShowConfirmation(
            $"Are you sure you want to unlock the account for {User.DisplayName}?",
            "Confirm Unlock"))
        {
            return;
        }

        try
        {
            IsLoading = true;
            LoadingMessage = "Unlocking account...";

            var result = await _userService.UnlockUserAsync(User.SamAccountName);

            if (result.IsSuccess)
            {
                _dialogService.ShowSuccess("Account unlocked successfully.", "Success");
                await LoadUserDataAsync(); // Reload to refresh status
            }
            else
            {
                _dialogService.ShowError(result.Message, "Unlock Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user {Username}", _username);
            _dialogService.ShowError($"Error unlocking account: {ex.Message}", "Error");
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        if (User == null) return;

        // TODO: Show password reset dialog
        _dialogService.ShowInformation("Password reset functionality will be implemented.", "Information");
    }

    [RelayCommand]
    private void BrowseManager()
    {
        // TODO: Show user browser dialog to select manager
        _dialogService.ShowInformation("Manager browser will be implemented.", "Information");
    }

    [RelayCommand]
    private async Task AddGroupAsync()
    {
        if (User == null) return;

        try
        {
            // Show group search dialog
            var groupSearchDialog = new GroupSearchDialog(_username);
            var result = groupSearchDialog.ShowDialog();

            if (result == true && groupSearchDialog.DataContext is GroupSearchViewModel gsVm)
            {
                var selectedGroupDns = gsVm.GetSelectedGroupDns();
                if (selectedGroupDns.Any())
                {
                    IsLoading = true;
                    LoadingMessage = "Adding groups...";

                    foreach (var groupDn in selectedGroupDns)
                    {
                        var groupCn = ExtractCnFromDn(groupDn);
                        var addResult = await _groupService.AddUserToGroupAsync(User.SamAccountName, groupCn);

                        if (!addResult.IsSuccess)
                        {
                            _dialogService.ShowWarning($"Failed to add to {groupCn}: {addResult.Message}", "Warning");
                        }
                    }

                    _dialogService.ShowSuccess($"Added user to {selectedGroupDns.Count} group(s).", "Success");
                    await LoadUserDataAsync(); // Reload to refresh groups
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding groups for user {Username}", _username);
            _dialogService.ShowError($"Error adding groups: {ex.Message}", "Error");
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task RemoveGroupAsync()
    {
        if (User == null || string.IsNullOrWhiteSpace(SelectedGroup)) return;

        if (!_dialogService.ShowConfirmation(
            $"Are you sure you want to remove {User.DisplayName} from the group '{SelectedGroup}'?",
            "Confirm Remove"))
        {
            return;
        }

        try
        {
            IsLoading = true;
            LoadingMessage = $"Removing from group {SelectedGroup}...";

            var result = await _groupService.RemoveUserFromGroupAsync(User.SamAccountName, SelectedGroup);

            if (result.IsSuccess)
            {
                _dialogService.ShowSuccess($"User removed from group {SelectedGroup}.", "Success");
                await LoadUserDataAsync(); // Reload to refresh groups
            }
            else
            {
                _dialogService.ShowError(result.Message, "Remove Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user from group {GroupName}", SelectedGroup);
            _dialogService.ShowError($"Error removing from group: {ex.Message}", "Error");
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = string.Empty;
        }
    }

    #endregion
}

using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using System.Windows;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace ADManagement.WPF.ViewModels;

public partial class UserDetailsViewModel : ObservableObject
{
    private readonly IADUserService _userService;
    private readonly IADGroupService _groupService;

    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserDetailsViewModel> _logger;

    public UserDetailsViewModel(
        IADUserService userService,
        IADGroupService groupService,
        IDialogService dialogService,
        INavigationService navigationService,
        IAuditService auditService,
        ILogger<UserDetailsViewModel> logger)
    {
        _userService = userService;
        _groupService = groupService;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _auditService = auditService;
        _logger = logger;
    }

    [ObservableProperty]
    private ADUserDto? _selectedUser;

    [ObservableProperty]
    private ObservableCollection<string> _groups = new();

    [ObservableProperty]
    private string? _selectedGroup;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public void SetParameter(object parameter)
    {
        if (parameter is ADUserDto user)
        {
            SelectedUser = user;
            _ = LoadUserGroupsAsync();
        }
    }

    [RelayCommand]
    private async Task LoadUserGroupsAsync()
    {
        if (SelectedUser == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = $"Loading groups for {SelectedUser.SamAccountName}...";

            var result = await _groupService.GetUserGroupsAsync(SelectedUser.SamAccountName);
            if (result.IsSuccess && result.Value != null)
            {
                Groups.Clear();
                foreach (var g in result.Value)
                {
                    Groups.Add(g.Name);
                }
                StatusMessage = $"User is member of {Groups.Count} group(s)";
            }
            else
            {
                _dialogService.ShowError(result.Message, "Load Groups Failed");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error loading groups: {ex.Message}", "Error");
            _logger.LogError(ex, "Error loading groups for user {Username}", SelectedUser?.SamAccountName);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // public wrapper so external code (views) can request a refresh
    public Task RefreshUserGroupsAsync()
    {
        return LoadUserGroupsAsync();
    }

    [RelayCommand]
    private void OpenGroupSearch()
    {
        if (SelectedUser == null) return;

        // Navigate to GroupSearchViewModel with parameter of selected username
        _navigationService.NavigateTo<GroupSearchViewModel>(SelectedUser.SamAccountName!);
    }

    [RelayCommand]
    private async Task AddToGroupAsync()
    {
        if (SelectedUser == null)
        {
            _dialogService.ShowWarning("Select a user first.", "Warning");
            return;
        }

        var groupName = _dialogService.ShowInputDialog($"Enter group name to add user '{SelectedUser.SamAccountName}' to:", "Add to Group");
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = $"Adding user to group {groupName}...";

            var result = await _groupService.AddUserToGroupAsync(SelectedUser.SamAccountName!, groupName);
            if (result.IsSuccess)
            {
                _dialogService.ShowSuccess(result.Message, "Success");
                await LoadUserGroupsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message, "Error");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error adding user to group: {ex.Message}", "Error");
            _logger.LogError(ex, "Error adding user {Username} to group {GroupName}", SelectedUser.SamAccountName, groupName);
        }
        finally
        {
            IsLoading = false;
            StatusMessage = "Ready";
        }
    }

    [RelayCommand]
    private async Task RemoveFromGroupAsync()
    {
        if (SelectedUser == null || SelectedGroup == null)
        {
            _dialogService.ShowWarning("Select a user and a group first.", "Warning");
            return;
        }

        if (!_dialogService.ShowConfirmation($"Remove user '{SelectedUser.SamAccountName}' from group '{SelectedGroup}'?", "Confirm"))
            return;

        try
        {
            IsLoading = true;
            StatusMessage = $"Removing user from group {SelectedGroup}...";

            var result = await _groupService.RemoveUserFromGroupAsync(SelectedUser.SamAccountName!, SelectedGroup);
            if (result.IsSuccess)
            {
                _dialogService.ShowSuccess(result.Message, "Success");
                await LoadUserGroupsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message, "Error");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error removing user from group: {ex.Message}", "Error");
            _logger.LogError(ex, "Error removing user {Username} from group {GroupName}", SelectedUser.SamAccountName, SelectedGroup);
        }
        finally
        {
            IsLoading = false;
            StatusMessage = "Ready";
        }
    }

    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        if (SelectedUser == null)
        {
            _dialogService.ShowWarning("No user selected.", "Warning");
            return;
        }

        // Ask if auto-generate or manual
        var auto = _dialogService.ShowConfirmation("Auto-generate a strong password? (Yes = auto, No = manual)", "Password Option");
        string newPassword;

        if (auto)
        {
            newPassword = GenerateStrongPassword(16);
            // Copy to clipboard and show to user
            try
            {
                Clipboard.SetText(newPassword);
                _dialogService.ShowInformation($"Generated password copied to clipboard.\nPlease store it securely.\nTemporary password: {newPassword}", "Password Generated");
            }
            catch
            {
                _dialogService.ShowInformation($"Generated password: {newPassword}", "Password Generated");
            }
        }
        else
        {
            newPassword = _dialogService.ShowInputDialog($"Enter new password for {SelectedUser.SamAccountName}:", "Reset Password");
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                _dialogService.ShowInformation("Password reset cancelled or empty.", "Cancelled");
                return;
            }
        }

        // Validate locally using validator rules by calling service; ChangePasswordAsync will validate as well
        var mustChange = _dialogService.ShowConfirmation($"Force user '{SelectedUser.SamAccountName}' to change password at next logon?", "Password Policy");
        
        var request = new PasswordChangeRequest
        {
            Username = SelectedUser.SamAccountName,
            NewPassword = newPassword,
            ConfirmPassword = newPassword,
            MustChangeAtNextLogon = mustChange
        };

        // Confirm
        if (!_dialogService.ShowConfirmation($"Reset password for user '{SelectedUser.SamAccountName}'?", "Confirm Reset"))
            return;

        bool success = false;
        string details = string.Empty;

        try
        {
            IsLoading = true;
            StatusMessage = $"Resetting password for {SelectedUser.SamAccountName}...";

            var result = await _userService.ChangePasswordAsync(request);
            if (result.IsSuccess)
            {
                _dialogService.ShowSuccess(result.Message, "Success");
                success = true;
            }
            else
            {
                _dialogService.ShowError(result.Message, "Failed");
                details = string.Join(';', result.Errors ?? new List<string>());
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error resetting password: {ex.Message}", "Error");
            _logger.LogError(ex, "Error resetting password for {Username}", SelectedUser?.SamAccountName);
            details = ex.Message;
        }
        finally
        {
            IsLoading = false;
            StatusMessage = "Ready";

            // Audit
            try
            {
                var performedBy = Environment.UserName;
                await _auditService.LogPasswordResetAsync(SelectedUser?.SamAccountName ?? "", performedBy, success, details);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write audit log for password reset");
            }
        }
    }

    private string GenerateStrongPassword(int length = 16)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string symbols = "!@#$%^&*()-_=+[{]}\\|;:'\",<.>/?`~";

        var all = upper + lower + digits + symbols;
        using var rand = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rand.GetBytes(bytes);

        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            var idx = bytes[i] % all.Length;
            sb.Append(all[idx]);
        }

        // Ensure at least one of each category
        var pw = sb.ToString().ToCharArray();
        if (!pw.Any(c => upper.Contains(c))) pw[0] = upper[0];
        if (!pw.Any(c => lower.Contains(c))) pw[1] = lower[0];
        if (!pw.Any(c => digits.Contains(c))) pw[2] = digits[0];
        if (!pw.Any(c => symbols.Contains(c))) pw[3] = symbols[0];

        return new string(pw);
    }
}

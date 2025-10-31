using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace ADManagement.WPF.ViewModels;

/// <summary>
/// ViewModel for Groups View
/// </summary>
public partial class GroupsViewModel : ObservableObject
{
    private readonly IADUserService _userService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<GroupsViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<string> _groups = new();

    [ObservableProperty]
    private string? _selectedGroup;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public GroupsViewModel(
        IADUserService userService,
        IDialogService dialogService,
        ILogger<GroupsViewModel> logger)
    {
        _userService = userService;
        _dialogService = dialogService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadUserGroupsAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            _dialogService.ShowWarning("Please enter a username.", "Username Required");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = $"Loading groups for {Username}...";

            _logger.LogInformation("Loading groups for user: {Username}", Username);

            var result = await _userService.GetUserGroupsAsync(Username);

            if (result.IsSuccess && result.Value != null)
            {
                Groups.Clear();
                foreach (var group in result.Value)
                {
                    Groups.Add(group);
                }

                StatusMessage = $"User is member of {Groups.Count} group(s)";
                _logger.LogInformation("Loaded {Count} groups for user {Username}", Groups.Count, Username);
            }
            else
            {
                StatusMessage = "Failed to load groups";
                _dialogService.ShowError(result.Message, "Load Groups Failed");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error loading groups";
            _dialogService.ShowError($"Error loading groups: {ex.Message}", "Error");
            _logger.LogError(ex, "Error loading groups for user: {Username}", Username);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddToGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            _dialogService.ShowWarning("Please enter a username.", "Username Required");
            return;
        }

        if (string.IsNullOrWhiteSpace(GroupName))
        {
            _dialogService.ShowWarning("Please enter a group name.", "Group Name Required");
            return;
        }

        if (!_dialogService.ShowConfirmation(
            $"Add user '{Username}' to group '{GroupName}'?",
            "Confirm Add to Group"))
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = $"Adding {Username} to {GroupName}...";

            var result = await _userService.AddUserToGroupAsync(Username, GroupName);

            if (result.IsSuccess)
            {
                _dialogService.ShowSuccess(result.Message, "Success");
                GroupName = string.Empty;
                await LoadUserGroupsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message, "Add to Group Failed");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error adding user to group: {ex.Message}", "Error");
            _logger.LogError(ex, "Error adding user {Username} to group {GroupName}", Username, GroupName);
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
        if (string.IsNullOrWhiteSpace(Username))
        {
            _dialogService.ShowWarning("Please enter a username.", "Username Required");
            return;
        }

        if (SelectedGroup == null)
        {
            _dialogService.ShowWarning("Please select a group to remove.", "No Group Selected");
            return;
        }

        if (!_dialogService.ShowConfirmation(
            $"Remove user '{Username}' from group '{SelectedGroup}'?",
            "Confirm Remove from Group"))
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = $"Removing {Username} from {SelectedGroup}...";

            var result = await _userService.RemoveUserFromGroupAsync(Username, SelectedGroup);

            if (result.IsSuccess)
            {
                _dialogService.ShowSuccess(result.Message, "Success");
                await LoadUserGroupsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message, "Remove from Group Failed");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error removing user from group: {ex.Message}", "Error");
            _logger.LogError(ex, "Error removing user {Username} from group {GroupName}", Username, SelectedGroup);
        }
        finally
        {
            IsLoading = false;
            StatusMessage = "Ready";
        }
    }

    [RelayCommand]
    private void ClearForm()
    {
        Username = string.Empty;
        GroupName = string.Empty;
        Groups.Clear();
        StatusMessage = "Ready";
    }
}
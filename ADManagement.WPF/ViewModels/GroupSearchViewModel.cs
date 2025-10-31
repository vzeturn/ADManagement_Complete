using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;

namespace ADManagement.WPF.ViewModels;

public partial class GroupSearchViewModel : ObservableObject
{
    private readonly IADUserService _userService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<GroupSearchViewModel> _logger;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ADGroupDto> _groups = new();

    [ObservableProperty]
    private ADGroupDto? _selectedGroup;

    [ObservableProperty]
    private string _username = string.Empty; // user to add groups to

    // Event to signal closing when used in code-behind window
    public event EventHandler? CloseRequested;

    public GroupSearchViewModel(
        IADUserService userService,
        IDialogService dialogService,
        ILogger<GroupSearchViewModel> logger)
    {
        _userService = userService;
        _dialogService = dialogService;
        _logger = logger;
    }

    public void SetParameter(object parameter)
    {
        if (parameter is string username)
        {
            Username = username;
        }
    }

    [RelayCommand]
    private async Task SearchGroupsAsync()
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            _dialogService.ShowWarning("Enter search text", "Warning");
            return;
        }

        try
        {
            var result = await _userService.SearchGroupsAsync(_searchText);
            if (result.IsSuccess && result.Value != null)
            {
                Groups.Clear();
                foreach (var g in result.Value)
                {
                    Groups.Add(g);
                }
            }
            else
            {
                _dialogService.ShowError(result.Message, "Error");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error searching groups: {ex.Message}", "Error");
            _logger.LogError(ex, "Error searching groups");
        }
    }

    [RelayCommand]
    private async Task AddSelectedGroupAsync()
    {
        if (SelectedGroup == null || string.IsNullOrWhiteSpace(Username))
        {
            _dialogService.ShowWarning("Select a group.", "Warning");
            return;
        }

        if (!_dialogService.ShowConfirmation($"Add user '{Username}' to group '{SelectedGroup.Name}'?", "Confirm"))
            return;

        var result = await _userService.AddUserToGroupAsync(Username, SelectedGroup.Name);
        if (result.IsSuccess)
        {
            _dialogService.ShowSuccess(result.Message, "Success");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            _dialogService.ShowError(result.Message, "Error");
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    // Backing field kept for source generator compatibility
    private readonly IDialogService _dialog_service = null!;
}

using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.Domain.Common;
using ADManagement.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Linq;

namespace ADManagement.WPF.ViewModels;

/// <summary>
/// Wrapper class for group search results with selection
/// </summary>
public partial class GroupSearchItem : ObservableObject
{
    private readonly ADGroupDto _group;

    public GroupSearchItem(ADGroupDto group)
    {
        _group = group;
    }

    [ObservableProperty]
    private bool _isSelected;

    public string Name => _group.Name;
    public string Description => _group.Description;
    public string DistinguishedName => _group.DistinguishedName;
    public string GroupScope => _group.GroupScope;
    public string GroupType => _group.GroupType;

    public ADGroupDto Group => _group;
}

/// <summary>
/// ViewModel for Group Search Dialog
/// </summary>
public partial class GroupSearchViewModel : ObservableObject
{
    private readonly IADGroupService _groupService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<GroupSearchViewModel> _logger;

    private string _username = string.Empty;

    public GroupSearchViewModel(
        IADGroupService groupService,
        IDialogService dialogService,
        ILogger<GroupSearchViewModel> logger)
    {
        _groupService = groupService;
        _dialogService = dialogService;
        _logger = logger;

        SearchResults = new ObservableCollection<GroupSearchItem>();
    }

    #region Properties

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<GroupSearchItem> _searchResults;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _showEmptyState = true;

    [ObservableProperty]
    private int _selectedGroupsCount;

    [ObservableProperty]
    private int _totalGroupsCount;

    [ObservableProperty]
    private bool _hasSelectedGroups;

    [ObservableProperty]
    private string _userInfo = string.Empty;

    public event EventHandler<bool>? CloseRequested;

    #endregion

    #region Public Methods

    public void SetParameter(string username)
    {
        _username = username;
        UserInfo = $"Adding groups for user: {username}";
    }

    public List<string> GetSelectedGroupDns()
    {
        return SearchResults
            .Where(g => g.IsSelected)
            .Select(g => g.DistinguishedName)
            .ToList();
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task SearchGroupsAsync()
    {
        try
        {
            IsSearching = true;
            ShowEmptyState = false;

            _logger.LogInformation("Searching groups with term: {SearchTerm}", SearchText);

            Result<IEnumerable<ADGroupDto>> result;

            // If search text is empty, get all groups
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                result = await _groupService.GetAllGroupsAsync();
            }
            else
            {
                // Perform fuzzy search
                result = await _groupService.SearchGroupsAsync(SearchText);
            }

            if (!result.IsSuccess || result.Value == null)
            {
                _dialogService.ShowError(result.Message, "Search Failed");
                SearchResults.Clear();
                ShowEmptyState = true;
                return;
            }

            // Convert to GroupSearchItems
            var items = result.Value
                .Select(g => new GroupSearchItem(g))
                .OrderBy(g => g.Name)
                .ToList();

            // Update collection
            SearchResults.Clear();
            foreach (var item in items)
            {
                // Subscribe to IsSelected changes
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(GroupSearchItem.IsSelected))
                    {
                        UpdateSelectionCounts();
                    }
                };

                SearchResults.Add(item);
            }

            TotalGroupsCount = SearchResults.Count;
            ShowEmptyState = SearchResults.Count == 0;

            if (SearchResults.Count > 0)
            {
                _logger.LogInformation("Found {Count} groups", SearchResults.Count);
            }
            else
            {
                _logger.LogInformation("No groups found matching search criteria");
            }

            UpdateSelectionCounts();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching groups");
            _dialogService.ShowError($"Error searching groups: {ex.Message}", "Error");
            SearchResults.Clear();
            ShowEmptyState = true;
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in SearchResults)
        {
            item.IsSelected = true;
        }
        UpdateSelectionCounts();
    }

    [RelayCommand]
    private void ClearAll()
    {
        foreach (var item in SearchResults)
        {
            item.IsSelected = false;
        }
        UpdateSelectionCounts();
    }

    [RelayCommand]
    private void AddSelectedGroups()
    {
        if (!HasSelectedGroups)
        {
            _dialogService.ShowWarning("Please select at least one group to add.", "No Selection");
            return;
        }

        var selectedCount = SearchResults.Count(g => g.IsSelected);

        if (!_dialogService.ShowConfirmation(
            $"Are you sure you want to add the user to {selectedCount} selected group(s)?",
            "Confirm Add to Groups"))
        {
            return;
        }

        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }

    #endregion

    #region Private Methods

    private void UpdateSelectionCounts()
    {
        SelectedGroupsCount = SearchResults.Count(g => g.IsSelected);
        HasSelectedGroups = SelectedGroupsCount > 0;
    }

    #endregion
}

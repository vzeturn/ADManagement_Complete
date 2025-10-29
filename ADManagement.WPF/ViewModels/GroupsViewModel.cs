using CommunityToolkit.Mvvm.ComponentModel;

namespace ADManagement.WPF.ViewModels;

public partial class GroupsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Groups management - Coming soon";

    public GroupsViewModel()
    {
        // TODO: Implement groups management
    }
}

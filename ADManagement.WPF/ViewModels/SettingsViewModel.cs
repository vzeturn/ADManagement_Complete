using CommunityToolkit.Mvvm.ComponentModel;

namespace ADManagement.WPF.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _domain = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private bool _useSSL;

    [ObservableProperty]
    private int _port = 389;

    public SettingsViewModel()
    {
        // TODO: Load settings from configuration
    }
}

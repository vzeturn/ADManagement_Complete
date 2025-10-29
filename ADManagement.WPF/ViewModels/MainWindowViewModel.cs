using ADManagement.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Media;

namespace ADManagement.WPF.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IADUserService _userService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly System.Windows.Threading.DispatcherTimer _timer;

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _connectionStatusText = "Not Connected";

    [ObservableProperty]
    private string _connectionStatusIcon = "CloudOff";

    [ObservableProperty]
    private Brush _connectionStatusColor = new SolidColorBrush(Colors.Orange);

    [ObservableProperty]
    private int _userCount;

    [ObservableProperty]
    private int _groupCount;

    [ObservableProperty]
    private DateTime _currentTime = DateTime.Now;

    public MainWindowViewModel(IADUserService userService, ILogger<MainWindowViewModel> logger)
    {
        _userService = userService;
        _logger = logger;

        // Setup timer for status bar clock
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => CurrentTime = DateTime.Now;
        _timer.Start();

        // Test connection on startup
        _ = TestConnectionAsync();
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            StatusMessage = "Testing connection...";
            ConnectionStatusText = "Connecting...";
            ConnectionStatusIcon = "Loading";
            ConnectionStatusColor = new SolidColorBrush(Colors.Orange);

            var result = await _userService.TestConnectionAsync();

            if (result.IsSuccess)
            {
                ConnectionStatusText = "Connected";
                ConnectionStatusIcon = "CloudCheck";
                ConnectionStatusColor = new SolidColorBrush(Colors.LightGreen);
                StatusMessage = "Connected to Active Directory";

                // Load user and group counts
                await LoadStatisticsAsync();
            }
            else
            {
                ConnectionStatusText = "Disconnected";
                ConnectionStatusIcon = "CloudOff";
                ConnectionStatusColor = new SolidColorBrush(Colors.Red);
                StatusMessage = $"Connection failed: {result.Message}";

                _logger.LogWarning("Connection test failed: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection");
            ConnectionStatusText = "Error";
            ConnectionStatusIcon = "AlertCircle";
            ConnectionStatusColor = new SolidColorBrush(Colors.Red);
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Navigate(string page)
    {
        StatusMessage = $"Navigating to {page}...";

        // TODO: Implement navigation to different view models
        CurrentViewModel = page switch
        {
            "Dashboard" => null, // DashboardViewModel
            "Users" => null, // UsersViewModel
            "Groups" => null, // GroupsViewModel
            "Export" => null, // ExportViewModel
            "OUs" => null, // OUsViewModel
            "About" => null, // AboutViewModel
            _ => null
        };

        StatusMessage = $"{page} loaded";
    }

    [RelayCommand]
    private void OpenSettings()
    {
        StatusMessage = "Opening settings...";
        // TODO: Implement settings dialog
        MessageBox.Show("Settings dialog - Coming soon!", "Settings", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task LoadStatisticsAsync()
    {
        try
        {
            var usersResult = await _userService.GetAllUsersAsync();
            if (usersResult.IsSuccess && usersResult.Value != null)
            {
                UserCount = usersResult.Value.Count();
            }

            // TODO: Load group count
            GroupCount = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading statistics");
        }
    }
}

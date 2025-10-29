using ADManagement.Application.Interfaces;
using ADManagement.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Media;

namespace ADManagement.WPF.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IADUserService _userService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;
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

    public MainWindowViewModel(
        IADUserService userService, 
        ILogger<MainWindowViewModel> logger,
        IServiceProvider serviceProvider)
    {
        _userService = userService;
        _logger = logger;
        _serviceProvider = serviceProvider;

        // Setup timer for status bar clock
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => CurrentTime = DateTime.Now;
        _timer.Start();

        // Test connection on startup
        _ = TestConnectionAsync();
        
        // Load dashboard by default
        Navigate("Dashboard");
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
    private void Navigate(string? page)
    {
        if (string.IsNullOrWhiteSpace(page))
            return;

        try
        {
            StatusMessage = $"Navigating to {page}...";

            CurrentViewModel = page switch
            {
                "Dashboard" => CreateDashboardViewModel(),
                "Users" => _serviceProvider.GetRequiredService<UsersViewModel>(),
                "Groups" => _serviceProvider.GetRequiredService<GroupsViewModel>(),
                "Export" => _serviceProvider.GetRequiredService<ExportViewModel>(),
                "OUs" => CreateOUsViewModel(),
                "About" => CreateAboutViewModel(),
                _ => null
            };

            StatusMessage = $"{page} loaded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to {Page}", page);
            StatusMessage = $"Error loading {page}: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        try
        {
            StatusMessage = "Opening settings...";
            CurrentViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
            StatusMessage = "Settings loaded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening settings");
            MessageBox.Show($"Failed to open settings: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
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

            // TODO: Load group count when group service is implemented
            GroupCount = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading statistics");
        }
    }

    private object CreateDashboardViewModel()
    {
        // Simple dashboard view model
        return new
        {
            Title = "Dashboard",
            WelcomeMessage = "Welcome to AD Management System",
            Statistics = new
            {
                TotalUsers = UserCount,
                TotalGroups = GroupCount,
                LastSync = DateTime.Now
            }
        };
    }

    private object CreateOUsViewModel()
    {
        // Simple OU view model
        return new
        {
            Title = "Organizational Units",
            Message = "OU management - Coming soon"
        };
    }

    private object CreateAboutViewModel()
    {
        // Simple about view model
        return new
        {
            Title = "About",
            ApplicationName = "AD Management System",
            Version = "1.0.0",
            Description = "Active Directory Management Tool built with .NET 9.0 and WPF",
            Copyright = $"© {DateTime.Now.Year} - All rights reserved"
        };
    }

    // Cleanup
    public void Dispose()
    {
        _timer?.Stop();
    }
}
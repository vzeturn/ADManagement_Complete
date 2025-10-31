using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows.Media;

namespace ADManagement.WPF.ViewModels;

public partial class CreateUserViewModel : ObservableObject
{
    private readonly IADUserService _userService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<CreateUserViewModel> _logger;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _department = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private bool _accountEnabled = true;

    [ObservableProperty]
    private bool _mustChangePasswordOnNextLogon = true;

    [ObservableProperty]
    private bool _passwordNeverExpires = false;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private Brush _statusColor = Brushes.Black;

    private string? _password;
    private string? _confirmPassword;

    public CreateUserViewModel()
    {
        // Resolve services from DI
        var services = App.Services;
        _userService = services?.GetRequiredService<IADUserService>() 
            ?? throw new InvalidOperationException("Service provider not available");
        _dialogService = services?.GetRequiredService<IDialogService>() 
            ?? throw new InvalidOperationException("Service provider not available");
        _logger = services?.GetService<ILogger<CreateUserViewModel>>() 
            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CreateUserViewModel>.Instance;
    }

    public CreateUserViewModel(IADUserService userService, IDialogService dialogService, ILogger<CreateUserViewModel> logger)
    {
        _userService = userService;
        _dialogService = dialogService;
        _logger = logger;
    }

    public void SetPassword(string password) => _password = password;
    public void SetConfirmPassword(string password) => _confirmPassword = password;

    [RelayCommand]
    private async Task CreateUserAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ShowError("Username is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            ShowError("First name and last name are required");
            return;
        }

        if (string.IsNullOrWhiteSpace(_password))
        {
            ShowError("Password is required");
            return;
        }

        if (_password != _confirmPassword)
        {
            ShowError("Passwords do not match");
            return;
        }

        try
        {
            StatusMessage = "Creating user...";
            StatusColor = Brushes.Blue;

            var request = new CreateUserRequest
            {
                Username = Username,
                FirstName = FirstName,
                LastName = LastName,
                Password = _password!,
                DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? $"{FirstName} {LastName}".Trim() : DisplayName,
                Email = Email,
                Department = Department,
                Title = Title,
                PhoneNumber = PhoneNumber,
                MustChangePasswordOnNextLogon = MustChangePasswordOnNextLogon,
                AccountEnabled = AccountEnabled,
                PasswordNeverExpires = PasswordNeverExpires
            };

            var result = await _userService.CreateUserAsync(request);

            if (result.IsSuccess)
            {
                StatusMessage = "User created successfully!";
                StatusColor = Brushes.Green;
                _dialogService.ShowSuccess("User created successfully!", "Success");
                
                // Close dialog after short delay
                await Task.Delay(500);
                CloseDialog?.Invoke(true);
            }
            else
            {
                ShowError(result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            ShowError($"Error creating user: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        StatusMessage = message;
        StatusColor = Brushes.Red;
        _dialogService.ShowError(message, "Error");
    }

    public Action<bool>? CloseDialog { get; set; }
}


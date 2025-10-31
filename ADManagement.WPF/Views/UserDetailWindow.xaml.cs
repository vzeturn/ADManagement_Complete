using System.Windows;
using ADManagement.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ADManagement.WPF.Views;

/// <summary>
/// Interaction logic for UserDetailWindow.xaml
/// </summary>
public partial class UserDetailWindow : Window
{
    public UserDetailWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Constructor with username parameter
    /// </summary>
    public UserDetailWindow(string username) : this()
    {
        // Get ViewModel from DI and set parameter
        var viewModel = App.Services?.GetRequiredService<UserDetailWindowViewModel>();
        if (viewModel != null)
        {
            DataContext = viewModel;
            viewModel.SetUsername(username);

            // Subscribe to close request
            viewModel.CloseRequested += (s, e) =>
            {
                DialogResult = e;
                Close();
            };
        }
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // Load user data after window is rendered
        if (DataContext is UserDetailWindowViewModel viewModel)
        {
            await viewModel.LoadUserDataAsync();
        }
    }
}

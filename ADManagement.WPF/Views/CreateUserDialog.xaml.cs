using ADManagement.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace ADManagement.WPF.Views;

public partial class CreateUserDialog : Window
{
    public CreateUserDialog()
    {
        InitializeComponent();
        
        // Get ViewModel from DI
        var services = App.Services;
        if (services != null)
        {
            DataContext = services.GetRequiredService<CreateUserViewModel>();
            if (DataContext is CreateUserViewModel vm)
            {
                vm.CloseDialog = (success) => DialogResult = success;
            }
        }

        // Bind password boxes
        PasswordBox.PasswordChanged += (s, e) =>
        {
            if (DataContext is CreateUserViewModel viewModel)
                viewModel.SetPassword(PasswordBox.Password);
        };
        
        ConfirmPasswordBox.PasswordChanged += (s, e) =>
        {
            if (DataContext is CreateUserViewModel viewModel)
                viewModel.SetConfirmPassword(ConfirmPasswordBox.Password);
        };
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}


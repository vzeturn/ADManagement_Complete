using System.Windows;
using ADManagement.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ADManagement.WPF.Views;

/// <summary>
/// Interaction logic for GroupSearchDialog.xaml
/// </summary>
public partial class GroupSearchDialog : Window
{
    public GroupSearchDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Constructor with username parameter
    /// </summary>
    public GroupSearchDialog(string username) : this()
    {
        // Get ViewModel from DI
        var viewModel = App.Services?.GetRequiredService<GroupSearchViewModel>();
        if (viewModel != null)
        {
            DataContext = viewModel;
            viewModel.SetParameter(username);

            // Subscribe to close request
            viewModel.CloseRequested += (s, success) =>
            {
                DialogResult = success;
                Close();
            };
        }
    }
}

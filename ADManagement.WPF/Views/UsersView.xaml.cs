using System.Windows.Controls;
using System.Windows.Input;
using ADManagement.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ADManagement.WPF.Views;

/// <summary>
/// Interaction logic for UsersView.xaml
/// </summary>
public partial class UsersView : UserControl
{
    public UsersView()
    {
        InitializeComponent();
    }

    private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is UsersViewModel && UsersDataGrid.SelectedItem is ADManagement.Application.DTOs.ADUserDto user)
        {
            var nav = App.Services?.GetRequiredService<ADManagement.WPF.Services.INavigationService>();
            nav?.NavigateTo<UserDetailsViewModel>(user);
        }
    }
}
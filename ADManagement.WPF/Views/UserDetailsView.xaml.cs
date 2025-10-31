using System.Windows.Controls;
using System.Windows.Input;
using ADManagement.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace ADManagement.WPF.Views;

public partial class UserDetailsView : UserControl
{
    public UserDetailsView()
    {
        InitializeComponent();
    }

    private void OpenGroupSearchDialog(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserDetailsViewModel vm && vm.SelectedUser != null)
        {
            var groupSearchWindow = new GroupSearchDialog();
            var gsVm = App.Services?.GetRequiredService<GroupSearchViewModel>();
            if (gsVm != null)
            {
                gsVm.SetParameter(vm.SelectedUser.SamAccountName);
                // subscribe to close request
                gsVm.CloseRequested += (s, args) => groupSearchWindow.Close();

                groupSearchWindow.DataContext = gsVm;
                groupSearchWindow.Owner = Window.GetWindow(this);
                groupSearchWindow.ShowDialog();

                // After modal close, refresh groups
                _ = vm.RefreshUserGroupsAsync();
            }
        }
    }
}

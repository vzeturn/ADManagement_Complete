using System.Windows.Controls;

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

    private void DataGridRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is ADManagement.WPF.ViewModels.UsersViewModel vm)
        {
            var user = UsersDataGrid.SelectedItem as ADManagement.Application.DTOs.ADUserDto;
            vm.OpenDetailsCommand.Execute(user);
        }
    }
}
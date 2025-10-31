using System.Windows;
using ADManagement.WPF.ViewModels;

namespace ADManagement.WPF.Views;

public partial class GroupSearchDialog : Window
{
    public GroupSearchDialog()
    {
        InitializeComponent();
        this.Loaded += GroupSearchDialog_Loaded;
    }

    private void GroupSearchDialog_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GroupSearchViewModel vm)
        {
            vm.CloseRequested += (s, args) => this.Close();
        }
    }
}

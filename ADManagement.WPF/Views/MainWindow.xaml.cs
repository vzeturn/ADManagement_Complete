using ADManagement.WPF.ViewModels;
using System.Windows;

namespace ADManagement.WPF.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

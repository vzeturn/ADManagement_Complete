using Microsoft.Win32;
using System.Windows;

namespace ADManagement.WPF.Services;

/// <summary>
/// Implementation of IDialogService for WPF
/// </summary>
public class DialogService : IDialogService
{
    public void ShowInformation(string message, string title = "Information")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowSuccess(string message, string title = "Success")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowWarning(string message, string title = "Warning")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void ShowError(string message, string title = "Error")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool ShowConfirmation(string message, string title = "Confirm")
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public string? ShowSaveFileDialog(string filter = "All files (*.*)|*.*", string defaultFileName = "")
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            FileName = defaultFileName,
            AddExtension = true
        };

        if (dialog.ShowDialog() == true)
        {
            return dialog.FileName;
        }

        return null;
    }

    public string? ShowOpenFileDialog(string filter = "All files (*.*)|*.*")
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            return dialog.FileName;
        }

        return null;
    }

    public string? ShowFolderBrowserDialog(string description = "Select Folder")
    {
        // Using Windows Forms FolderBrowserDialog
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = description,
            ShowNewFolderButton = true,
            UseDescriptionForTitle = true
        };

        var result = dialog.ShowDialog();
        if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return dialog.SelectedPath;
        }

        return null;
    }

    public string? ShowInputDialog(string message, string title = "Input", string defaultValue = "")
    {
        // Create a simple input dialog window
        var inputWindow = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize
        };

        var stackPanel = new System.Windows.Controls.StackPanel
        {
            Margin = new Thickness(16)
        };

        var textBlock = new System.Windows.Controls.TextBlock
        {
            Text = message,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var textBox = new System.Windows.Controls.TextBox
        {
            Text = defaultValue,
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };

        var okButton = new System.Windows.Controls.Button
        {
            Content = "OK",
            Width = 80,
            Height = 32,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "Cancel",
            Width = 80,
            Height = 32,
            IsCancel = true
        };

        okButton.Click += (s, e) =>
        {
            inputWindow.DialogResult = true;
            inputWindow.Close();
        };

        cancelButton.Click += (s, e) =>
        {
            inputWindow.DialogResult = false;
            inputWindow.Close();
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        stackPanel.Children.Add(textBlock);
        stackPanel.Children.Add(textBox);
        stackPanel.Children.Add(buttonPanel);

        inputWindow.Content = stackPanel;

        textBox.Focus();
        textBox.SelectAll();

        if (inputWindow.ShowDialog() == true)
        {
            return textBox.Text;
        }

        return null;
    }
}
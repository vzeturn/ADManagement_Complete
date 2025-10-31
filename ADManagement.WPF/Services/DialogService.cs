using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Input;

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
        var dialog = new OpenFolderDialog
        {
            Title = "Select Folder",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        
        if (dialog.ShowDialog() == true)
        {
            return dialog.FolderName;
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

    public (string? Username, string? Password)? ShowCredentialsDialog(string message = "Enter domain credentials", string title = "Credentials")
    {
        var window = new Window
        {
            Title = title,
            Width = 420,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize
        };

        var grid = new Grid { Margin = new Thickness(12) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var text = new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 8) };
        Grid.SetRow(text, 0);
        grid.Children.Add(text);

        var userBox = new TextBox { Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(8) };
        Grid.SetRow(userBox, 1);
        grid.Children.Add(userBox);

        var passBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(8) };
        Grid.SetRow(passBox, 2);
        grid.Children.Add(passBox);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var ok = new Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "Cancel", Width = 80, IsCancel = true };

        ok.Click += (s, e) => { window.DialogResult = true; window.Close(); };
        cancel.Click += (s, e) => { window.DialogResult = false; window.Close(); };

        buttonPanel.Children.Add(ok);
        buttonPanel.Children.Add(cancel);
        Grid.SetRow(buttonPanel, 3);
        grid.Children.Add(buttonPanel);

        window.Content = grid;

        if (window.ShowDialog() == true)
        {
            return (userBox.Text, passBox.Password);
        }

        return null;
    }

	public async Task<(string? Username, string? Password)?> ShowCredentialsDialogWithValidationAsync(
		Func<string, string, Task<bool>> validator,
		string message = "Enter domain credentials",
		string title = "Credentials")
	{
		var window = new Window
		{
			Title = title,
			Width = 460,
			Height = 260,
			WindowStartupLocation = WindowStartupLocation.CenterScreen,
			ResizeMode = ResizeMode.NoResize
		};

		var grid = new Grid { Margin = new Thickness(12) };
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		var text = new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 8) };
		Grid.SetRow(text, 0);
		grid.Children.Add(text);

		var userLabel = new TextBlock { Text = "Username", Margin = new Thickness(0, 0, 0, 2) };
		Grid.SetRow(userLabel, 1);
		grid.Children.Add(userLabel);

		var userBox = new TextBox { Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(8) };
		Grid.SetRow(userBox, 2);
		grid.Children.Add(userBox);

		var passLabel = new TextBlock { Text = "Password", Margin = new Thickness(0, 0, 0, 2) };
		Grid.SetRow(passLabel, 3);
		grid.Children.Add(passLabel);

		var passBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(8) };
		Grid.SetRow(passBox, 4);
		grid.Children.Add(passBox);

		var statusText = new TextBlock { Text = string.Empty, Margin = new Thickness(0, 0, 0, 8), Foreground = System.Windows.Media.Brushes.OrangeRed };
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		Grid.SetRow(statusText, 5);
		grid.Children.Add(statusText);

		var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
		var ok = new Button { Content = "Connect", Width = 100, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
		var cancel = new Button { Content = "Cancel", Width = 100, IsCancel = true };

		ok.Click += async (s, e) =>
		{
			statusText.Text = string.Empty;
			ok.IsEnabled = false;
			cancel.IsEnabled = false;
			userBox.IsEnabled = false;
			passBox.IsEnabled = false;
			Mouse.OverrideCursor = Cursors.Wait;
			try
			{
				statusText.Text = "Testing credentials...";
				var isValid = false;
				try
				{
					isValid = await validator(userBox.Text ?? string.Empty, passBox.Password ?? string.Empty);
				}
				catch (Exception ex)
				{
					statusText.Text = $"Validation error: {ex.Message}";
				}

				if (isValid)
				{
					window.DialogResult = true;
					window.Close();
					return;
				}

				statusText.Text = "Authentication failed. Please check your username and password and try again.";
			}
			finally
			{
				Mouse.OverrideCursor = null;
				ok.IsEnabled = true;
				cancel.IsEnabled = true;
				userBox.IsEnabled = true;
				passBox.IsEnabled = true;
			}
		};

		cancel.Click += (s, e) => { window.DialogResult = false; window.Close(); };

		buttonPanel.Children.Add(ok);
		buttonPanel.Children.Add(cancel);
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		Grid.SetRow(buttonPanel, 6);
		grid.Children.Add(buttonPanel);

		window.Content = grid;

		var result = window.ShowDialog();
		if (result == true)
		{
			return (userBox.Text, passBox.Password);
		}
		return null;
	}
}
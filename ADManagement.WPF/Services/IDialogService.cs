namespace ADManagement.WPF.Services;

/// <summary>
/// Service interface for showing dialogs and messages
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows an information message
    /// </summary>
    void ShowInformation(string message, string title = "Information");
    
    /// <summary>
    /// Shows a success message
    /// </summary>
    void ShowSuccess(string message, string title = "Success");
    
    /// <summary>
    /// Shows a warning message
    /// </summary>
    void ShowWarning(string message, string title = "Warning");
    
    /// <summary>
    /// Shows an error message
    /// </summary>
    void ShowError(string message, string title = "Error");
    
    /// <summary>
    /// Shows a confirmation dialog
    /// </summary>
    /// <returns>True if user clicked Yes/OK, false otherwise</returns>
    bool ShowConfirmation(string message, string title = "Confirm");
    
    /// <summary>
    /// Shows a file save dialog
    /// </summary>
    /// <returns>Selected file path or null if cancelled</returns>
    string? ShowSaveFileDialog(string filter = "All files (*.*)|*.*", string defaultFileName = "");
    
    /// <summary>
    /// Shows a file open dialog
    /// </summary>
    /// <returns>Selected file path or null if cancelled</returns>
    string? ShowOpenFileDialog(string filter = "All files (*.*)|*.*");
    
    /// <summary>
    /// Shows a folder browser dialog
    /// </summary>
    /// <returns>Selected folder path or null if cancelled</returns>
    string? ShowFolderBrowserDialog(string description = "Select Folder");
    
    /// <summary>
    /// Shows an input dialog
    /// </summary>
    /// <returns>User input or null if cancelled</returns>
    string? ShowInputDialog(string message, string title = "Input", string defaultValue = "");

    /// <summary>
    /// Shows a credentials dialog for domain authentication
    /// </summary>
    /// <returns>A tuple of username and password, or null if cancelled</returns>
    (string? Username, string? Password)? ShowCredentialsDialog(string message = "Enter domain credentials", string title = "Credentials");

	/// <summary>
	/// Shows a credentials dialog that validates input inline before closing.
	/// The validator should return true if the provided username/password are valid.
	/// On success the dialog closes and returns the provided credentials; on failure it stays open and shows the error.
	/// Returns null if the user cancels.
	/// </summary>
	Task<(string? Username, string? Password)?> ShowCredentialsDialogWithValidationAsync(
		Func<string, string, Task<bool>> validator,
		string message = "Enter domain credentials",
		string title = "Credentials");
}
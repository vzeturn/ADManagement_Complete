namespace ADManagement.WPF.Services;

public interface IDialogService
{
    void ShowMessage(string message, string title = "Information");
    void ShowError(string message, string title = "Error");
    void ShowWarning(string message, string title = "Warning");
    bool ShowConfirmation(string message, string title = "Confirmation");
    string? ShowSaveFileDialog(string filter, string defaultFileName);
    string? ShowOpenFileDialog(string filter);
}

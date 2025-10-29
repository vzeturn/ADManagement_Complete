namespace ADManagement.WPF.Services;

public interface INavigationService
{
    void NavigateTo(string viewName);
    void GoBack();
    bool CanGoBack { get; }
}

public class NavigationService : INavigationService
{
    public bool CanGoBack => false; // TODO: Implement navigation history

    public void GoBack()
    {
        // TODO: Implement navigation history
    }

    public void NavigateTo(string viewName)
    {
        // TODO: Implement view navigation
    }
}

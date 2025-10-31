namespace ADManagement.WPF.Services;

/// <summary>
/// Service interface for navigation between views
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to a view
    /// </summary>
    void NavigateTo<TViewModel>() where TViewModel : class;
    
    /// <summary>
    /// Navigates to a view with parameter
    /// </summary>
    void NavigateTo<TViewModel>(object parameter) where TViewModel : class;
    
    /// <summary>
    /// Gets the current view model
    /// </summary>
    object? CurrentViewModel { get; }
    
    /// <summary>
    /// Event raised when navigation occurs
    /// </summary>
    event EventHandler<object>? NavigationChanged;
}
using Microsoft.Extensions.DependencyInjection;

namespace ADManagement.WPF.Services;

/// <summary>
/// Implementation of INavigationService
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private object? _currentViewModel;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            _currentViewModel = value;
            NavigationChanged?.Invoke(this, _currentViewModel!);
        }
    }

    public event EventHandler<object>? NavigationChanged;

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        CurrentViewModel = viewModel;
    }

    public void NavigateTo<TViewModel>(object parameter) where TViewModel : class
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        
        // If viewModel has a method to receive parameters, call it
        var parameterMethod = viewModel.GetType().GetMethod("SetParameter");
        parameterMethod?.Invoke(viewModel, new[] { parameter });
        
        CurrentViewModel = viewModel;
    }
}
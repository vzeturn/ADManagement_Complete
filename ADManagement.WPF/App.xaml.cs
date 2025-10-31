using ADManagement.Application;
using ADManagement.Application.Interfaces;
using ADManagement.Infrastructure;
using ADManagement.WPF.Services;
using ADManagement.WPF.ViewModels;
using ADManagement.WPF.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Windows;

namespace ADManagement.WPF;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    public App()
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/admanagement-wpf-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            Log.Information("Starting ADManagement WPF Application");

            _host = CreateHostBuilder(e.Args).Build();
            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            Log.Information("Application started successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            MessageBox.Show($"Failed to start application: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("Application shutting down");

        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                    .AddUserSecrets<App>(optional: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Add Application Services
                services.AddApplicationServices();

                // Add Infrastructure Services
                services.AddInfrastructureServices(context.Configuration);

                // Add WPF Services
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<INavigationService, NavigationService>();

                // Add ViewModels (Transient - new instance each time)
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<UsersViewModel>();
                services.AddTransient<GroupsViewModel>();
                services.AddTransient<ExportViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Add Main Window (Singleton)
                services.AddSingleton<MainWindow>();
            });
}
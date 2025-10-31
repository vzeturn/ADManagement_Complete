using ADManagement.Application;
using ADManagement.Application.Interfaces;
using ADManagement.Infrastructure;
using ADManagement.Infrastructure.Services;
using ADManagement.WPF.Services;
using ADManagement.WPF.ViewModels;
using ADManagement.WPF.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Windows;
using System.Text;

namespace ADManagement.WPF;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    // Expose the application's service provider for resolving windows/viewmodels from code-behind
    public static IServiceProvider? Services => Current is App app ? app._host?.Services : null;

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

            var dialog = _host.Services.GetRequiredService<IDialogService>();
            var credService = _host.Services.GetRequiredService<ICredentialService>();
            var adConfig = _host.Services.GetRequiredService<ADManagement.Application.Configuration.ADConfiguration>();

            // Load saved credentials if available
            if (credService.TryLoadCredentials(out var savedUser, out var savedPass))
            {
                adConfig.Username = savedUser;
                adConfig.Password = savedPass;
                Log.Information("Loaded saved credentials for user: {Username}", savedUser);
            }

            // Perform a quick connection test
            var quickOk = await RunQuickConnectionTestWithScopeAsync();

            if (!quickOk)
            {
                // Allow attempts with credential prompt
                const int maxAttempts = 3;
                int attempt = 0;
                bool authenticated = false;

                while (attempt < maxAttempts && !authenticated)
                {
                    attempt++;
                    Log.Information("Prompting for credentials (attempt {Attempt}/{MaxAttempts})", attempt, maxAttempts);

                    var creds = dialog.ShowCredentialsDialog(
                        $"Enter domain credentials to connect to {adConfig.Domain}:", 
                        "AD Credentials"
                    );

                    if (creds == null)
                    {
                        var cont = MessageBox.Show(
                            "No credentials provided. Do you want to retry?",
                            "Authentication Required",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );

                        if (cont == MessageBoxResult.No)
                        {
                            Log.Information("User cancelled credential entry - shutting down");
                            Shutdown();
                            return;
                        }
                        else continue;
                    }

                    adConfig.Username = creds.Value.Username ?? string.Empty;
                    adConfig.Password = creds.Value.Password ?? string.Empty;

                    Log.Information("Attempting connection with new credentials for user: {Username}", adConfig.Username);
                    quickOk = await RunQuickConnectionTestWithScopeAsync();

                    if (quickOk)
                    {
                        authenticated = true;
                        Log.Information("Successfully authenticated with provided credentials");
                        
                        // Save credentials for next run
                        try
                        {
                            credService.SaveCredentials(adConfig.Username, adConfig.Password);
                            Log.Information("Saved credentials for future use");
                        }
                        catch (Exception ex) 
                        {
                            Log.Warning(ex, "Failed to save credentials");
                        }
                        break;
                    }

                    var tryAgain = MessageBox.Show(
                        $"Connection attempt {attempt} failed. Would you like to try again with different credentials?",
                        "Connection Failed",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );

                    if (tryAgain == MessageBoxResult.No)
                    {
                        Log.Information("User chose not to retry after failed attempt - shutting down");
                        Shutdown();
                        return;
                    }
                }

                bool windowShown = false;

                if (!authenticated && !quickOk)
                {
                    Log.Warning("All authentication attempts failed - opening settings");
                    
                    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                    mainWindow.Show();
                    windowShown = true;

                    var nav = _host.Services.GetRequiredService<INavigationService>();
                    nav.NavigateTo<SettingsViewModel>();

                    MessageBox.Show(
                        "Connection failed after multiple attempts. The application will open in Settings mode so you can update the AD configuration.",
                        "Connection Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }

                if (!windowShown)
                {
                    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                    mainWindow.Show();
                }
            }
            else
            {
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }

            Log.Information("Application started successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            MessageBox.Show(
                $"Failed to start application: {ex.Message}\n\nCheck the logs for more details.",
                "Fatal Error", 
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            Shutdown();
        }
    }

    private async Task<bool> RunQuickConnectionTestWithScopeAsync()
    {
        try
        {
            using var scope = _host!.Services.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IADUserService>();
            var result = await userService.TestConnectionAsync();
            
            if (!result.IsSuccess)
            {
                var message = "Failed to connect to Active Directory. ";
                if (result.Errors?.Any() == true)
                {
                    if (result.Errors.Any(e => e.Contains("server could not be contacted")))
                    {
                        message += "\n\nPossible causes:" +
                                  "\n- LDAP server address is incorrect" +
                                  "\n- Server is not reachable on the network" +
                                  "\n- Firewall is blocking LDAP ports (389/636)" +
                                  "\n\nPlease verify your network connection and LDAP settings.";
                    }
                    else if (result.Errors.Any(e => e.Contains("invalid credentials")))
                    {
                        message += "\n\nThe provided credentials are invalid." +
                                  "\nPlease verify your username and password.";
                    }
                    else
                    {
                        message += "\n\n" + string.Join("\n", result.Errors);
                    }
                }

                Log.Warning("Connection test failed: {Message}", message);
            }

            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            var errorMessage = ex switch
            {
                System.DirectoryServices.AccountManagement.PrincipalServerDownException => 
                    "Could not reach the LDAP server. Please verify the server address and your network connection.",
                System.DirectoryServices.Protocols.LdapException ldapEx => 
                    $"LDAP Error: {ldapEx.Message}. Please check your connection settings.",
                System.Security.Authentication.AuthenticationException => 
                    "Authentication failed. Please verify your credentials.",
                _ => $"Unexpected error: {ex.Message}"
            };

            Log.Warning(ex, "Quick connection test failed: {ErrorMessage}", errorMessage);
            return false;
        }
    }

    // Keep full diagnostics method for manual diagnostics UI if needed
    private async Task<DiagnosticsResult?> RunDiagnosticsWithScopeAsync()
    {
        try
        {
            using var scope = _host!.Services.CreateScope();
            var diagnostics = scope.ServiceProvider.GetRequiredService<ADConnectionDiagnosticsService>();
            var result = await diagnostics.RunFullDiagnosticsAsync();
            return result;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Diagnostics run failed");
            return null;
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
                services.AddSingleton<ICredentialService, CredentialService>();

                // Add ViewModels (Transient - new instance each time)
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<UsersViewModel>();
                services.AddTransient<GroupsViewModel>();
                services.AddTransient<ExportViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<UserDetailsViewModel>();
                services.AddTransient<GroupSearchViewModel>();

                // Add Windows (Transient)
                services.AddTransient<MainWindow>();
                services.AddTransient<GroupSearchDialog>();

                // Register AuditService
                services.AddSingleton<ADManagement.Application.Interfaces.IAuditService, ADManagement.Infrastructure.Services.AuditService>();
            });
}
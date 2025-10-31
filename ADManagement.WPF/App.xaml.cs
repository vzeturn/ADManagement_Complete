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
using ADManagement.Domain.Common;
using System.Linq;

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
            var credProvider = _host.Services.GetRequiredService<ICredentialProvider>();
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();

            // Try connecting with current/saved credentials
            bool isConnected = await TryConnectAsync();

            if (!isConnected && credProvider.HasCredentials)
            {
                // Clear invalid saved credentials
                credProvider.ClearCredentials();
            }

            // Show main window early
            mainWindow.Show();

            if (!isConnected)
            {
                // Try getting credentials from user
                var creds = dialog.ShowCredentialsDialog(
                    "Enter your domain credentials to connect to Active Directory:",
                    "AD Authentication Required"
                );

                if (creds != null)
                {
                    credProvider.SetCredentials(
                        creds.Value.Username ?? string.Empty,
                        creds.Value.Password ?? string.Empty
                    );

                    isConnected = await TryConnectAsync();
                }
            }

            if (!isConnected)
            {
                Log.Warning("No valid credentials provided - opening settings");
                var nav = _host.Services.GetRequiredService<INavigationService>();
                nav.NavigateTo<SettingsViewModel>();

                MessageBox.Show(
                    "Please configure your Active Directory connection settings and credentials.",
                    "Configuration Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
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

    private async Task<bool> TryConnectAsync()
    {
        try
        {
            using var scope = _host!.Services.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IADUserService>();

            // Log connection info (mask password)
            var adConfig = scope.ServiceProvider.GetRequiredService<ADManagement.Application.Configuration.ADConfiguration>();
            var credProvider = scope.ServiceProvider.GetService<ICredentialProvider>();

            var usernameDisplay = string.IsNullOrWhiteSpace(adConfig.Username)
                ? "(using current Windows credentials)"
                : adConfig.Username;

            var maskedPassword = string.IsNullOrWhiteSpace(adConfig.Password) ? "(none)" : new string('*', 8);
            var credSource = credProvider != null && credProvider.HasCredentials ? "CredentialProvider (cached)" : "Configuration / Environment";

            Log.Information("Attempting AD connection: Domain={Domain}, LdapServer={LdapServer}, Port={Port}, UseSSL={UseSSL}, Username={UsernameDisplay}, CredSource={CredSource}",
                adConfig.Domain ?? "(none)",
                string.IsNullOrWhiteSpace(adConfig.LdapServer) ? "(auto)" : adConfig.LdapServer,
                adConfig.Port, adConfig.UseSSL, usernameDisplay, credSource);

            var result = await userService.TestConnectionAsync();

            if (!result.IsSuccess)
            {
                var message = "Failed to connect to Active Directory. ";
                if (result.Errors?.Any() == true)
                {
                    var error = result.Errors.FirstOrDefault()?.ToLowerInvariant() ?? string.Empty;

                    if (error.Contains("server could not be contacted") || error.Contains("server down"))
                    {
                        message += "\n\nNetwork Connection Error:" +
                                  "\n- Check if the LDAP server address is correct" +
                                  "\n- Verify your network connection" +
                                  "\n- Ensure LDAP ports (389/636) are not blocked";
                    }
                    else if (error.Contains("invalid credentials") || error.Contains("authentication"))
                    {
                        message += "\n\nAuthentication Error:" +
                                  "\n- Verify your username and password are correct" +
                                  "\n- Ensure your account is not locked or expired" +
                                  "\n- Check if you have permission to access the domain";
                    }
                    else
                    {
                        message += "\n\nDetails: " + string.Join("\n", result.Errors);
                    }
                }

                Log.Warning("Connection test failed: {Message}", message);

                MessageBox.Show(
                    message,
                    "Connection Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
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

            // Log full exception with stack trace
            Log.Warning(ex, "Connection test failed: {ErrorMessage}", errorMessage);

            MessageBox.Show(
                errorMessage,
                "Connection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

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
                services.AddSingleton<ADManagement.Application.Interfaces.ICredentialService, ADManagement.Infrastructure.Services.CredentialService>();
                services.AddSingleton<ICredentialProvider, CredentialProvider>();

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
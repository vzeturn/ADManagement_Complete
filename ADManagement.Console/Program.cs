using ADManagement.Application;
using ADManagement.Application.Interfaces;
using ADManagement.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ADManagement.Infrastructure.Services;
using ADManagement.Infrastructure.Logging;

namespace ADManagement.Console;

class Program
{
    static async Task Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/admanagement-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting AD Management Console Application");

            var host = CreateHostBuilder(args).Build();

            using var scope = host.Services.CreateScope();
            var app = scope.ServiceProvider.GetRequiredService<EnhancedConsoleApp>();

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                    .AddUserSecrets<Program>(optional: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Add Application Services
                services.AddApplicationServices();


                // Add Infrastructure Services
                services.AddInfrastructureServices(context.Configuration);
                services.AddScoped<ADConnectionDiagnosticsService>();
                services.AddScoped<EnhancedConsoleApp>();
            });

    static async Task RunApplication(IServiceProvider services)
    {
        var userService = services.GetRequiredService<IADUserService>();
        var exportService = services.GetRequiredService<IExportService>();

        System.Console.WriteLine("╔══════════════════════════════════════════════╗");
        System.Console.WriteLine("║   AD Management Console Application          ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════╝");
        System.Console.WriteLine();

        // Test connection
        System.Console.WriteLine("Testing Active Directory connection...");
        var connectionResult = await userService.TestConnectionAsync();
        
        if (!connectionResult.IsSuccess)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"❌ Connection failed: {connectionResult.Message}");
            System.Console.ResetColor();
            return;
        }

        System.Console.ForegroundColor = ConsoleColor.Green;
        System.Console.WriteLine("✓ Connection successful");
        System.Console.ResetColor();
        System.Console.WriteLine();

        bool running = true;
        while (running)
        {
            ShowMenu();
            var choice = System.Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await ExportAllUsers(exportService);
                    break;
                case "2":
                    await SearchUsers(userService);
                    break;
                case "3":
                    await GetUserDetails(userService);
                    break;
                case "4":
                    await EnableDisableUser(userService);
                    break;
                case "5":
                    await UnlockUser(userService);
                    break;
                case "6":
                    await ChangePassword(userService);
                    break;
                case "7":
                    await ExportGroups(exportService);
                    break;
                case "8":
                    await ManageUserGroups(userService);
                    break;
                case "0":
                    running = false;
                    break;
                default:
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.WriteLine("Invalid choice. Please try again.");
                    System.Console.ResetColor();
                    break;
            }

            if (running)
            {
                System.Console.WriteLine("\nPress any key to continue...");
                System.Console.ReadKey();
            }
        }

        System.Console.WriteLine("\nThank you for using AD Management Console!");
    }

    static void ShowMenu()
    {
        System.Console.Clear();
        System.Console.WriteLine("╔══════════════════════════════════════════════╗");
        System.Console.WriteLine("║              MAIN MENU                       ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════╝");
        System.Console.WriteLine();
        System.Console.WriteLine("  1. Export All Users to Excel");
        System.Console.WriteLine("  2. Search Users");
        System.Console.WriteLine("  3. Get User Details");
        System.Console.WriteLine("  4. Enable/Disable User");
        System.Console.WriteLine("  5. Unlock User Account");
        System.Console.WriteLine("  6. Change User Password");
        System.Console.WriteLine("  7. Export All Groups to Excel");
        System.Console.WriteLine("  8. Manage User Groups");
        System.Console.WriteLine("  0. Exit");
        System.Console.WriteLine();
        System.Console.Write("Enter your choice: ");
    }

    static async Task ExportAllUsers(IExportService exportService)
    {
        System.Console.WriteLine("\n--- Export All Users ---");
        System.Console.Write("Enter output file path (or press Enter for default): ");
        var filePath = System.Console.ReadLine();

        System.Console.WriteLine("Exporting users...");
        var result = await exportService.ExportAllUsersAsync(
            string.IsNullOrWhiteSpace(filePath) ? null : filePath);

        if (result.IsSuccess)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"✓ {result.Message}");
            System.Console.WriteLine($"  File: {result.Value}");
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"❌ {result.Message}");
        }
        System.Console.ResetColor();
    }

    static async Task SearchUsers(IADUserService userService)
    {
        System.Console.WriteLine("\n--- Search Users ---");
        System.Console.Write("Enter search term: ");
        var searchTerm = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            System.Console.WriteLine("Search term cannot be empty.");
            return;
        }

        System.Console.WriteLine("Searching...");
        var result = await userService.SearchUsersAsync(searchTerm);

        if (result.IsSuccess && result.Value != null)
        {
            var users = result.Value.ToList();
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"\n✓ Found {users.Count} user(s):\n");
            System.Console.ResetColor();

            foreach (var user in users)
            {
                System.Console.WriteLine($"  • {user.Username,-20} {user.DisplayName,-30} {user.Email}");
            }
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"❌ {result.Message}");
            System.Console.ResetColor();
        }
    }

    static async Task GetUserDetails(IADUserService userService)
    {
        System.Console.WriteLine("\n--- Get User Details ---");
        System.Console.Write("Enter username: ");
        var username = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(username))
        {
            System.Console.WriteLine("Username cannot be empty.");
            return;
        }

        System.Console.WriteLine("Retrieving user details...");
        var result = await userService.GetUserByUsernameAsync(username);

        if (result.IsSuccess && result.Value != null)
        {
            var user = result.Value;
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("\n✓ User Details:\n");
            System.Console.ResetColor();

            System.Console.WriteLine($"  Username:        {user.Username}");
            System.Console.WriteLine($"  Display Name:    {user.DisplayName}");
            System.Console.WriteLine($"  Email:           {user.Email}");
            System.Console.WriteLine($"  Department:      {user.Department}");
            System.Console.WriteLine($"  Title:           {user.Title}");
            System.Console.WriteLine($"  Office:          {user.Office}");
            System.Console.WriteLine($"  Phone:           {user.PhoneNumber}");
            System.Console.WriteLine($"  Account Status:  {user.AccountStatus}");
            System.Console.WriteLine($"  Is Enabled:      {user.IsEnabled}");
            System.Console.WriteLine($"  Is Locked:       {user.IsLocked}");
            System.Console.WriteLine($"  Last Logon:      {user.LastLogonFormatted}");
            System.Console.WriteLine($"  Groups:          {user.MemberOf.Count}");
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"❌ {result.Message}");
            System.Console.ResetColor();
        }
    }

    static async Task EnableDisableUser(IADUserService userService)
    {
        System.Console.WriteLine("\n--- Enable/Disable User ---");
        System.Console.Write("Enter username: ");
        var username = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(username))
        {
            System.Console.WriteLine("Username cannot be empty.");
            return;
        }

        System.Console.Write("Enable (E) or Disable (D)? ");
        var action = System.Console.ReadLine()?.ToUpper();

        if (action != "E" && action != "D")
        {
            System.Console.WriteLine("Invalid choice.");
            return;
        }

        var result = action == "E"
            ? await userService.EnableUserAsync(username)
            : await userService.DisableUserAsync(username);

        if (result.IsSuccess)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"✓ {result.Message}");
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"❌ {result.Message}");
        }
        System.Console.ResetColor();
    }

    static async Task UnlockUser(IADUserService userService)
    {
        System.Console.WriteLine("\n--- Unlock User Account ---");
        System.Console.Write("Enter username: ");
        var username = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(username))
        {
            System.Console.WriteLine("Username cannot be empty.");
            return;
        }

        var result = await userService.UnlockUserAsync(username);

        if (result.IsSuccess)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"✓ {result.Message}");
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"❌ {result.Message}");
        }
        System.Console.ResetColor();
    }

    static async Task ChangePassword(IADUserService userService)
    {
        System.Console.WriteLine("\n--- Change User Password ---");
        System.Console.Write("Enter username: ");
        var username = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(username))
        {
            System.Console.WriteLine("Username cannot be empty.");
            return;
        }

        System.Console.Write("Enter new password: ");
        var password = ReadPassword();

        System.Console.Write("\nConfirm password: ");
        var confirmPassword = ReadPassword();

        var request = new Application.DTOs.PasswordChangeRequest
        {
            Username = username,
            NewPassword = password,
            ConfirmPassword = confirmPassword
        };

        var result = await userService.ChangePasswordAsync(request);

        if (result.IsSuccess)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"\n✓ {result.Message}");
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"\n❌ {result.Message}");
            if (result.Errors.Any())
            {
                foreach (var error in result.Errors)
                {
                    System.Console.WriteLine($"  • {error}");
                }
            }
        }
        System.Console.ResetColor();
    }

    static async Task ExportGroups(IExportService exportService)
    {
        System.Console.WriteLine("\n--- Export All Groups ---");
        System.Console.Write("Enter output file path (or press Enter for default): ");
        var filePath = System.Console.ReadLine();

        System.Console.WriteLine("Exporting groups...");
        var result = await exportService.ExportAllGroupsAsync(
            string.IsNullOrWhiteSpace(filePath) ? null : filePath);

        if (result.IsSuccess)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"✓ {result.Message}");
            System.Console.WriteLine($"  File: {result.Value}");
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"❌ {result.Message}");
        }
        System.Console.ResetColor();
    }

    static async Task ManageUserGroups(IADUserService userService)
    {
        System.Console.WriteLine("\n--- Manage User Groups ---");
        System.Console.Write("Enter username: ");
        var username = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(username))
        {
            System.Console.WriteLine("Username cannot be empty.");
            return;
        }

        System.Console.WriteLine("\n1. View User Groups");
        System.Console.WriteLine("2. Add User to Group");
        System.Console.WriteLine("3. Remove User from Group");
        System.Console.Write("\nEnter choice: ");
        var choice = System.Console.ReadLine();

        switch (choice)
        {
            case "1":
                var groupsResult = await userService.GetUserGroupsAsync(username);
                if (groupsResult.IsSuccess && groupsResult.Value != null)
                {
                    var groups = groupsResult.Value.ToList();
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine($"\n✓ User is member of {groups.Count} group(s):\n");
                    System.Console.ResetColor();
                    foreach (var group in groups)
                    {
                        System.Console.WriteLine($"  • {group}");
                    }
                }
                else
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine($"❌ {groupsResult.Message}");
                    System.Console.ResetColor();
                }
                break;

            case "2":
                System.Console.Write("Enter group name: ");
                var addGroup = System.Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(addGroup))
                {
                    var addResult = await userService.AddUserToGroupAsync(username, addGroup);
                    System.Console.ForegroundColor = addResult.IsSuccess ? ConsoleColor.Green : ConsoleColor.Red;
                    System.Console.WriteLine($"{(addResult.IsSuccess ? "✓" : "❌")} {addResult.Message}");
                    System.Console.ResetColor();
                }
                break;

            case "3":
                System.Console.Write("Enter group name: ");
                var removeGroup = System.Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(removeGroup))
                {
                    var removeResult = await userService.RemoveUserFromGroupAsync(username, removeGroup);
                    System.Console.ForegroundColor = removeResult.IsSuccess ? ConsoleColor.Green : ConsoleColor.Red;
                    System.Console.WriteLine($"{(removeResult.IsSuccess ? "✓" : "❌")} {removeResult.Message}");
                    System.Console.ResetColor();
                }
                break;
        }
    }

    static string ReadPassword()
    {
        var password = string.Empty;
        ConsoleKey key;

        do
        {
            var keyInfo = System.Console.ReadKey(intercept: true);
            key = keyInfo.Key;

            if (key == ConsoleKey.Backspace && password.Length > 0)
            {
                System.Console.Write("\b \b");
                password = password[0..^1];
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                System.Console.Write("*");
                password += keyInfo.KeyChar;
            }
        } while (key != ConsoleKey.Enter);

        return password;
    }
}

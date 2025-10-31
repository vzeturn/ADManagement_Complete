using ADManagement.Application.Configuration;
using ADManagement.Application.DTOs;
using ADManagement.Application.Interfaces;
using ADManagement.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace ADManagement.Console;

/// <summary>
/// Enhanced console application with connection diagnostics
/// </summary>
public class EnhancedConsoleApp
{
    private readonly IADUserService _userService;
    private readonly IADGroupService _groupService;
    private readonly IExportService _exportService;
    private readonly ADConnectionDiagnosticsService _diagnosticsService;
    private readonly ADConfiguration _config;
    private readonly ILogger<EnhancedConsoleApp> _logger;

    public EnhancedConsoleApp(
        IADUserService userService,
        IADGroupService groupService,
        IExportService exportService,
        ADConnectionDiagnosticsService diagnosticsService,
        ADConfiguration config,
        ILogger<EnhancedConsoleApp> logger)
    {
        _userService = userService;
        _groupService = groupService;
        _exportService = exportService;
        _diagnosticsService = diagnosticsService;
        _config = config;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        DisplayBanner();
        
        // Quick connection test on startup
        await QuickConnectionTestAsync();

        while (true)
        {
            DisplayMenu();
            var choice = System.Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        await TestConnectionAsync();
                        break;
                    case "2":
                        await RunFullDiagnosticsAsync();
                        break;
                    case "3":
                        await ViewConfigurationAsync();
                        break;
                    case "4":
                        await ExportAllUsersAsync();
                        break;
                    case "5":
                        await SearchUsersAsync();
                        break;
                    case "6":
                        await GetUserDetailsAsync();
                        break;
                    case "7":
                        await ViewLogsAsync();
                        break;
                    case "8":
                        await ManageUserGroupsAsync();
                        break;
                    case "0":
                        System.Console.WriteLine("\n👋 Goodbye!");
                        return;
                    default:
                        System.Console.WriteLine("\n❌ Invalid option. Please try again.");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing menu option {Option}", choice);
                System.Console.WriteLine($"\n❌ Error: {ex.Message}");
            }

            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }
    }

    private void DisplayBanner()
    {
        System.Console.Clear();
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════╗
║                                                               ║
║     Active Directory Management System - Enhanced Edition    ║
║                                                               ║
║     With Advanced Connection Diagnostics & Logging           ║
║                                                               ║
╚══════════════════════════════════════════════════════════════╝
        ");
        System.Console.ResetColor();
    }

    private void DisplayMenu()
    {
        System.Console.Clear();
        System.Console.ForegroundColor = ConsoleColor.Yellow;
        System.Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║                         MAIN MENU                             ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        System.Console.ResetColor();

        System.Console.WriteLine("\n🔧 CONNECTION & DIAGNOSTICS");
        System.Console.WriteLine("  1. Quick Connection Test");
        System.Console.WriteLine("  2. Full Diagnostics (Recommended for troubleshooting)");
        System.Console.WriteLine("  3. View Current Configuration");

        System.Console.WriteLine("\n📊 DATA OPERATIONS");
        System.Console.WriteLine("  4. Export All Users to Excel");
        System.Console.WriteLine("  5. Search Users");
        System.Console.WriteLine("  6. Get User Details");
        System.Console.WriteLine("  8. Manage User Groups");

        System.Console.WriteLine("\n📝 SYSTEM");
        System.Console.WriteLine("  7. View Recent Logs");
        System.Console.WriteLine("  0. Exit");

        System.Console.Write("\n👉 Select an option: ");
    }

    private async Task QuickConnectionTestAsync()
    {
        System.Console.WriteLine("\n🔄 Performing quick connection test...\n");
        
        var result = await _userService.TestConnectionAsync();

        if (result.IsSuccess)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("✅ Connection successful!");
            System.Console.ResetColor();
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"❌ Connection failed: {result.Message}");
            System.Console.WriteLine("\n💡 Tip: Select option '2' for detailed diagnostics");
            System.Console.ResetColor();
        }

        System.Console.WriteLine("\nPress any key to continue...");
        System.Console.ReadKey();
    }

    private async Task TestConnectionAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║                   QUICK CONNECTION TEST                       ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        System.Console.WriteLine("Testing connection to Active Directory...\n");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _userService.TestConnectionAsync();
        stopwatch.Stop();

        if (result.IsSuccess)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            System.Console.WriteLine("║  ✅ CONNECTION SUCCESSFUL                                     ║");
            System.Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            System.Console.ResetColor();
            
            System.Console.WriteLine($"\n📊 Connection Details:");
            System.Console.WriteLine($"   • Domain: {_config.Domain}");
            System.Console.WriteLine($"   • Server: {_config.LdapServer ?? "(auto-detected)"}");
            System.Console.WriteLine($"   • Port: {_config.Port}");
            System.Console.WriteLine($"   • Protocol: {(_config.UseSSL ? "LDAPS (Secure)" : "LDAP")}");
            System.Console.WriteLine($"   • Response Time: {stopwatch.ElapsedMilliseconds}ms");
            System.Console.WriteLine($"   • Status: {result.Message}");
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            System.Console.WriteLine("║  ❌ CONNECTION FAILED                                         ║");
            System.Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            System.Console.ResetColor();

            System.Console.WriteLine($"\n❌ Error: {result.Message}");
            
            if (result.Errors != null && result.Errors.Any())
            {
                System.Console.WriteLine("\n📋 Error Details:");
                foreach (var error in result.Errors)
                {
                    System.Console.WriteLine($"   • {error}");
                }
            }

            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine("\n💡 Recommendations:");
            System.Console.WriteLine("   1. Run 'Full Diagnostics' (Option 2) for detailed analysis");
            System.Console.WriteLine("   2. Check your configuration (Option 3)");
            System.Console.WriteLine("   3. View logs for more information (Option 7)");
            System.Console.ResetColor();
        }
    }

    private async Task RunFullDiagnosticsAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║              FULL CONNECTION DIAGNOSTICS                      ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        System.Console.WriteLine("This will perform a comprehensive analysis of your AD connection.");
        System.Console.WriteLine("The diagnostics will test:");
        System.Console.WriteLine("  • Configuration validation");
        System.Console.WriteLine("  • DNS resolution");
        System.Console.WriteLine("  • Network connectivity");
        System.Console.WriteLine("  • Port availability");
        System.Console.WriteLine("  • LDAP connection");
        System.Console.WriteLine("  • Authentication");
        System.Console.WriteLine("  • Query execution");
        System.Console.WriteLine("\n⏱️  This may take 30-60 seconds...\n");

        System.Console.Write("Press any key to start diagnostics...");
        System.Console.ReadKey();
        System.Console.WriteLine("\n");

        var result = await _diagnosticsService.RunFullDiagnosticsAsync();

        System.Console.WriteLine("\n\n" + new string('═', 64));
        
        if (result.IsFullyOperational)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("\n✅ DIAGNOSTICS PASSED - System is fully operational!");
            System.Console.ResetColor();
            System.Console.WriteLine("\nYou can now use all features of the application.");
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("\n❌ DIAGNOSTICS FAILED - Some tests did not pass");
            System.Console.ResetColor();
            System.Console.WriteLine("\nPlease review the detailed output above for specific issues.");
            System.Console.WriteLine("Each failed test includes troubleshooting tips to help resolve the issue.");
        }

        System.Console.WriteLine("\n💾 Detailed logs have been saved to the logs directory.");
    }

    private async Task ViewConfigurationAsync()
    {
        await Task.CompletedTask;
        
        System.Console.Clear();
        System.Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║                  CURRENT CONFIGURATION                        ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        System.Console.WriteLine("📋 Active Directory Configuration:");
        System.Console.WriteLine(new string('─', 64));
        
        DisplayConfigItem("Domain", _config.Domain);
        DisplayConfigItem("LDAP Server", string.IsNullOrWhiteSpace(_config.LdapServer) 
            ? "(auto-detect from domain)" 
            : _config.LdapServer);
        DisplayConfigItem("Port", _config.Port.ToString());
        DisplayConfigItem("Use SSL/TLS", _config.UseSSL ? "Yes (Port 636 recommended)" : "No (Port 389)");
        DisplayConfigItem("Username", string.IsNullOrWhiteSpace(_config.Username) 
            ? "(using current Windows user)" 
            : _config.Username);
        DisplayConfigItem("Password", string.IsNullOrWhiteSpace(_config.Password) 
            ? "(using current Windows user)" 
            : "***SET***");
        DisplayConfigItem("Page Size", _config.PageSize.ToString());
        DisplayConfigItem("Timeout", $"{_config.TimeoutSeconds} seconds");
        DisplayConfigItem("Default Search OU", string.IsNullOrWhiteSpace(_config.DefaultSearchOU) 
            ? "(root domain)" 
            : _config.DefaultSearchOU);

        System.Console.WriteLine("\n" + new string('─', 64));
        System.Console.WriteLine("\n⚙️  Configuration File Location:");
        System.Console.WriteLine($"   {Path.Combine(AppContext.BaseDirectory, "appsettings.json")}");

        System.Console.WriteLine("\n💡 Security Notes:");
        System.Console.WriteLine("   • Never commit credentials to source control");
        System.Console.WriteLine("   • Use User Secrets for development");
        System.Console.WriteLine("   • Use Azure Key Vault or environment variables for production");
        System.Console.WriteLine("   • Enable SSL/TLS (LDAPS) in production environments");
    }

    private void DisplayConfigItem(string name, string value)
    {
        System.Console.Write($"  {name,-20}: ");
        
        if (name.Contains("Password") && value != "(using current Windows user)")
        {
            System.Console.ForegroundColor = ConsoleColor.Yellow;
        }
        else if (value.Contains("auto-detect") || value.Contains("current Windows"))
        {
            System.Console.ForegroundColor = ConsoleColor.Cyan;
        }
        else if (name.Contains("SSL") && value.Contains("Yes"))
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
        }
        
        System.Console.WriteLine(value);
        System.Console.ResetColor();
    }

    private async Task ExportAllUsersAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║                    EXPORT ALL USERS                           ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        System.Console.Write("Enter output path (or press Enter for default): ");
        var path = System.Console.ReadLine();

        System.Console.WriteLine("\n📊 Exporting users...");
        
        var result = await _exportService.ExportAllUsersAsync(
            string.IsNullOrWhiteSpace(path) ? null : path);

        if (result.IsSuccess)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"\n✅ Export successful!");
            System.Console.ResetColor();
            System.Console.WriteLine($"📁 File location: {result.Value}");
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"\n❌ Export failed: {result.Message}");
            System.Console.ResetColor();
        }
    }

    private async Task SearchUsersAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║                       SEARCH USERS                            ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        System.Console.Write("Enter search term: ");
        var searchTerm = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            System.Console.WriteLine("❌ Search term cannot be empty.");
            return;
        }

        System.Console.WriteLine($"\n🔍 Searching for '{searchTerm}'...");

        var result = await _userService.SearchUsersAsync(searchTerm);

        if (result.IsSuccess && result.Value != null)
        {
            var users = result.Value.ToList();
            
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"\n✅ Found {users.Count} user(s)");
            System.Console.ResetColor();

            if (users.Any())
            {
                System.Console.WriteLine("\n" + new string('─', 64));
                foreach (var user in users.Take(20))
                {
                    DisplayUserSummary(user);
                }

                if (users.Count > 20)
                {
                    System.Console.WriteLine($"\n... and {users.Count - 20} more users");
                }
            }
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"\n❌ Search failed: {result.Message}");
            System.Console.ResetColor();
        }
    }

    private async Task GetUserDetailsAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║                      USER DETAILS                             ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        System.Console.Write("Enter username: ");
        var username = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(username))
        {
            System.Console.WriteLine("❌ Username cannot be empty.");
            return;
        }

        System.Console.WriteLine($"\n🔍 Getting details for '{username}'...");

        var result = await _userService.GetUserByUsernameAsync(username);

        if (result.IsSuccess && result.Value != null)
        {
            DisplayUserDetails(result.Value);
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"\n❌ Failed: {result.Message}");
            System.Console.ResetColor();
        }
    }

    private async Task ViewLogsAsync()
    {
        await Task.CompletedTask;
        
        System.Console.Clear();
        System.Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║                       RECENT LOGS                             ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
        
        if (!Directory.Exists(logsDir))
        {
            System.Console.WriteLine("❌ Logs directory not found.");
            return;
        }

        var logFiles = Directory.GetFiles(logsDir, "*.txt")
            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
            .Take(5)
            .ToList();

        if (!logFiles.Any())
        {
            System.Console.WriteLine("📋 No log files found.");
            return;
        }

        System.Console.WriteLine("📋 Recent log files:\n");
        
        for (int i = 0; i < logFiles.Count; i++)
        {
            var fileInfo = new FileInfo(logFiles[i]);
            System.Console.WriteLine($"  {i + 1}. {fileInfo.Name}");
            System.Console.WriteLine($"     Size: {fileInfo.Length / 1024} KB | Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            System.Console.WriteLine();
        }

        System.Console.Write("\nEnter number to view (or 0 to cancel): ");
        if (int.TryParse(System.Console.ReadLine(), out int choice) && choice > 0 && choice <= logFiles.Count)
        {
            var selectedFile = logFiles[choice - 1];
            System.Console.WriteLine($"\n📄 Viewing: {Path.GetFileName(selectedFile)}\n");
            System.Console.WriteLine(new string('─', 64));

            try
            {
                var lines = File.ReadLines(selectedFile).Reverse().Take(50).Reverse();
                foreach (var line in lines)
                {
                    if (line.Contains("Error") || line.Contains("❌"))
                        System.Console.ForegroundColor = ConsoleColor.Red;
                    else if (line.Contains("Warning") || line.Contains("⚠"))
                        System.Console.ForegroundColor = ConsoleColor.Yellow;
                    else if (line.Contains("Success") || line.Contains("✅"))
                        System.Console.ForegroundColor = ConsoleColor.Green;

                    System.Console.WriteLine(line);
                    System.Console.ResetColor();
                }
                
                System.Console.WriteLine(new string('─', 64));
                System.Console.WriteLine($"\n💡 Showing last 50 lines. Full log: {selectedFile}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error reading file: {ex.Message}");
            }
        }
    }

    private void DisplayUserSummary(ADUserDto user)
    {
        System.Console.WriteLine($"\n👤 {user.DisplayName ?? user.SamAccountName}");
        System.Console.WriteLine($"   Username : {user.SamAccountName}");
        System.Console.WriteLine($"   Email    : {user.Email ?? "N/A"}");
        System.Console.WriteLine($"   Status   : {(user.IsEnabled ? "✅ Enabled" : "❌ Disabled")}");
    }

    private void DisplayUserDetails(ADUserDto user)
    {
        System.Console.WriteLine("\n" + new string('═', 64));
        System.Console.WriteLine("USER DETAILS");
        System.Console.WriteLine(new string('═', 64));

        DisplayDetailItem("Display Name", user.DisplayName);
        DisplayDetailItem("Username (SAM)", user.SamAccountName);
        DisplayDetailItem("UPN", user.UserPrincipalName);
        DisplayDetailItem("Email", user.Email);
        DisplayDetailItem("First Name", user.FirstName);
        DisplayDetailItem("Last Name", user.LastName);
        DisplayDetailItem("Department", user.Department);
        DisplayDetailItem("Title", user.Title);
        DisplayDetailItem("Manager", user.Manager);
        DisplayDetailItem("Phone", user.TelephoneNumber);
        DisplayDetailItem("Mobile", user.Mobile);
        DisplayDetailItem("Office", user.Office);
        DisplayDetailItem("Distinguished Name", user.DistinguishedName);
        
        System.Console.WriteLine(new string('─', 64));
        DisplayDetailItem("Status", user.IsEnabled ? "✅ Enabled" : "❌ Disabled");
        DisplayDetailItem("Account Locked", user.IsLocked ? "🔒 Yes" : "No");
        DisplayDetailItem("Password Expired", user.PasswordExpired ? "⚠️  Yes" : "No");
        DisplayDetailItem("Must Change Password", user.MustChangePassword ? "⚠️  Yes" : "No");
        DisplayDetailItem("Password Never Expires", user.PasswordNeverExpires ? "Yes" : "No");
        
        if (user.LastLogon.HasValue)
            DisplayDetailItem("Last Logon", user.LastLogon.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        
        if (user.PasswordLastSet.HasValue)
            DisplayDetailItem("Password Last Set", user.PasswordLastSet.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        
        if (user.AccountExpirationDate.HasValue)
            DisplayDetailItem("Account Expires", user.AccountExpirationDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));

        System.Console.WriteLine(new string('═', 64));
    }

    private void DisplayDetailItem(string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
            
        System.Console.Write($"{label,-25}: ");
        
        if (value.Contains("❌") || value.Contains("🔒") || value.Contains("⚠️"))
            System.Console.ForegroundColor = ConsoleColor.Yellow;
        else if (value.Contains("✅"))
            System.Console.ForegroundColor = ConsoleColor.Green;
            
        System.Console.WriteLine(value);
        System.Console.ResetColor();
    }

    private async Task ManageUserGroupsAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║                    MANAGE USER GROUPS                         ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        System.Console.Write("Enter username: ");
        var username = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(username))
        {
            System.Console.WriteLine("❌ Username cannot be empty.");
            return;
        }

        bool managing = true;
        while (managing)
        {
            System.Console.Clear();
            System.Console.WriteLine($"📋 Group Management for User: {username}\n");
            System.Console.WriteLine("1. View User's Groups");
            System.Console.WriteLine("2. Add User to Group");
            System.Console.WriteLine("3. Remove User from Group");
            System.Console.WriteLine("0. Back to Main Menu");

            System.Console.Write("\n👉 Select an option: ");
            var choice = System.Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    await ViewUserGroupsAsync(username);
                    break;
                case "2":
                    await AddUserToGroupAsync(username);
                    break;
                case "3":
                    await RemoveUserFromGroupAsync(username);
                    break;
                case "0":
                    managing = false;
                    break;
                default:
                    System.Console.WriteLine("\n❌ Invalid option. Please try again.");
                    break;
            }

            if (managing)
            {
                System.Console.WriteLine("\nPress any key to continue...");
                System.Console.ReadKey();
            }
        }
    }

    private async Task ViewUserGroupsAsync(string username)
    {
        System.Console.WriteLine($"\n🔍 Fetching groups for user '{username}'...");
        
        var result = await _groupService.GetUserGroupsAsync(username);
        
        if (result.IsSuccess && result.Value != null)
        {
            var groups = result.Value.ToList();
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"\n✅ User is member of {groups.Count} group(s):\n");
            System.Console.ResetColor();
            
            if (groups.Any())
            {
                System.Console.WriteLine(new string('─', 64));
                foreach (var group in groups)
                {
                    System.Console.WriteLine($"  👥 {group}");
                }
                System.Console.WriteLine(new string('─', 64));
            }
            else
            {
                System.Console.WriteLine("User is not a member of any groups.");
            }
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"\n❌ Failed to fetch groups: {result.Message}");
            System.Console.ResetColor();
        }
    }

    private async Task AddUserToGroupAsync(string username)
    {
        System.Console.Write("\nEnter group name to add user to: ");
        var groupName = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(groupName))
        {
            System.Console.WriteLine("❌ Group name cannot be empty.");
            return;
        }

        System.Console.WriteLine($"\n➕ Adding user '{username}' to group '{groupName}'...");
        
        var result = await _groupService.AddUserToGroupAsync(username, groupName);

        if (result.IsSuccess)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"\n✅ {result.Message}");
            System.Console.ResetColor();
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"\n❌ Failed: {result.Message}");
            System.Console.ResetColor();
        }
    }

    private async Task RemoveUserFromGroupAsync(string username)
    {
        // First show current groups
        await ViewUserGroupsAsync(username);

        System.Console.Write("\nEnter group name to remove user from: ");
        var groupName = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(groupName))
        {
            System.Console.WriteLine("❌ Group name cannot be empty.");
            return;
        }

        System.Console.WriteLine($"\n➖ Removing user '{username}' from group '{groupName}'...");
        
        var result = await _groupService.RemoveUserFromGroupAsync(username, groupName);

        if (result.IsSuccess)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"\n✅ {result.Message}");
            System.Console.ResetColor();
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"\n❌ Failed: {result.Message}");
            System.Console.ResetColor();
        }
    }
}
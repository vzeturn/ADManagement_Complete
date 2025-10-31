using ADManagement.Application.Configuration;
using ADManagement.Domain.Common;
using Microsoft.Extensions.Logging;
using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ADManagement.Infrastructure.Services;

/// <summary>
/// Service for diagnosing Active Directory connection issues
/// </summary>
public class ADConnectionDiagnosticsService
{
    private readonly ADConfiguration _config;
    private readonly ILogger<ADConnectionDiagnosticsService> _logger;

    public ADConnectionDiagnosticsService(
        ADConfiguration config,
        ILogger<ADConnectionDiagnosticsService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Performs comprehensive connection diagnostics
    /// </summary>
    public async Task<DiagnosticsResult> RunFullDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("════════════════════════════════════════════════════════════════");
        _logger.LogInformation("Starting Active Directory Connection Diagnostics");
        _logger.LogInformation("════════════════════════════════════════════════════════════════");

        var result = new DiagnosticsResult();
        
        // Step 1: Display configuration
        DisplayConfiguration();
        result.ConfigurationValid = ValidateConfiguration();

        if (!result.ConfigurationValid)
        {
            _logger.LogError("❌ Configuration validation failed. Please check your settings.");
            return result;
        }

        // Step 2: DNS Resolution
        _logger.LogInformation("\n[STEP 1] Testing DNS Resolution...");
        result.DnsResolved = await TestDnsResolutionAsync(cancellationToken);

        // Step 3: Network connectivity
        _logger.LogInformation("\n[STEP 2] Testing Network Connectivity...");
        result.NetworkReachable = await TestNetworkConnectivityAsync(cancellationToken);

        // Step 4: Port availability
        _logger.LogInformation("\n[STEP 3] Testing Port Availability...");
        result.PortOpen = await TestPortConnectivityAsync(cancellationToken);

        // Step 5: LDAP connection
        _logger.LogInformation("\n[STEP 4] Testing LDAP Connection...");
        result.LdapConnected = await TestLdapConnectionAsync(cancellationToken);

        // Step 6: Authentication
        if (result.LdapConnected)
        {
            _logger.LogInformation("\n[STEP 5] Testing Authentication...");
            result.Authenticated = await TestAuthenticationAsync(cancellationToken);
        }

        // Step 7: Query test
        if (result.Authenticated)
        {
            _logger.LogInformation("\n[STEP 6] Testing AD Query...");
            result.QuerySuccessful = await TestQueryAsync(cancellationToken);
        }

        // Summary
        DisplaySummary(result);

        return result;
    }

    private void DisplayConfiguration()
    {
        _logger.LogInformation("\n┌─────────────────────────────────────────────────────────────┐");
        _logger.LogInformation("│ CURRENT CONFIGURATION                                        │");
        _logger.LogInformation("└─────────────────────────────────────────────────────────────┘");
        
        _logger.LogInformation("Domain          : {Domain}", _config.Domain ?? "(not set)");
        _logger.LogInformation("LDAP Server     : {Server}", 
            string.IsNullOrWhiteSpace(_config.LdapServer) ? "(auto-detect from domain)" : _config.LdapServer);
        _logger.LogInformation("Port            : {Port}", _config.Port);
        _logger.LogInformation("Use SSL         : {UseSSL}", _config.UseSSL ? "Yes (LDAPS)" : "No (LDAP)");
        _logger.LogInformation("Username        : {Username}", 
            string.IsNullOrWhiteSpace(_config.Username) ? "(using current user)" : _config.Username);
        _logger.LogInformation("Password        : {Password}", 
            string.IsNullOrWhiteSpace(_config.Password) ? "(using current user)" : "***SET***");
        _logger.LogInformation("Page Size       : {PageSize}", _config.PageSize);
        _logger.LogInformation("Timeout         : {Timeout} seconds", _config.TimeoutSeconds);
        _logger.LogInformation("Default Search OU: {OU}", 
            string.IsNullOrWhiteSpace(_config.DefaultSearchOU) ? "(root domain)" : _config.DefaultSearchOU);
    }

    private bool ValidateConfiguration()
    {
        _logger.LogInformation("\n┌─────────────────────────────────────────────────────────────┐");
        _logger.LogInformation("│ VALIDATING CONFIGURATION                                     │");
        _logger.LogInformation("└─────────────────────────────────────────────────────────────┘");

        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(_config.Domain))
        {
            issues.Add("Domain is not configured");
        }

        if (_config.Port <= 0 || _config.Port > 65535)
        {
            issues.Add($"Invalid port number: {_config.Port}");
        }

        if (_config.UseSSL && _config.Port == 389)
        {
            _logger.LogWarning("⚠️  SSL is enabled but using standard LDAP port 389. Consider using port 636 for LDAPS.");
        }

        if (!_config.UseSSL && _config.Port == 636)
        {
            _logger.LogWarning("⚠️  Using LDAPS port 636 but SSL is disabled. Consider enabling SSL.");
        }

        if (_config.TimeoutSeconds < 5)
        {
            _logger.LogWarning("⚠️  Timeout is very short ({Timeout}s). Consider increasing to at least 10 seconds.", 
                _config.TimeoutSeconds);
        }

        // Check if username is set without password or vice versa
        var hasUsername = !string.IsNullOrWhiteSpace(_config.Username);
        var hasPassword = !string.IsNullOrWhiteSpace(_config.Password);

        if (hasUsername != hasPassword)
        {
            issues.Add("Username and Password must both be set or both be empty");
        }

        if (issues.Any())
        {
            _logger.LogError("❌ Configuration has {Count} issue(s):", issues.Count);
            foreach (var issue in issues)
            {
                _logger.LogError("   • {Issue}", issue);
            }
            return false;
        }

        _logger.LogInformation("✅ Configuration is valid");
        return true;
    }

    private async Task<bool> TestDnsResolutionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var targetHost = string.IsNullOrWhiteSpace(_config.LdapServer) 
                ? _config.Domain 
                : _config.LdapServer;

            _logger.LogInformation("Resolving: {Host}", targetHost);

            var addresses = await Dns.GetHostAddressesAsync(targetHost, cancellationToken);

            if (addresses.Length == 0)
            {
                _logger.LogError("❌ No IP addresses found for {Host}", targetHost);
                _logger.LogError("   💡 Solution: Check DNS settings or use a different DNS server");
                return false;
            }

            _logger.LogInformation("✅ DNS Resolution successful");
            _logger.LogInformation("   Found {Count} IP address(es):", addresses.Length);
            foreach (var addr in addresses)
            {
                _logger.LogInformation("   • {IP} ({AddressFamily})", addr, addr.AddressFamily);
            }

            return true;
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "❌ DNS Resolution failed");
            _logger.LogError("   Error Code: {ErrorCode}", ex.ErrorCode);
            _logger.LogError("   💡 Solutions:");
            _logger.LogError("      1. Check if the domain name is correct");
            _logger.LogError("      2. Verify DNS server settings");
            _logger.LogError("      3. Try using the DC's IP address instead of domain name");
            _logger.LogError("      4. Check network connectivity");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error during DNS resolution");
            return false;
        }
    }

    private async Task<bool> TestNetworkConnectivityAsync(CancellationToken cancellationToken)
    {
        try
        {
            var targetHost = string.IsNullOrWhiteSpace(_config.LdapServer) 
                ? _config.Domain 
                : _config.LdapServer;

            _logger.LogInformation("Pinging: {Host}", targetHost);

            using var ping = new Ping();
            var reply = await ping.SendPingAsync(targetHost, _config.TimeoutSeconds * 1000);

            if (reply.Status == IPStatus.Success)
            {
                _logger.LogInformation("✅ Network reachable");
                _logger.LogInformation("   Response Time: {Time}ms", reply.RoundtripTime);
                _logger.LogInformation("   TTL: {TTL}", reply.Options?.Ttl);
                return true;
            }
            else
            {
                _logger.LogWarning("⚠️  Ping failed: {Status}", reply.Status);
                _logger.LogWarning("   This may be normal if ICMP is blocked by firewall");
                _logger.LogWarning("   Continuing with connection test...");
                return true; // Not critical, firewall might block ping
            }
        }
        catch (PingException ex)
        {
            _logger.LogWarning(ex, "⚠️  Ping test failed");
            _logger.LogWarning("   This may be normal if ICMP is blocked by firewall");
            return true; // Not critical
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Network connectivity test failed");
            return false;
        }
    }

    private async Task<bool> TestPortConnectivityAsync(CancellationToken cancellationToken)
    {
        try
        {
            var targetHost = string.IsNullOrWhiteSpace(_config.LdapServer) 
                ? _config.Domain 
                : _config.LdapServer;

            _logger.LogInformation("Testing port connection: {Host}:{Port}", targetHost, _config.Port);

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(targetHost, _config.Port);
            var timeoutTask = Task.Delay(_config.TimeoutSeconds * 1000, cancellationToken);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.LogError("❌ Connection timeout after {Timeout} seconds", _config.TimeoutSeconds);
                _logger.LogError("   💡 Solutions:");
                _logger.LogError("      1. Check firewall settings (port {Port} must be open)", _config.Port);
                _logger.LogError("      2. Verify the server is running and accessible");
                _logger.LogError("      3. Increase timeout in configuration");
                _logger.LogError("      4. Check if VPN is required");
                return false;
            }

            await connectTask; // This will throw if connection failed

            _logger.LogInformation("✅ Port {Port} is open and accepting connections", _config.Port);
            return true;
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "❌ Port connection failed");
            _logger.LogError("   Error Code: {ErrorCode}", ex.ErrorCode);
            _logger.LogError("   💡 Solutions:");
            _logger.LogError("      1. Firewall may be blocking port {Port}", _config.Port);
            _logger.LogError("      2. Server may not be running on this port");
            _logger.LogError("      3. Check if you're using the correct port:");
            _logger.LogError("         - LDAP  = 389");
            _logger.LogError("         - LDAPS = 636");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Port connectivity test failed");
            return false;
        }
    }

    private async Task<bool> TestLdapConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var targetHost = string.IsNullOrWhiteSpace(_config.LdapServer) 
                ? _config.Domain 
                : _config.LdapServer;

            _logger.LogInformation("Connecting to LDAP server: {Host}:{Port}", targetHost, _config.Port);

            using var connection = new LdapConnection(
                new LdapDirectoryIdentifier(targetHost, _config.Port));

            connection.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

            if (_config.UseSSL)
            {
                connection.SessionOptions.SecureSocketLayer = true;
                connection.SessionOptions.ProtocolVersion = 3;
                _logger.LogInformation("Using SSL/TLS encryption");
            }

            // Try to bind
            connection.Bind();

            _logger.LogInformation("✅ LDAP connection established successfully");
            _logger.LogInformation("   Protocol Version: 3");
            _logger.LogInformation("   Encryption: {Encryption}", _config.UseSSL ? "Yes (SSL/TLS)" : "No (Plain)");

            return true;
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "❌ LDAP connection failed");
            _logger.LogError("   Error Code: {ErrorCode}", ex.ErrorCode);
            _logger.LogError("   Server Error Message: {Message}", ex.ServerErrorMessage);
            _logger.LogError("   💡 Solutions:");
            
            if (ex.ErrorCode == 81) // Server down
            {
                _logger.LogError("      1. LDAP server is not available");
                _logger.LogError("      2. Check if domain controller is running");
                _logger.LogError("      3. Verify network connectivity");
            }
            else if (ex.ErrorCode == 49) // Invalid credentials
            {
                _logger.LogError("      1. Check username and password");
                _logger.LogError("      2. Verify account is not locked");
                _logger.LogError("      3. Use correct format: DOMAIN\\username or username@domain.com");
            }
            else if (ex.ErrorCode == 52) // Unavailable
            {
                _logger.LogError("      1. Server is temporarily unavailable");
                _logger.LogError("      2. Try again later");
            }
            else
            {
                _logger.LogError("      1. Check LDAP server logs for details");
                _logger.LogError("      2. Verify configuration settings");
                _logger.LogError("      3. Check LDAP server documentation for error code {ErrorCode}", ex.ErrorCode);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error during LDAP connection");
            return false;
        }
    }

    private async Task<bool> TestAuthenticationAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Testing authentication...");

                var ldapPath = $"LDAP://{(_config.UseSSL ? $"{_config.Domain}:{_config.Port}" : _config.Domain)}";
                _logger.LogInformation("LDAP Path: {Path}", ldapPath);

                using var entry = string.IsNullOrWhiteSpace(_config.Username)
                    ? new DirectoryEntry(ldapPath)
                    : new DirectoryEntry(ldapPath, _config.Username, _config.Password);

                // Try to access a property to trigger authentication
                var nativeObject = entry.NativeObject;

                _logger.LogInformation("✅ Authentication successful");
                _logger.LogInformation("   User: {User}", 
                    string.IsNullOrWhiteSpace(_config.Username) ? "Current Windows user" : _config.Username);
                _logger.LogInformation("   Domain: {Domain}", entry.Name);

                return true;
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                _logger.LogError(ex, "❌ Authentication failed");
                _logger.LogError("   HRESULT: 0x{HResult:X8}", ex.HResult);
                _logger.LogError("   💡 Solutions:");

                if (ex.HResult == unchecked((int)0x8007052E)) // Logon failure
                {
                    _logger.LogError("      1. Invalid username or password");
                    _logger.LogError("      2. Account may be locked or disabled");
                    _logger.LogError("      3. Password may have expired");
                    _logger.LogError("      4. Check username format: DOMAIN\\username or username@domain.com");
                }
                else if (ex.HResult == unchecked((int)0x80072020)) // Unknown error
                {
                    _logger.LogError("      1. Check if domain is accessible");
                    _logger.LogError("      2. Verify DNS settings");
                    _logger.LogError("      3. Try using domain controller's IP address");
                }
                else if (ex.HResult == unchecked((int)0x8007203A)) // Server unavailable
                {
                    _logger.LogError("      1. Domain controller is unavailable");
                    _logger.LogError("      2. Check network connectivity");
                    _logger.LogError("      3. Verify firewall settings");
                }
                else
                {
                    _logger.LogError("      1. Check Windows Event Viewer for details");
                    _logger.LogError("      2. Verify Active Directory service is running");
                    _logger.LogError("      3. Contact system administrator");
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error during authentication");
                return false;
            }
        }, cancellationToken);
    }

    private async Task<bool> TestQueryAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Executing test query (finding root DSE)...");

                var ldapPath = $"LDAP://{_config.Domain}/RootDSE";
                
                using var entry = string.IsNullOrWhiteSpace(_config.Username)
                    ? new DirectoryEntry(ldapPath)
                    : new DirectoryEntry(ldapPath, _config.Username, _config.Password);

                entry.RefreshCache();

                _logger.LogInformation("✅ Query successful");
                _logger.LogInformation("   Root DSE properties retrieved:");
                _logger.LogInformation("   • Default Naming Context: {Context}", 
                    entry.Properties["defaultNamingContext"]?.Value);
                _logger.LogInformation("   • DNS Host Name: {Host}", 
                    entry.Properties["dnsHostName"]?.Value);
                _logger.LogInformation("   • LDAP Service Name: {Service}", 
                    entry.Properties["ldapServiceName"]?.Value);

                // Test searching for users
                _logger.LogInformation("\nTesting user search (first 5 users)...");
                
                var searchPath = $"LDAP://{_config.Domain}";
                using var searchRoot = string.IsNullOrWhiteSpace(_config.Username)
                    ? new DirectoryEntry(searchPath)
                    : new DirectoryEntry(searchPath, _config.Username, _config.Password);

                using var searcher = new DirectorySearcher(searchRoot)
                {
                    Filter = "(&(objectClass=user)(objectCategory=person))",
                    PageSize = 5,
                    PropertiesToLoad = { "sAMAccountName", "displayName", "mail" }
                };

                var results = searcher.FindAll();
                
                _logger.LogInformation("   Found {Count} users (showing first 5):", results.Count);
                
                var count = 0;
                foreach (SearchResult result in results)
                {
                    if (count++ >= 5) break;
                    
                    var username = result.Properties["sAMAccountName"]?[0]?.ToString() ?? "N/A";
                    var displayName = result.Properties["displayName"]?[0]?.ToString() ?? "N/A";
                    _logger.LogInformation("   • {Username} - {DisplayName}", username, displayName);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Query test failed");
                _logger.LogError("   💡 Solutions:");
                _logger.LogError("      1. Check if user has permission to query Active Directory");
                _logger.LogError("      2. Verify search base DN is correct");
                _logger.LogError("      3. Check LDAP filter syntax");
                return false;
            }
        }, cancellationToken);
    }

    private void DisplaySummary(DiagnosticsResult result)
    {
        _logger.LogInformation("\n╔═════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║ DIAGNOSTICS SUMMARY                                          ║");
        _logger.LogInformation("╚═════════════════════════════════════════════════════════════╝");

        _logger.LogInformation("Configuration Valid    : {Status}", FormatStatus(result.ConfigurationValid));
        _logger.LogInformation("DNS Resolution         : {Status}", FormatStatus(result.DnsResolved));
        _logger.LogInformation("Network Reachable      : {Status}", FormatStatus(result.NetworkReachable));
        _logger.LogInformation("Port Open              : {Status}", FormatStatus(result.PortOpen));
        _logger.LogInformation("LDAP Connected         : {Status}", FormatStatus(result.LdapConnected));
        _logger.LogInformation("Authenticated          : {Status}", FormatStatus(result.Authenticated));
        _logger.LogInformation("Query Successful       : {Status}", FormatStatus(result.QuerySuccessful));

        _logger.LogInformation("\n┌─────────────────────────────────────────────────────────────┐");
        if (result.IsFullyOperational)
        {
            _logger.LogInformation("│ ✅ ALL TESTS PASSED - Active Directory is fully operational │");
        }
        else
        {
            _logger.LogInformation("│ ❌ SOME TESTS FAILED - Check errors above for solutions     │");
        }
        _logger.LogInformation("└─────────────────────────────────────────────────────────────┘");

        _logger.LogInformation("\n════════════════════════════════════════════════════════════════");
    }

    private string FormatStatus(bool status)
    {
        return status ? "✅ PASS" : "❌ FAIL";
    }
}

/// <summary>
/// Result of connection diagnostics
/// </summary>
public class DiagnosticsResult
{
    public bool ConfigurationValid { get; set; }
    public bool DnsResolved { get; set; }
    public bool NetworkReachable { get; set; }
    public bool PortOpen { get; set; }
    public bool LdapConnected { get; set; }
    public bool Authenticated { get; set; }
    public bool QuerySuccessful { get; set; }

    public bool IsFullyOperational =>
        ConfigurationValid &&
        DnsResolved &&
        NetworkReachable &&
        PortOpen &&
        LdapConnected &&
        Authenticated &&
        QuerySuccessful;
}
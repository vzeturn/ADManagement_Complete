using System.Collections.Concurrent;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.Protocols;
using ADManagement.Application.Configuration;
using System.Text;
using ADManagement.Domain.Common;
using ADManagement.Domain.Entities;
using ADManagement.Domain.Exceptions;
using ADManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace ADManagement.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Active Directory operations
/// </summary>

public class ADRepository : IADRepository
{
    private readonly ADConfiguration _config;
    private readonly ILogger<ADRepository> _logger;
    // Simple in-memory cache for search results to reduce repeated AD queries
    private static readonly ConcurrentDictionary<string, (DateTime Expire, List<ADUser> Users)> _searchCache = new();

    public ADRepository(ADConfiguration config, ILogger<ADRepository> logger)
    {
        _config = config;
        _logger = logger;
    }

    #region Connection

    public async Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: do a quick network-level check (DNS resolution + TCP connect) to fail fast
        try
        {
            var targetHost = string.IsNullOrWhiteSpace(_config.LdapServer) ? _config.Domain : _config.LdapServer;
            var port = _config.Port <= 0 ? 389 : _config.Port;

            _logger.LogInformation("Running quick network test to {Host}:{Port}", targetHost, port);

            var quickOk = await TryTcpConnectAsync(targetHost, port, TimeSpan.FromSeconds(Math.Max(3, _config.TimeoutSeconds)));
            if (!quickOk)
            {
                _logger.LogWarning("Quick network test failed for {Host}:{Port}", targetHost, port);
                return Result.Failure($"Network connection to {targetHost}:{port} failed. Check DNS/network/firewall.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Quick connection test threw an exception");
            return Result.Failure($"Quick connection test failed: {ex.Message}");
        }

        // If network quick test passed, attempt an AD bind using PrincipalContext (may still require credentials)
        return await Task.Run(() =>
        {
            try
            {
                using var context = GetPrincipalContext();
                // Minimal AD operation to verify bind and basic query
                using var searcher = new PrincipalSearcher(new UserPrincipal(context));
                _ = searcher.FindAll().Take(1).ToList();

                _logger.LogInformation("Successfully connected to Active Directory: {Domain}", _config.Domain);
                return Result.Success("Connection successful");
            }
            catch (PrincipalServerDownException ex)
            {
                _logger.LogError(ex, "Failed to connect to Active Directory - server down");
                return Result.Failure("Could not contact the LDAP server. Check server address, port and network connectivity.");
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "LDAP error during connection test");
                return Result.Failure($"LDAP error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Active Directory");
                return Result.Failure($"Connection failed: {ex.Message}");
            }
        }, cancellationToken);
    }

    // Try a TCP connect to host:port with timeout. Returns true if connected.
    private static async Task<bool> TryTcpConnectAsync(string host, int port, TimeSpan timeout)
    {
        try
        {
            // Resolve host to avoid blocking on connect to wrong name
            IPAddress[] addrs = null;
            try
            {
                addrs = await Dns.GetHostAddressesAsync(host);
            }
            catch
            {
                // if DNS resolution fails, attempt to use host as-is in connect
            }

            var addresses = (addrs != null && addrs.Length > 0) ? addrs : new IPAddress[] { IPAddress.None };

            foreach (var addr in addresses)
            {
                try
                {
                    using var client = new TcpClient();
                    var connectTask = (addr == IPAddress.None)
                        ? client.ConnectAsync(host, port)
                        : client.ConnectAsync(addr, port);

                    var cts = new CancellationTokenSource(timeout);
                    var completed = await Task.WhenAny(connectTask, Task.Delay(Timeout.Infinite, cts.Token));
                    if (completed == connectTask && client.Connected)
                    {
                        client.Close();
                        return true;
                    }
                }
                catch
                {
                    // try next address
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region User Operations

    public async Task<Result<IEnumerable<ADUser>>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Retrieving all users from Active Directory");

                using var context = GetPrincipalContext();
                using var searcher = new PrincipalSearcher(new UserPrincipal(context));

                var users = new List<ADUser>();
                var results = searcher.FindAll();

                foreach (UserPrincipal user in results)
                {
                    if (user != null)
                    {
                        try
                        {
                            users.Add(MapUserPrincipalToADUser(user));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to map user: {Username}", user.SamAccountName);
                        }
                        finally
                        {
                            user.Dispose();
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} users", users.Count);
                return Result.Success<IEnumerable<ADUser>>(users, $"Retrieved {users.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                return Result.Failure<IEnumerable<ADUser>>($"Failed to retrieve users: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<Result<IEnumerable<ADUser>>> GetUsersByOUAsync(string ouPath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Retrieving users from OU: {OUPath}", ouPath);

                using var entry = new DirectoryEntry($"{_config.LdapPath}/{ouPath}");
                using var searcher = new DirectorySearcher(entry)
                {
                    Filter = "(&(objectClass=user)(objectCategory=person))",
                    PageSize = _config.PageSize
                };

                LoadUserProperties(searcher);

                var users = new List<ADUser>();
                var results = searcher.FindAll();

                foreach (SearchResult result in results)
                {
                    try
                    {
                        users.Add(MapSearchResultToADUser(result));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to map user from search result");
                    }
                }

                _logger.LogInformation("Retrieved {Count} users from OU", users.Count);
                return Result.Success<IEnumerable<ADUser>>(users, $"Retrieved {users.Count} users from OU");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users from OU: {OUPath}", ouPath);
                return Result.Failure<IEnumerable<ADUser>>($"Failed to retrieve users from OU: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<Result<ADUser>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Retrieving user: {Username}", username);

                using var context = GetPrincipalContext();
                using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {Username}", username);
                    throw new UserNotFoundException(username);
                }

                var adUser = MapUserPrincipalToADUser(user);
                return Result.Success(adUser, "User retrieved successfully");
            }
            catch (UserNotFoundException ex)
            {
                return Result.Failure<ADUser>(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user: {Username}", username);
                return Result.Failure<ADUser>($"Failed to retrieve user: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<Result<ADUser>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Retrieving user by email: {Email}", email);

                using var context = GetPrincipalContext();
                using var searchUser = new UserPrincipal(context) { EmailAddress = email };
                using var searcher = new PrincipalSearcher(searchUser);

                var result = searcher.FindOne() as UserPrincipal;

                if (result == null)
                {
                    _logger.LogWarning("User not found with email: {Email}", email);
                    return Result.Failure<ADUser>($"User with email '{email}' not found");
                }

                var adUser = MapUserPrincipalToADUser(result);
                result.Dispose();

                return Result.Success(adUser, "User retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
                return Result.Failure<ADUser>($"Failed to retrieve user: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<Result<IEnumerable<ADUser>>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Searching users with term: {SearchTerm}", searchTerm);

                // Return cached result when available
                if (!string.IsNullOrWhiteSpace(searchTerm) && _config.SearchCacheSeconds > 0)
                {
                    var cacheKey = $"search:{_config.DefaultSearchOU}:{_config.PageSize}:{searchTerm}".ToLowerInvariant();
                    if (_searchCache.TryGetValue(cacheKey, out var entryCached))
                    {
                        if (DateTime.UtcNow < entryCached.Expire)
                        {
                            _logger.LogInformation("Returning cached search results for term '{SearchTerm}'", searchTerm);
                            return Result.Success<IEnumerable<ADUser>>(entryCached.Users, $"Found {entryCached.Users.Count} users (cached)");
                        }
                        else
                        {
                            _searchCache.TryRemove(cacheKey, out _);
                        }
                    }
                }

                // Create DirectoryEntry with credentials if provided to avoid anonymous/incorrect binds
                    AuthenticationTypes authType = AuthenticationTypes.None;
                    if (_config.UseSSL)
                    {
                        authType |= AuthenticationTypes.SecureSocketsLayer;
                    }
                    authType |= AuthenticationTypes.Secure;

                    // Build DirectoryEntry with credentials if provided
                    // Prepare username for DirectoryEntry: prefer DOMAIN\username if not already specified
                    string ldapUsername = _config.Username ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(ldapUsername) && !ldapUsername.Contains("\\") && !ldapUsername.Contains("@") && !string.IsNullOrWhiteSpace(_config.Domain))
                    {
                        ldapUsername = $"{_config.Domain}\\{ldapUsername}";
                    }

                    // If DefaultSearchOU provided, scope the entry to that OU to reduce search domain
                    string ldapPath = string.IsNullOrWhiteSpace(_config.DefaultSearchOU)
                        ? _config.LdapPath
                        : $"{_config.LdapPath}/{_config.DefaultSearchOU}";

                    var entry = !string.IsNullOrWhiteSpace(ldapUsername) && !string.IsNullOrWhiteSpace(_config.Password)
                        ? new DirectoryEntry(ldapPath, ldapUsername, _config.Password, authType)
                        : new DirectoryEntry(ldapPath);

                    var users = new List<ADUser>();

                    // Use explicit using blocks to avoid compiler issues with declarations in embedded statements
                    using (entry)
                    {
                        using (var searcher = new DirectorySearcher(entry))
                        {
                            // Escape search term to prevent LDAP filter injection and malformed queries
                            var escaped = EscapeLdapFilter(searchTerm);
                            searcher.Filter = $"(&(objectClass=user)(objectCategory=person)(|(sAMAccountName=*{escaped}*)(displayName=*{escaped}*)(mail=*{escaped}*)(department=*{escaped}*)))";

                            // Limit size/page to reasonable amount and set server time limit
                            searcher.PageSize = _config.PageSize;
                            searcher.ServerTimeLimit = TimeSpan.FromSeconds(_config.TimeoutSeconds);
                            searcher.SizeLimit = _config.PageSize; // cap results returned

                            // Only load minimal properties needed for list view to speed up queries
                            searcher.PropertiesToLoad.Clear();
                            searcher.PropertiesToLoad.AddRange(new[] { "sAMAccountName", "displayName", "mail", "department" });

                            var results = searcher.FindAll();

                            foreach (SearchResult result in results)
                            {
                                try
                                {
                                    users.Add(MapSearchResultToADUser(result));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to map user from search result");
                                }
                            }
                        }

                    // Cache results briefly to speed up repeated queries
                    if (!string.IsNullOrWhiteSpace(searchTerm) && _config.SearchCacheSeconds > 0)
                    {
                        var cacheKey = $"search:{_config.DefaultSearchOU}:{_config.PageSize}:{searchTerm}".ToLowerInvariant();
                        var expire = DateTime.UtcNow.AddSeconds(_config.SearchCacheSeconds);
                        _searchCache[cacheKey] = (expire, users);
                    }
                    }

                _logger.LogInformation("Found {Count} users matching search term", users.Count);
                return Result.Success<IEnumerable<ADUser>>(users, $"Found {users.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users: {SearchTerm}", searchTerm);
                return Result.Failure<IEnumerable<ADUser>>($"Search failed: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<Result> ChangePasswordAsync(string username, string newPassword, bool mustChangeAtNextLogon, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Changing password for user: {Username}", username);

                using var context = GetPrincipalContext();
                using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                if (user == null)
                {
                    throw new UserNotFoundException(username);
                }

                user.SetPassword(newPassword);
                if (mustChangeAtNextLogon)
                {
                    // Force change at next logon
                    user.ExpirePasswordNow();
                }
                user.Save();

                _logger.LogInformation("Password changed successfully for user: {Username}", username);
                return Result.Success("Password changed successfully");
            }
            catch (UserNotFoundException ex)
            {
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {Username}", username);
                return Result.Failure($"Failed to change password: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<Result<ADUser>> CreateUserAsync(
        string username,
        string firstName,
        string lastName,
        string password,
        string? organizationalUnit,
        string? displayName,
        string? email,
        string? department,
        string? title,
        string? company,
        string? office,
        string? phoneNumber,
        string? description,
        bool mustChangePasswordOnNextLogon,
        bool accountEnabled,
        bool passwordNeverExpires,
        IEnumerable<string>? initialGroups,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Creating new user: {Username}", username);

                using var context = GetPrincipalContext();
                
                // Check if user already exists
                using var existingUser = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
                if (existingUser != null)
                {
                    return Result.Failure<ADUser>($"User '{username}' already exists");
                }

                // Determine container/OU
                PrincipalContext? containerContext = null;
                if (!string.IsNullOrWhiteSpace(organizationalUnit))
                {
                    var containerPath = organizationalUnit.StartsWith(_config.LdapPath, StringComparison.OrdinalIgnoreCase)
                        ? organizationalUnit
                        : $"{_config.LdapPath}/{organizationalUnit}";
                    try
                    {
                        ContextOptions options = ContextOptions.Negotiate;
                        if (_config.UseSSL)
                        {
                            options |= ContextOptions.SecureSocketLayer;
                        }
                        containerContext = new PrincipalContext(
                            ContextType.Domain,
                            string.IsNullOrWhiteSpace(_config.LdapServer) ? _config.Domain : _config.LdapServer,
                            containerPath,
                            options,
                            !string.IsNullOrWhiteSpace(_config.Username) && !string.IsNullOrWhiteSpace(_config.Password) ? _config.Username : null,
                            !string.IsNullOrWhiteSpace(_config.Username) && !string.IsNullOrWhiteSpace(_config.Password) ? _config.Password : null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create context for OU {OU}, falling back to default", organizationalUnit);
                        containerContext = null;
                    }
                }

                using var targetContext = containerContext ?? context;
                
                // Create new user
                using var newUser = new UserPrincipal(targetContext, username, password, accountEnabled)
                {
                    GivenName = firstName,
                    Surname = lastName,
                    DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName : $"{firstName} {lastName}".Trim(),
                    Name = !string.IsNullOrWhiteSpace(displayName) ? displayName : $"{firstName} {lastName}".Trim()
                };
                
                if (!string.IsNullOrWhiteSpace(email))
                    newUser.EmailAddress = email;
                if (!string.IsNullOrWhiteSpace(description))
                    newUser.Description = description;
                
                // Set password options
                if (mustChangePasswordOnNextLogon)
                {
                    newUser.ExpirePasswordNow();
                }
                
                if (passwordNeverExpires)
                {
                    newUser.PasswordNeverExpires = true;
                }
                
                newUser.Save();

                // Set additional properties via DirectoryEntry for better control
                try
                {
                    using var entry = newUser.GetUnderlyingObject() as DirectoryEntry;
                    if (entry != null)
                    {
                        if (!string.IsNullOrWhiteSpace(department))
                            entry.Properties["department"].Value = department;
                        if (!string.IsNullOrWhiteSpace(title))
                            entry.Properties["title"].Value = title;
                        if (!string.IsNullOrWhiteSpace(company))
                            entry.Properties["company"].Value = company;
                        if (!string.IsNullOrWhiteSpace(office))
                            entry.Properties["physicalDeliveryOfficeName"].Value = office;
                        if (!string.IsNullOrWhiteSpace(phoneNumber))
                            entry.Properties["telephoneNumber"].Value = phoneNumber;
                        
                        entry.CommitChanges();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set additional properties for user {Username}", username);
                    // Continue - user was created, just some optional properties failed
                }

                // Add user to initial groups
                if (initialGroups != null && initialGroups.Any())
                {
                    foreach (var groupName in initialGroups)
                    {
                        try
                        {
                            using var group = GroupPrincipal.FindByIdentity(context, IdentityType.Name, groupName);
                            if (group != null && !group.Members.Contains(newUser))
                            {
                                group.Members.Add(newUser);
                                group.Save();
                                _logger.LogInformation("Added user {Username} to group {GroupName}", username, groupName);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to add user {Username} to group {GroupName}", username, groupName);
                            // Continue - don't fail user creation if group add fails
                        }
                    }
                }

                // Retrieve created user to map to ADUser
                using var createdUser = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
                if (createdUser == null)
                {
                    return Result.Failure<ADUser>("User was created but could not be retrieved");
                }

                var adUser = MapUserPrincipalToADUser(createdUser);
                _logger.LogInformation("User {Username} created successfully", username);
                return Result.Success(adUser, "User created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", username);
                return Result.Failure<ADUser>($"Failed to create user: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<Result> SetUserStatusAsync(string username, bool enabled, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var action = enabled ? "Enabling" : "Disabling";
                _logger.LogInformation("{Action} user: {Username}", action, username);

                using var context = GetPrincipalContext();
                using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                if (user == null)
                {
                    throw new UserNotFoundException(username);
                }

                user.Enabled = enabled;
                user.Save();

                var status = enabled ? "enabled" : "disabled";
                _logger.LogInformation("User {Username} {Status} successfully", username, status);
                return Result.Success($"User {status} successfully");
            }
            catch (UserNotFoundException ex)
            {
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting user status: {Username}", username);
                return Result.Failure($"Failed to set user status: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<Result> UnlockUserAsync(string username, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Unlocking user: {Username}", username);

                using var context = GetPrincipalContext();
                using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                if (user == null)
                {
                    throw new UserNotFoundException(username);
                }

                user.UnlockAccount();
                user.Save();

                _logger.LogInformation("User {Username} unlocked successfully", username);
                return Result.Success("User unlocked successfully");
            }
            catch (UserNotFoundException ex)
            {
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user: {Username}", username);
                return Result.Failure($"Failed to unlock user: {ex.Message}");
            }
        }, cancellationToken);
    }

    #endregion

    #region Group Operations

    public async Task<Result<IEnumerable<ADGroup>>> GetAllGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Retrieving all groups from Active Directory");

                using var context = GetPrincipalContext();
                using var searcher = new PrincipalSearcher(new GroupPrincipal(context));

                var groups = new List<ADGroup>();
                var results = searcher.FindAll();

                foreach (GroupPrincipal group in results)
                {
                    if (group != null)
                    {
                        try
                        {
                            groups.Add(MapGroupPrincipalToADGroup(group));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to map group: {GroupName}", group.Name);
                        }
                        finally
                        {
                            group.Dispose();
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} groups", groups.Count);
                return Result.Success<IEnumerable<ADGroup>>(groups, $"Retrieved {groups.Count} groups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving groups");
                return Result.Failure<IEnumerable<ADGroup>>($"Failed to retrieve groups: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<Result<IEnumerable<string>>> GetUserGroupsAsync(string username, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Retrieving groups for user: {Username}", username);

                // Use DirectoryEntry + DirectorySearcher to read memberOf attribute directly which is much faster
                AuthenticationTypes authType = AuthenticationTypes.None;
                if (_config.UseSSL)
                {
                    authType |= AuthenticationTypes.SecureSocketsLayer;
                }
                authType |= AuthenticationTypes.Secure;

                string ldapUsername = _config.Username ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(ldapUsername) && !ldapUsername.Contains("\\") && !ldapUsername.Contains("@") && !string.IsNullOrWhiteSpace(_config.Domain))
                {
                    ldapUsername = $"{_config.Domain}\\{ldapUsername}";
                }

                string ldapPath = string.IsNullOrWhiteSpace(_config.DefaultSearchOU)
                    ? _config.LdapPath
                    : $"{_config.LdapPath}/{_config.DefaultSearchOU}";

                using var entry = !string.IsNullOrWhiteSpace(ldapUsername) && !string.IsNullOrWhiteSpace(_config.Password)
                    ? new DirectoryEntry(ldapPath, ldapUsername, _config.Password, authType)
                    : new DirectoryEntry(ldapPath);

                using var searcher = new DirectorySearcher(entry)
                {
                    Filter = $"(&(objectClass=user)(sAMAccountName={EscapeLdapFilter(username)}))",
                    PageSize = 1,
                    SizeLimit = 1,
                    ServerTimeLimit = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                searcher.PropertiesToLoad.Clear();
                searcher.PropertiesToLoad.Add("memberOf");

                var result = searcher.FindOne();

                if (result == null)
                {
                    _logger.LogWarning("User not found: {Username}", username);
                    throw new UserNotFoundException(username);
                }

                var groups = new List<string>();

                if (result.Properties.Contains("memberOf"))
                {
                    foreach (var val in result.Properties["memberOf"] )
                    {
                        var dn = val?.ToString();
                        if (!string.IsNullOrWhiteSpace(dn))
                        {
                            groups.Add(GetNameFromDistinguishedName(dn));
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} groups for user {Username}", groups.Count, username);
                return Result.Success<IEnumerable<string>>(groups, $"Retrieved {groups.Count} groups");
            }
            catch (UserNotFoundException ex)
            {
                return Result.Failure<IEnumerable<string>>(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving groups for user: {Username}", username);
                return Result.Failure<IEnumerable<string>>($"Failed to retrieve groups: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<Result> AddUserToGroupAsync(string username, string groupName, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Adding user {Username} to group {GroupName}", username, groupName);

                using var context = GetPrincipalContext();
                using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
                using var group = GroupPrincipal.FindByIdentity(context, IdentityType.Name, groupName);

                if (user == null)
                {
                    throw new UserNotFoundException(username);
                }

                if (group == null)
                {
                    throw new GroupNotFoundException(groupName);
                }

                if (group.Members.Contains(user))
                {
                    return Result.Success("User is already a member of this group");
                }

                group.Members.Add(user);
                group.Save();

                _logger.LogInformation("User {Username} added to group {GroupName} successfully", username, groupName);
                return Result.Success("User added to group successfully");
            }
            catch (UserNotFoundException ex)
            {
                return Result.Failure(ex.Message);
            }
            catch (GroupNotFoundException ex)
            {
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {Username} to group {GroupName}", username, groupName);
                return Result.Failure($"Failed to add user to group: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<Result> RemoveUserFromGroupAsync(string username, string groupName, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Removing user {Username} from group {GroupName}", username, groupName);

                using var context = GetPrincipalContext();
                using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
                using var group = GroupPrincipal.FindByIdentity(context, IdentityType.Name, groupName);

                if (user == null)
                {
                    throw new UserNotFoundException(username);
                }

                if (group == null)
                {
                    throw new GroupNotFoundException(groupName);
                }

                if (!group.Members.Contains(user))
                {
                    return Result.Success("User is not a member of this group");
                }

                group.Members.Remove(user);
                group.Save();

                _logger.LogInformation("User {Username} removed from group {GroupName} successfully", username, groupName);
                return Result.Success("User removed from group successfully");
            }
            catch (UserNotFoundException ex)
            {
                return Result.Failure(ex.Message);
            }
            catch (GroupNotFoundException ex)
            {
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {Username} from group {GroupName}", username, groupName);
                return Result.Failure($"Failed to remove user from group: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<Result<IEnumerable<ADGroup>>> SearchGroupsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Searching groups with term: {SearchTerm}", searchTerm);

                using var entry = new DirectoryEntry(_config.LdapPath);
                using var searcher = new DirectorySearcher(entry)
                {
                    Filter = $"(&(objectClass=group)(|(cn=*{EscapeLdapFilter(searchTerm)}*)(displayName=*{EscapeLdapFilter(searchTerm)}*)(description=*{EscapeLdapFilter(searchTerm)}*)))",
                    PageSize = _config.PageSize,
                    ServerTimeLimit = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                searcher.PropertiesToLoad.Clear();
                searcher.PropertiesToLoad.AddRange(new[] { "cn", "displayName", "description", "distinguishedName", "groupType", "whenCreated", "whenChanged" });

                var results = searcher.FindAll();
                var groups = new List<ADGroup>();

                foreach (SearchResult res in results)
                {
                    try
                    {
                        var gp = new ADGroup
                        {
                            Name = GetProperty(res, "cn"),
                            DisplayName = GetProperty(res, "displayName"),
                            Description = GetProperty(res, "description"),
                            DistinguishedName = GetProperty(res, "distinguishedName")
                        };

                        groups.Add(gp);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to map group search result");
                    }
                }

                _logger.LogInformation("Found {Count} groups for search term", groups.Count);
                return Result.Success<IEnumerable<ADGroup>>(groups, $"Found {groups.Count} groups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching groups");
                return Result.Failure<IEnumerable<ADGroup>>($"Group search failed: {ex.Message}");
            }
        }, cancellationToken);
    }

    #endregion

    #region Organizational Unit Operations

    public async Task<Result<IEnumerable<OrganizationalUnit>>> GetAllOUsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Retrieving all organizational units");

                using var entry = new DirectoryEntry(_config.LdapPath);
                using var searcher = new DirectorySearcher(entry)
                {
                    Filter = "(objectClass=organizationalUnit)",
                    PageSize = _config.PageSize
                };

                searcher.PropertiesToLoad.AddRange(new[] { "name", "distinguishedName", "description", "whenCreated", "whenChanged" });

                var ous = new List<OrganizationalUnit>();
                var results = searcher.FindAll();

                foreach (SearchResult result in results)
                {
                    try
                    {
                        ous.Add(new OrganizationalUnit
                        {
                            Name = GetProperty(result, "name"),
                            DistinguishedName = GetProperty(result, "distinguishedName"),
                            Description = GetProperty(result, "description"),
                            Path = GetProperty(result, "distinguishedName"),
                            WhenCreated = GetDateTimeProperty(result, "whenCreated"),
                            WhenChanged = GetDateTimeProperty(result, "whenChanged")
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to map OU from search result");
                    }
                }

                _logger.LogInformation("Retrieved {Count} organizational units", ous.Count);
                return Result.Success<IEnumerable<OrganizationalUnit>>(ous, $"Retrieved {ous.Count} organizational units");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organizational units");
                return Result.Failure<IEnumerable<OrganizationalUnit>>($"Failed to retrieve organizational units: {ex.Message}");
            }
        }, cancellationToken);
    }

    #endregion

    #region Helper Methods

    private PrincipalContext GetPrincipalContext()
    {
        try
        {
            // Prefer explicit LDAP server if configured to target a specific domain controller for consistency
            var target = string.IsNullOrWhiteSpace(_config.LdapServer) ? _config.Domain : _config.LdapServer;
            var container = string.IsNullOrWhiteSpace(_config.DefaultSearchOU) ? null : _config.DefaultSearchOU;

            ContextOptions options = ContextOptions.Negotiate;
            if (_config.UseSSL)
            {
                options |= ContextOptions.SecureSocketLayer;
            }

            if (!string.IsNullOrWhiteSpace(_config.Username) && !string.IsNullOrWhiteSpace(_config.Password))
            {
                _logger.LogDebug("Creating PrincipalContext to {Target} with explicit credentials", target);
                return new PrincipalContext(ContextType.Domain, target, container, options, _config.Username, _config.Password);
            }

            _logger.LogDebug("Creating PrincipalContext to {Target} using current credentials", target);
            return new PrincipalContext(ContextType.Domain, target, container, options);
        }
        catch (PrincipalServerDownException ex)
        {
            _logger.LogError(ex, "PrincipalContext failed - server down for target {DomainOrServer}", _config.LdapServer ?? _config.Domain);
            // Provide a helpful message to caller
            throw new PrincipalServerDownException("LDAP server is unavailable. Verify the server address, port and network connectivity.", ex);
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "LDAP exception creating PrincipalContext");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating PrincipalContext");
            throw;
        }
    }

    private void LoadUserProperties(DirectorySearcher searcher)
    {
        var properties = new[]
        {
            "sAMAccountName", "displayName", "mail", "givenName", "sn",
            "department", "title", "company", "physicalDeliveryOfficeName", "manager",
            "telephoneNumber", "mobile", "facsimileTelephoneNumber",
            "streetAddress", "l", "st", "postalCode", "co",
            "distinguishedName", "userAccountControl", "lockoutTime",
            "pwdLastSet", "accountExpires", "whenCreated", "whenChanged",
            "description", "memberOf"
        };

        searcher.PropertiesToLoad.AddRange(properties);
    }

    private ADUser MapUserPrincipalToADUser(UserPrincipal user)
    {
        using var entry = user.GetUnderlyingObject() as DirectoryEntry;

        var adUser = new ADUser
        {
            Username = user.SamAccountName ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            Email = user.EmailAddress ?? string.Empty,
            FirstName = user.GivenName ?? string.Empty,
            LastName = user.Surname ?? string.Empty,
            DistinguishedName = user.DistinguishedName ?? string.Empty,
            IsEnabled = user.Enabled ?? false,
            IsLockedOut = user.IsAccountLockedOut(),
            LastLogon = user.LastLogon,
            LastPasswordSet = user.LastPasswordSet,
            AccountExpires = user.AccountExpirationDate,
            Description = user.Description ?? string.Empty
        };

        // Additional properties from DirectoryEntry
        if (entry != null)
        {
            adUser.Department = entry.Properties["department"].Value?.ToString() ?? string.Empty;
            adUser.Title = entry.Properties["title"].Value?.ToString() ?? string.Empty;
            adUser.Company = entry.Properties["company"].Value?.ToString() ?? string.Empty;
            adUser.Office = entry.Properties["physicalDeliveryOfficeName"].Value?.ToString() ?? string.Empty;
            adUser.PhoneNumber = entry.Properties["telephoneNumber"].Value?.ToString() ?? string.Empty;
            adUser.MobileNumber = entry.Properties["mobile"].Value?.ToString() ?? string.Empty;
        }

        // Get groups from DirectoryEntry memberOf which is much faster than enumerating GroupPrincipal objects
        try
        {
            var groups = new List<string>();

            if (entry != null && entry.Properties.Contains("memberOf"))
            {
                foreach (var val in entry.Properties["memberOf"] )
                {
                    var dn = val?.ToString();
                    if (!string.IsNullOrWhiteSpace(dn))
                    {
                        groups.Add(GetNameFromDistinguishedName(dn));
                    }
                }
            }

            adUser.MemberOf = groups;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve groups for user: {Username}", user.SamAccountName);
            adUser.MemberOf = new List<string>();
        }

        return adUser;
    }

    private string GetNameFromDistinguishedName(string dn)
    {
        // Extract the CN or first RDN value from a distinguished name: CN=Group Name,OU=... -> Group Name
        if (string.IsNullOrWhiteSpace(dn)) return string.Empty;

        try
        {
            var parts = dn.Split(',');
            if (parts.Length == 0) return dn;

            var rdn = parts[0];
            var equalsIndex = rdn.IndexOf('=');
            if (equalsIndex <= 0 || equalsIndex >= rdn.Length - 1) return rdn;

            return rdn[(equalsIndex + 1)..].Trim();
        }
        catch
        {
            return dn;
        }
    }

    private ADUser MapSearchResultToADUser(SearchResult result)
    {
        var uac = GetInt32Property(result, "userAccountControl");
        var isEnabled = (uac & 0x0002) == 0; // ADS_UF_ACCOUNTDISABLE

        return new ADUser
        {
            Username = GetProperty(result, "sAMAccountName"),
            DisplayName = GetProperty(result, "displayName"),
            Email = GetProperty(result, "mail"),
            FirstName = GetProperty(result, "givenName"),
            LastName = GetProperty(result, "sn"),
            Department = GetProperty(result, "department"),
            Title = GetProperty(result, "title"),
            Company = GetProperty(result, "company"),
            Office = GetProperty(result, "physicalDeliveryOfficeName"),
            Manager = GetProperty(result, "manager"),
            PhoneNumber = GetProperty(result, "telephoneNumber"),
            MobileNumber = GetProperty(result, "mobile"),
            FaxNumber = GetProperty(result, "facsimileTelephoneNumber"),
            StreetAddress = GetProperty(result, "streetAddress"),
            City = GetProperty(result, "l"),
            State = GetProperty(result, "st"),
            PostalCode = GetProperty(result, "postalCode"),
            Country = GetProperty(result, "co"),
            DistinguishedName = GetProperty(result, "distinguishedName"),
            IsEnabled = isEnabled,
            IsLockedOut = GetInt64Property(result, "lockoutTime") > 0,
            LastPasswordSet = ConvertFileTimeToDateTime(GetInt64Property(result, "pwdLastSet")),
            AccountExpires = ConvertFileTimeToDateTime(GetInt64Property(result, "accountExpires")),
            WhenCreated = GetDateTimeProperty(result, "whenCreated"),
            WhenChanged = GetDateTimeProperty(result, "whenChanged"),
            Description = GetProperty(result, "description"),
            MemberOf = GetMultiValueProperty(result, "memberOf")
        };
    }

    private string GetProperty(SearchResult result, string propertyName)
    {
        return result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0
            ? result.Properties[propertyName][0]?.ToString() ?? string.Empty
            : string.Empty;
    }

    private int GetInt32Property(SearchResult result, string propertyName)
    {
        if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
        {
            if (int.TryParse(result.Properties[propertyName][0]?.ToString(), out var value))
            {
                return value;
            }
        }
        return 0;
    }

    private long GetInt64Property(SearchResult result, string propertyName)
    {
        if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
        {
            var value = result.Properties[propertyName][0];
            if (value is long longValue)
            {
                return longValue;
            }
            if (long.TryParse(value?.ToString(), out var parsedValue))
            {
                return parsedValue;
            }
        }
        return 0;
    }

    private DateTime? GetDateTimeProperty(SearchResult result, string propertyName)
    {
        if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
        {
            var value = result.Properties[propertyName][0];
            if (value is DateTime dateTime)
            {
                return dateTime;
            }
        }
        return null;
    }

    private List<string> GetMultiValueProperty(SearchResult result, string propertyName)
    {
        var values = new List<string>();
        if (result.Properties.Contains(propertyName))
        {
            foreach (var value in result.Properties[propertyName])
            {
                if (value != null)
                {
                    values.Add(value.ToString() ?? string.Empty);
                }
            }
        }
        return values;
    }

    private DateTime? ConvertFileTimeToDateTime(long fileTime)
    {
        if (fileTime == 0 || fileTime == long.MaxValue)
        {
            return null;
        }

        try
        {
            return DateTime.FromFileTime(fileTime);
        }
        catch
        {
            return null;
        }
    }

    // Escape special characters in LDAP search filters to avoid malformed filters or injection
    private string EscapeLdapFilter(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var sb = new StringBuilder();
        foreach (var c in input)
        {
            switch (c)
            {
                case '\\': sb.Append("\\5c"); break;
                case '*': sb.Append("\\2a"); break;
                case '(' : sb.Append("\\28"); break;
                case ')' : sb.Append("\\29"); break;
                case '\0': sb.Append("\\00"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    private ADGroup MapGroupPrincipalToADGroup(GroupPrincipal group)
    {
        using var entry = group.GetUnderlyingObject() as DirectoryEntry;

        var adGroup = new ADGroup
        {
            Name = group.Name ?? string.Empty,
            DisplayName = group.DisplayName ?? string.Empty,
            Description = group.Description ?? string.Empty,
            DistinguishedName = group.DistinguishedName ?? string.Empty
        };

        // Get group properties from DirectoryEntry
        if (entry != null)
        {
            adGroup.GroupScope = entry.Properties["groupType"].Value?.ToString() ?? string.Empty;
            adGroup.GroupType = entry.Properties["groupType"].Value?.ToString() ?? string.Empty;

            var whenCreated = entry.Properties["whenCreated"].Value;
            if (whenCreated is DateTime created)
            {
                adGroup.WhenCreated = created;
            }

            var whenChanged = entry.Properties["whenChanged"].Value;
            if (whenChanged is DateTime changed)
            {
                adGroup.WhenChanged = changed;
            }
        }

        // Get members
        try
        {
            var members = group.Members;
            adGroup.Members = members.Select(m => m.Name ?? string.Empty).ToList();

            foreach (var member in members)
            {
                member?.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve members for group: {GroupName}", group.Name);
            adGroup.Members = new List<string>();
        }

        return adGroup;
    }

    #endregion
}
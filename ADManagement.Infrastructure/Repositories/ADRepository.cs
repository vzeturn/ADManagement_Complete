using ADManagement.Application.Configuration;
using ADManagement.Application.DTOs;
using ADManagement.Domain.Common;
using ADManagement.Domain.Entities;
using ADManagement.Domain.Exceptions;
using ADManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Net.Sockets;
using System.Text;

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

/// <summary>
/// Gets all groups from Active Directory
/// </summary>
public async Task<Result<IEnumerable<ADGroup>>> GetAllGroupsAsync(CancellationToken cancellationToken = default)
{
    return await Task.Run(() =>
    {
        try
        {
            _logger.LogInformation("Getting all groups from Active Directory");

            var _entry = GetDirectoryEntry();
            using var searcher = new DirectorySearcher(_entry)
            {
                Filter = "(objectCategory=group)",
                PageSize = _config.PageSize
            };

            // Properties to load
            searcher.PropertiesToLoad.AddRange(new[]
            {
                "cn", "name", "distinguishedName", "description",
                "groupType", "mail", "managedBy", "member", "memberOf",
                "whenCreated", "whenChanged", "sAMAccountName"
            });

            var results = searcher.FindAll();
            var groups = new List<ADGroup>();

            foreach (SearchResult result in results)
            {
                var group = MapSearchResultToADGroup(result);
                groups.Add(group);
            }

            _logger.LogInformation("Retrieved {Count} groups", groups.Count);
            return Result.Success<IEnumerable<ADGroup>>(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all groups");
            return Result.Failure<IEnumerable<ADGroup>>($"Error getting groups: {ex.Message}");
        }
    }, cancellationToken);
}

/// <summary>
/// Searches for groups matching the search term (fuzzy search)
/// </summary>
public async Task<Result<IEnumerable<ADGroup>>> SearchGroupsAsync(string searchTerm, CancellationToken cancellationToken = default)
{
    return await Task.Run(() =>
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return GetAllGroupsAsync(cancellationToken).Result;
            }

            _logger.LogInformation("Searching groups with term: {SearchTerm}", searchTerm);

            // Fuzzy search: search in cn, name, description, displayName
            var filter = $"(&(objectCategory=group)(|(cn=*{searchTerm}*)(name=*{searchTerm}*)(description=*{searchTerm}*)(displayName=*{searchTerm}*)))";

            var _entry = GetDirectoryEntry();
            using var searcher = new DirectorySearcher(_entry)
            {
                Filter = filter,
                PageSize = _config.PageSize
            };

            searcher.PropertiesToLoad.AddRange(new[]
            {
                "cn", "name", "distinguishedName", "description",
                "groupType", "mail", "managedBy", "member", "memberOf",
                "whenCreated", "whenChanged", "sAMAccountName"
            });

            var results = searcher.FindAll();
            var groups = new List<ADGroup>();

            foreach (SearchResult result in results)
            {
                var group = MapSearchResultToADGroup(result);
                groups.Add(group);
            }

            _logger.LogInformation("Found {Count} groups matching '{SearchTerm}'", groups.Count, searchTerm);
            return Result.Success<IEnumerable<ADGroup>>(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching groups with term: {SearchTerm}", searchTerm);
            return Result.Failure<IEnumerable<ADGroup>>($"Error searching groups: {ex.Message}");
        }
    }, cancellationToken);
}

/// <summary>
/// Gets a specific group by name
/// </summary>
public async Task<Result<ADGroup>> GetGroupByNameAsync(string groupName, CancellationToken cancellationToken = default)
{
    return await Task.Run(() =>
    {
        try
        {
            _logger.LogInformation("Getting group by name: {GroupName}", groupName);

            using var context = GetPrincipalContext();
            using var group = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, groupName);

            if (group == null)
            {
                _logger.LogWarning("Group not found: {GroupName}", groupName);
                return Result.Failure<ADGroup>($"Group '{groupName}' not found");
            }

            var adGroup = MapGroupPrincipalToADGroup(group);

            _logger.LogInformation("Successfully retrieved group: {GroupName}", groupName);
            return Result.Success<ADGroup>(adGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group by name: {GroupName}", groupName);
            return Result.Failure<ADGroup>($"Error getting group: {ex.Message}");
        }
    }, cancellationToken);
}

/// <summary>
/// Gets groups that a user is a member of
/// </summary>
public async Task<Result<IEnumerable<ADGroup>>> GetUserGroupsAsync(string username, CancellationToken cancellationToken = default)
{
    return await Task.Run(() =>
    {
        try
        {
            _logger.LogInformation("Getting groups for user: {Username}", username);

            using var context = GetPrincipalContext();
            using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

            if (user == null)
            {
                _logger.LogWarning("User not found: {Username}", username);
                return Result.Failure<IEnumerable<ADGroup>>($"User '{username}' not found");
            }

            var groups = user.GetGroups().ToList();
            var adGroups = new List<ADGroup>();

            foreach (Principal group in groups)
            {
                if (group is GroupPrincipal groupPrincipal)
                {
                    adGroups.Add(MapGroupPrincipalToADGroup(groupPrincipal));
                }
            }

            _logger.LogInformation("User {Username} is member of {Count} groups", username, adGroups.Count);
            return Result.Success<IEnumerable<ADGroup>>(adGroups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups for user: {Username}", username);
            return Result.Failure<IEnumerable<ADGroup>>($"Error getting user groups: {ex.Message}");
        }
    }, cancellationToken);
}

/// <summary>
/// Adds a user to a group
/// </summary>
public async Task<Result> AddUserToGroupAsync(string username, string groupName, CancellationToken cancellationToken = default)
{
    return await Task.Run(() =>
    {
        try
        {
            _logger.LogInformation("Adding user {Username} to group {GroupName}", username, groupName);

            using var context = GetPrincipalContext();

            // Find user
            using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
            if (user == null)
            {
                _logger.LogWarning("User not found: {Username}", username);
                return Result.Failure($"User '{username}' not found");
            }

            // Find group
            using var group = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, groupName);
            if (group == null)
            {
                _logger.LogWarning("Group not found: {GroupName}", groupName);
                return Result.Failure($"Group '{groupName}' not found");
            }

            // Check if user is already a member
            if (user.IsMemberOf(group))
            {
                _logger.LogInformation("User {Username} is already a member of group {GroupName}", username, groupName);
                return Result.Success($"User '{username}' is already a member of group '{groupName}'");
            }

            // Add user to group
            group.Members.Add(user);
            group.Save();

            _logger.LogInformation("Successfully added user {Username} to group {GroupName}", username, groupName);
            return Result.Success($"User '{username}' added to group '{groupName}' successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {Username} to group {GroupName}", username, groupName);
            return Result.Failure($"Error adding user to group: {ex.Message}");
        }
    }, cancellationToken);
}

/// <summary>
/// Removes a user from a group
/// </summary>
public async Task<Result> RemoveUserFromGroupAsync(string username, string groupName, CancellationToken cancellationToken = default)
{
    return await Task.Run(() =>
    {
        try
        {
            _logger.LogInformation("Removing user {Username} from group {GroupName}", username, groupName);

            using var context = GetPrincipalContext();

            // Find user
            using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
            if (user == null)
            {
                _logger.LogWarning("User not found: {Username}", username);
                return Result.Failure($"User '{username}' not found");
            }

            // Find group
            using var group = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, groupName);
            if (group == null)
            {
                _logger.LogWarning("Group not found: {GroupName}", groupName);
                return Result.Failure($"Group '{groupName}' not found");
            }

            // Check if user is a member
            if (!user.IsMemberOf(group))
            {
                _logger.LogInformation("User {Username} is not a member of group {GroupName}", username, groupName);
                return Result.Success($"User '{username}' is not a member of group '{groupName}'");
            }

            // Remove user from group
            group.Members.Remove(user);
            group.Save();

            _logger.LogInformation("Successfully removed user {Username} from group {GroupName}", username, groupName);
            return Result.Success($"User '{username}' removed from group '{groupName}' successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {Username} from group {GroupName}", username, groupName);
            return Result.Failure($"Error removing user from group: {ex.Message}");
        }
    }, cancellationToken);
}

/// <summary>
/// Gets all members of a group
/// </summary>
public async Task<Result<IEnumerable<ADUser>>> GetGroupMembersAsync(string groupName, CancellationToken cancellationToken = default)
{
    return await Task.Run(() =>
    {
        try
        {
            _logger.LogInformation("Getting members of group: {GroupName}", groupName);

            using var context = GetPrincipalContext();
            using var group = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, groupName);

            if (group == null)
            {
                _logger.LogWarning("Group not found: {GroupName}", groupName);
                return Result.Failure<IEnumerable<ADUser>>($"Group '{groupName}' not found");
            }

            var members = group.GetMembers().ToList();
            var adUsers = new List<ADUser>();

            foreach (Principal member in members)
            {
                if (member is UserPrincipal userPrincipal)
                {
                    adUsers.Add(MapUserPrincipalToADUser(userPrincipal));
                }
            }

            _logger.LogInformation("Group {GroupName} has {Count} members", groupName, adUsers.Count);
            return Result.Success<IEnumerable<ADUser>>(adUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting members of group: {GroupName}", groupName);
            return Result.Failure<IEnumerable<ADUser>>($"Error getting group members: {ex.Message}");
        }
    }, cancellationToken);
}

/// <summary>
/// Creates a new group in Active Directory
/// </summary>
public async Task<Result<ADGroup>> CreateGroupAsync(
    string groupName,
    string description,
    string groupScope,
    string groupType,
    string organizationalUnit = "",
    CancellationToken cancellationToken = default)
{
    return await Task.Run(() =>
    {
        try
        {
            _logger.LogInformation("Creating group: {GroupName}", groupName);

            using var context = string.IsNullOrWhiteSpace(organizationalUnit)
                ? GetPrincipalContext()
                : new PrincipalContext(ContextType.Domain, _config.Domain, organizationalUnit, 
                    _config.Username, _config.Password);

            using var group = new GroupPrincipal(context)
            {
                Name = groupName,
                SamAccountName = groupName,
                Description = description
            };

            // Set group scope
            if (!string.IsNullOrWhiteSpace(groupScope))
            {
                group.GroupScope = groupScope.ToLower() switch
                {
                    "global" => GroupScope.Global,
                    "universal" => GroupScope.Universal,
                    "local" => GroupScope.Local,
                    _ => GroupScope.Global
                };
            }

            // Note: GroupPrincipal doesn't have IsSecurityGroup property
            // Group type is determined by GroupPrincipal vs DistributionGroup

            group.Save();

            var adGroup = MapGroupPrincipalToADGroup(group);

            _logger.LogInformation("Successfully created group: {GroupName}", groupName);
            return Result<ADGroup>.Success(adGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group: {GroupName}", groupName);
            return Result.Failure<ADGroup>($"Error creating group: {ex.Message}");
        }
    }, cancellationToken);
}

/// <summary>
/// Deletes a group from Active Directory
/// </summary>
public async Task<Result> DeleteGroupAsync(string groupName, CancellationToken cancellationToken = default)
{
    return await Task.Run(() =>
    {
        try
        {
            _logger.LogInformation("Deleting group: {GroupName}", groupName);

            using var context = GetPrincipalContext();
            using var group = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, groupName);

            if (group == null)
            {
                _logger.LogWarning("Group not found: {GroupName}", groupName);
                return Result.Failure($"Group '{groupName}' not found");
            }

            group.Delete();

            _logger.LogInformation("Successfully deleted group: {GroupName}", groupName);
            return Result.Success($"Group '{groupName}' deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group: {GroupName}", groupName);
            return Result.Failure($"Error deleting group: {ex.Message}");
        }
    }, cancellationToken);
}

/// <summary>
/// Updates group properties
/// </summary>
public async Task<Result> UpdateGroupAsync(ADGroup group, CancellationToken cancellationToken = default)
{
    return await Task.Run(() =>
    {
        try
        {
            _logger.LogInformation("Updating group: {GroupName}", group.Name);

            using var context = GetPrincipalContext();
            using var groupPrincipal = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, group.SamAccountName);

            if (groupPrincipal == null)
            {
                _logger.LogWarning("Group not found: {GroupName}", group.Name);
                return Result.Failure($"Group '{group.Name}' not found");
            }

            // Update properties
            groupPrincipal.Description = group.Description;

            // Use DirectoryEntry for additional properties
            using var entry = groupPrincipal.GetUnderlyingObject() as DirectoryEntry;
            if (entry != null)
            {
                if (!string.IsNullOrWhiteSpace(group.Email))
                    entry.Properties["mail"].Value = group.Email;

                if (!string.IsNullOrWhiteSpace(group.ManagedBy))
                    entry.Properties["managedBy"].Value = group.ManagedBy;

                entry.CommitChanges();
            }

            groupPrincipal.Save();

            _logger.LogInformation("Successfully updated group: {GroupName}", group.Name);
            return Result.Success($"Group '{group.Name}' updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group: {GroupName}", group.Name);
            return Result.Failure($"Error updating group: {ex.Message}");
        }
    }, cancellationToken);
}
public async Task<Result> UpdateUserAsync(ADUser user, CancellationToken cancellationToken = default)
{
    return await Task.Run(() =>
    {
        try
        {
            using var entry = GetDirectoryEntry(user.DistinguishedName);

            // Update example fields
            entry.Properties["displayName"].Value = user.DisplayName;
            entry.Properties["description"].Value = user.Description;
            entry.Properties["department"].Value = user.Department;
            entry.Properties["title"].Value = user.Title;
            entry.Properties["company"].Value = user.Company;
            entry.Properties["officeName"].Value = user.Office;
            //entry.Properties["telephoneNumber"].Value = user.TelephoneNumber;
            //entry.Properties["mobile"].Value = user.Mobile;

            entry.CommitChanges();

            _logger.LogInformation("Updated user {Username}", user.Username);
            return Result.Success("User updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {Username}", user.Username);
            return Result.Failure($"Error updating user: {ex.Message}");
        }
    }, cancellationToken);
}

    #endregion

    #region Helper Methods for Group Mapping

    /// <summary>
    /// Maps SearchResult to ADGroup
    /// </summary>
    private ADGroup MapSearchResultToADGroup(SearchResult result)
{
    var groupType = GetInt32Property(result, "groupType");
    
    return new ADGroup
    {
        Name = GetProperty(result, "cn"),
        DistinguishedName = GetProperty(result, "distinguishedName"),
        Description = GetProperty(result, "description"),
        GroupScope = GetGroupScope(groupType),
        GroupType = GetGroupType(groupType),
        Email = GetProperty(result, "mail"),
        ManagedBy = GetProperty(result, "managedBy"),
        Members = GetMultiValueProperty(result, "member"),
        MemberOf = GetMultiValueProperty(result, "memberOf"),
        WhenCreated = GetDateTimeProperty(result, "whenCreated"),
        WhenChanged = GetDateTimeProperty(result, "whenChanged"),
        SamAccountName = GetProperty(result, "sAMAccountName")
    };
}

/// <summary>
/// Maps GroupPrincipal to ADGroup
/// </summary>
private ADGroup MapGroupPrincipalToADGroup(GroupPrincipal group)
{
    var members = new List<string>();
    var memberOf = new List<string>();

    try
    {
        var groupMembers = group.GetMembers();
        members = groupMembers.Select(m => m.DistinguishedName).ToList();
    }
    catch { /* Ignore errors getting members */ }

    try
    {
        var groups = group.GetGroups();
        memberOf = groups.Select(g => g.DistinguishedName).ToList();
    }
    catch { /* Ignore errors getting groups */ }

    return new ADGroup
    {
        Name = group.Name,
        DistinguishedName = group.DistinguishedName,
        Description = group.Description,
        GroupScope = group.GroupScope?.ToString() ?? "Unknown",
        GroupType = group.IsSecurityGroup == true ? "Security" : "Distribution",
        Email = string.Empty, // Not available in GroupPrincipal
        ManagedBy = string.Empty, // Not available in GroupPrincipal
        Members = members,
        MemberOf = memberOf,
        WhenCreated = DateTime.MinValue, // Not available in GroupPrincipal
        WhenChanged = DateTime.MinValue, // Not available in GroupPrincipal
        SamAccountName = group.SamAccountName
    };
}

/// <summary>
/// Gets group scope from groupType flag
/// </summary>
private string GetGroupScope(int groupType)
{
    // GroupType flags:
    // 2 = Global
    // 4 = Domain Local
    // 8 = Universal

    if ((groupType & 8) == 8)
        return "Universal";
    if ((groupType & 4) == 4)
        return "DomainLocal";
    if ((groupType & 2) == 2)
        return "Global";

    return "Unknown";
}

/// <summary>
/// Gets group type from groupType flag
/// </summary>
private string GetGroupType(int groupType)
{
    // GroupType flags:
    // -2147483648 (0x80000000) = Security Group
    // If not security, it's a Distribution Group

    return (groupType & -2147483648) != 0 ? "Security" : "Distribution";
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
    private DirectoryEntry GetDirectoryEntry(string? searchPath = null)
    {
        try
        {
            string ldapPath = string.IsNullOrWhiteSpace(searchPath)
                ? _config.LdapPath
                : searchPath;

            AuthenticationTypes authType = AuthenticationTypes.Secure;
            if (_config.UseSSL)
                authType |= AuthenticationTypes.SecureSocketsLayer;

            string? username = _config.Username;
            string? password = _config.Password;

            // Chuẩn hóa username (DOMAIN\username) nếu cần
            if (!string.IsNullOrWhiteSpace(username) &&
                !username.Contains("\\") && !username.Contains("@") &&
                !string.IsNullOrWhiteSpace(_config.Domain))
            {
                username = $"{_config.Domain}\\{username}";
            }

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                _logger.LogDebug("Creating DirectoryEntry with credentials for {LdapPath}", ldapPath);
                return new DirectoryEntry(ldapPath, username, password, authType);
            }

            _logger.LogDebug("Creating DirectoryEntry without explicit credentials for {LdapPath}", ldapPath);
            return new DirectoryEntry(ldapPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create DirectoryEntry");
            throw;
        }
    }


    #endregion
}
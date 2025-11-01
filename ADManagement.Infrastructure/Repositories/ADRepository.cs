using ADManagement.Application.Configuration;
using ADManagement.Domain.Common;
using ADManagement.Domain.Entities;
using ADManagement.Domain.Exceptions;
using ADManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace ADManagement.Infrastructure.Repositories;

/// <summary>
/// ✨ OPTIMIZED Repository implementation for Active Directory operations
/// - Connection pooling
/// - Batch operations
/// - Streaming support
/// - Proper resource disposal
/// - Parallel processing
/// </summary>
public class ADRepository : IADRepository, IDisposable
{
    private readonly ADConfiguration _config;
    private readonly ILogger<ADRepository> _logger;

    // ✨ NEW: Connection pooling
    private readonly SemaphoreSlim _connectionLock;
    private readonly ConcurrentBag<DirectoryEntry> _connectionPool;
    private bool _disposed;

    // Cache for search results
    private static readonly ConcurrentDictionary<string, (DateTime Expire, List<ADUser> Users)> _searchCache = new();

    // ✨ NEW: Constants for optimization
    private const int BATCH_SIZE = 100;
    private const int MAX_POOL_SIZE = 10;

    public ADRepository(ADConfiguration config, ILogger<ADRepository> logger)
    {
        _config = config;
        _logger = logger;
        _connectionLock = new SemaphoreSlim(config.MaxConcurrentOperations, config.MaxConcurrentOperations);
        _connectionPool = new ConcurrentBag<DirectoryEntry>();

        _logger.LogInformation(
            "ADRepository initialized - PoolSize: {PoolSize}, MaxConcurrent: {MaxConcurrent}",
            config.ConnectionPoolSize,
            config.MaxConcurrentOperations);
    }

    #region Connection Management

    /// <summary>
    /// ✨ OPTIMIZED: Get pooled DirectoryEntry
    /// </summary>
    private DirectoryEntry GetDirectoryEntry(string? path = null)
    {
        // Try get from pool first
        if (_connectionPool.TryTake(out var entry))
        {
            try
            {
                // Validate connection
                _ = entry.NativeObject;
                _logger.LogDebug("Reusing pooled connection");
                return entry;
            }
            catch
            {
                entry?.Dispose();
            }
        }

        // Create new connection
        var authType = AuthenticationTypes.Secure;
        if (_config.UseSSL)
            authType |= AuthenticationTypes.SecureSocketsLayer;

        var ldapPath = path ?? _config.LdapPath;

        var newEntry = new DirectoryEntry(
            ldapPath,
            _config.Username,
            _config.Password,
            authType);

        _logger.LogDebug("Created new connection");
        return newEntry;
    }

    /// <summary>
    /// ✨ NEW: Return connection to pool
    /// </summary>
    private void ReturnToPool(DirectoryEntry entry)
    {
        if (_connectionPool.Count < MAX_POOL_SIZE)
        {
            try
            {
                // Validate before returning to pool
                _ = entry.NativeObject;
                _connectionPool.Add(entry);
                _logger.LogDebug("Returned connection to pool");
            }
            catch
            {
                entry?.Dispose();
            }
        }
        else
        {
            entry?.Dispose();
        }
    }

    public async Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var targetHost = string.IsNullOrWhiteSpace(_config.LdapServer) ? _config.Domain : _config.LdapServer;
            var port = _config.Port <= 0 ? 389 : _config.Port;

            _logger.LogInformation("Testing connection to {Host}:{Port}", targetHost, port);

            // Quick TCP test
            var quickOk = await TryTcpConnectAsync(targetHost, port, TimeSpan.FromSeconds(Math.Max(3, _config.TimeoutSeconds)));
            if (!quickOk)
            {
                _logger.LogWarning("TCP connection failed to {Host}:{Port}", targetHost, port);
                return Result.Failure($"Network connection to {targetHost}:{port} failed. Check DNS/network/firewall.");
            }

            // Try LDAP bind
            return await Task.Run(() =>
            {
                try
                {
                    using var context = GetPrincipalContext();
                    using var searcher = new PrincipalSearcher(new UserPrincipal(context));
                    using var result = searcher.FindOne();

                    _logger.LogInformation("AD connection successful");
                    return Result.Success("Connection successful");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AD bind failed");
                    return Result.Failure($"AD bind failed: {ex.Message}");
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return Result.Failure($"Connection test failed: {ex.Message}");
        }
    }

    private async Task<bool> TryTcpConnectAsync(string host, int port, TimeSpan timeout)
    {
        using var client = new TcpClient();
        try
        {
            var connectTask = client.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            return completedTask == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private PrincipalContext GetPrincipalContext()
    {
        if (!string.IsNullOrWhiteSpace(_config.Username) && !string.IsNullOrWhiteSpace(_config.Password))
        {
            return new PrincipalContext(
                ContextType.Domain,
                _config.Domain,
                _config.Username,
                _config.Password);
        }

        return new PrincipalContext(ContextType.Domain, _config.Domain);
    }

    #endregion

    #region User Operations - Optimized

    public async Task<Result<IEnumerable<ADUser>>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        DirectoryEntry? entry = null;
        SearchResultCollection? results = null;

        try
        {
            _logger.LogInformation("Retrieving all users (optimized)");

            entry = GetDirectoryEntry();
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = "(&(objectClass=user)(objectCategory=person))",
                PageSize = _config.PageSize
            };

            LoadUserProperties(searcher);

            var users = new List<ADUser>();
            results = searcher.FindAll();

            foreach (SearchResult result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    users.Add(MapSearchResultToADUser(result));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to map user from search result");
                }
                finally
                {
                    result?.Dispose(); // ✨ CRITICAL: Dispose each result
                }
            }

            _logger.LogInformation("Retrieved {Count} users", users.Count);
            return Result.Success<IEnumerable<ADUser>>(users, $"Retrieved {users.Count} users");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Get all users cancelled");
            return Result.Failure<IEnumerable<ADUser>>("Operation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return Result.Failure<IEnumerable<ADUser>>($"Failed to retrieve users: {ex.Message}");
        }
        finally
        {
            results?.Dispose();
            if (entry != null)
                ReturnToPool(entry);
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// ✨ NEW: Stream users asynchronously to avoid loading all in memory
    /// </summary>
    public async IAsyncEnumerable<ADUser> StreamUsersAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        DirectoryEntry? entry = null;
        SearchResultCollection? results = null;

        try
        {
            _logger.LogInformation("Streaming users (optimized for memory)");

            entry = GetDirectoryEntry();
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = "(&(objectClass=user)(objectCategory=person))",
                PageSize = _config.PageSize // ✨ CRITICAL: Paging enables streaming
            };

            LoadUserProperties(searcher);
            results = searcher.FindAll();

            _logger.LogDebug("Starting to stream users with page size: {PageSize}", _config.PageSize);

            foreach (SearchResult result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ADUser? user = null;
                try
                {
                    user = MapSearchResultToADUser(result);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to map user, skipping");
                }
                finally
                {
                    result?.Dispose(); // ✨ CRITICAL: Dispose immediately
                }

                if (user != null)
                {
                    yield return user;
                }
            }
        }
        finally
        {
            results?.Dispose();
            if (entry != null)
                ReturnToPool(entry);
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// ✨ NEW: Stream users from specific OU
    /// </summary>
    public async IAsyncEnumerable<ADUser> StreamUsersByOUAsync(
        string ouPath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        DirectoryEntry? entry = null;
        SearchResultCollection? results = null;

        try
        {
            _logger.LogInformation("Streaming users from OU: {OUPath}", ouPath);

            entry = GetDirectoryEntry($"{_config.LdapPath}/{ouPath}");
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = "(&(objectClass=user)(objectCategory=person))",
                PageSize = _config.PageSize
            };

            LoadUserProperties(searcher);
            results = searcher.FindAll();

            foreach (SearchResult result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ADUser? user = null;
                try
                {
                    user = MapSearchResultToADUser(result);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to map user from OU");
                }
                finally
                {
                    result?.Dispose();
                }

                if (user != null)
                {
                    yield return user;
                }
            }
        }
        finally
        {
            results?.Dispose();
            if (entry != null)
                ReturnToPool(entry);
            _connectionLock.Release();
        }
    }

    public async Task<Result<IEnumerable<ADUser>>> GetUsersByOUAsync(string ouPath, CancellationToken cancellationToken = default)
    {
        // Use streaming internally
        var users = new List<ADUser>();
        await foreach (var user in StreamUsersByOUAsync(ouPath, cancellationToken))
        {
            users.Add(user);
        }
        return Result.Success<IEnumerable<ADUser>>(users, $"Retrieved {users.Count} users from OU");
    }

    public async Task<Result<ADUser>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        DirectoryEntry? entry = null;
        SearchResultCollection? results = null;

        try
        {
            _logger.LogInformation("Retrieving user by username: {Username}", username);

            entry = GetDirectoryEntry();
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = $"(&(objectClass=user)(objectCategory=person)(sAMAccountName={EscapeLdapFilter(username)}))",
                SearchScope = System.DirectoryServices.SearchScope.Subtree
            };

            LoadUserProperties(searcher);
            results = searcher.FindAll();

            if (results.Count == 0)
            {
                return Result.Failure<ADUser>($"User '{username}' not found");
            }

            var user = MapSearchResultToADUser(results[0]);
            results[0]?.Dispose();

            _logger.LogInformation("Successfully retrieved user: {Username}", username);
            return Result.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by username: {Username}", username);
            return Result.Failure<ADUser>($"Failed to retrieve user: {ex.Message}");
        }
        finally
        {
            results?.Dispose();
            if (entry != null)
                ReturnToPool(entry);
            _connectionLock.Release();
        }
    }

    public async Task<Result<ADUser>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        DirectoryEntry? entry = null;
        SearchResultCollection? results = null;

        try
        {
            _logger.LogInformation("Retrieving user by email: {Email}", email);

            entry = GetDirectoryEntry();
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = $"(&(objectClass=user)(objectCategory=person)(mail={EscapeLdapFilter(email)}))",
                SearchScope = System.DirectoryServices.SearchScope.Subtree
            };

            LoadUserProperties(searcher);
            results = searcher.FindAll();

            if (results.Count == 0)
            {
                return Result.Failure<ADUser>($"User with email '{email}' not found");
            }

            var user = MapSearchResultToADUser(results[0]);
            results[0]?.Dispose();

            return Result.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            return Result.Failure<ADUser>($"Failed to retrieve user: {ex.Message}");
        }
        finally
        {
            results?.Dispose();
            if (entry != null)
                ReturnToPool(entry);
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// ✨ OPTIMIZED: Search with caching
    /// </summary>
    public async Task<Result<IEnumerable<ADUser>>> SearchUsersAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (!string.IsNullOrWhiteSpace(searchTerm) && _config.SearchCacheSeconds > 0)
        {
            var cacheKey = $"search:{searchTerm.ToLowerInvariant()}";
            if (_searchCache.TryGetValue(cacheKey, out var cached))
            {
                if (DateTime.UtcNow < cached.Expire)
                {
                    _logger.LogDebug("Returning cached search results for: {SearchTerm}", searchTerm);
                    return Result.Success<IEnumerable<ADUser>>(cached.Users, $"Found {cached.Users.Count} users (cached)");
                }
                else
                {
                    _searchCache.TryRemove(cacheKey, out _);
                }
            }
        }

        await _connectionLock.WaitAsync(cancellationToken);
        DirectoryEntry? entry = null;
        SearchResultCollection? results = null;

        try
        {
            _logger.LogInformation("Searching users: {SearchTerm}", searchTerm);

            entry = GetDirectoryEntry();

            // Build fuzzy search filter
            var filter = $"(&(objectClass=user)(objectCategory=person)(|(cn=*{EscapeLdapFilter(searchTerm)}*)(sAMAccountName=*{EscapeLdapFilter(searchTerm)}*)(displayName=*{EscapeLdapFilter(searchTerm)}*)(mail=*{EscapeLdapFilter(searchTerm)}*)))";

            using var searcher = new DirectorySearcher(entry)
            {
                Filter = filter,
                PageSize = _config.PageSize
            };

            LoadUserProperties(searcher);
            results = searcher.FindAll();

            var users = new List<ADUser>();
            foreach (SearchResult result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    users.Add(MapSearchResultToADUser(result));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to map user from search result");
                }
                finally
                {
                    result?.Dispose();
                }
            }

            // Cache results
            if (!string.IsNullOrWhiteSpace(searchTerm) && _config.SearchCacheSeconds > 0)
            {
                var cacheKey = $"search:{searchTerm.ToLowerInvariant()}";
                var expiry = DateTime.UtcNow.AddSeconds(_config.SearchCacheSeconds);
                _searchCache[cacheKey] = (expiry, users);
            }

            _logger.LogInformation("Found {Count} users matching search term", users.Count);
            return Result.Success<IEnumerable<ADUser>>(users, $"Found {users.Count} users");
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<IEnumerable<ADUser>>("Search cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users: {SearchTerm}", searchTerm);
            return Result.Failure<IEnumerable<ADUser>>($"Search failed: {ex.Message}");
        }
        finally
        {
            results?.Dispose();
            if (entry != null)
                ReturnToPool(entry);
            _connectionLock.Release();
        }
    }

    public async Task<Result> ChangePasswordAsync(
        string username,
        string newPassword,
        bool mustChangeAtNextLogon,
        CancellationToken cancellationToken = default)
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

    public async Task<Result> SetUserStatusAsync(
        string username,
        bool enabled,
        CancellationToken cancellationToken = default)
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

                // Create user
                using var newUser = new UserPrincipal(context)
                {
                    SamAccountName = username,
                    GivenName = firstName,
                    Surname = lastName,
                    DisplayName = displayName ?? $"{firstName} {lastName}",
                    EmailAddress = email,
                    Enabled = accountEnabled,
                    PasswordNeverExpires = passwordNeverExpires
                };

                newUser.SetPassword(password);
                if (mustChangePasswordOnNextLogon)
                {
                    newUser.ExpirePasswordNow();
                }

                newUser.Save();

                // Set additional properties via DirectoryEntry
                if (newUser.GetUnderlyingObject() is DirectoryEntry entry)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(description))
                            entry.Properties["description"].Value = description;
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
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set additional properties for user {Username}", username);
                    }
                }

                // Add to initial groups
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
                        }
                    }
                }

                // Retrieve created user
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

    public async Task<Result> UpdateUserAsync(ADUser user, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            DirectoryEntry? entry = null;
            try
            {
                _logger.LogInformation("Updating user: {Username}", user.Username);

                entry = GetDirectoryEntry(user.DistinguishedName);

                // Update properties
                SetPropertyIfNotNull(entry, "displayName", user.DisplayName);
                SetPropertyIfNotNull(entry, "givenName", user.FirstName);
                SetPropertyIfNotNull(entry, "sn", user.LastName);
                SetPropertyIfNotNull(entry, "description", user.Description);
                SetPropertyIfNotNull(entry, "department", user.Department);
                SetPropertyIfNotNull(entry, "title", user.Title);
                SetPropertyIfNotNull(entry, "company", user.Company);
                SetPropertyIfNotNull(entry, "physicalDeliveryOfficeName", user.Office);
                SetPropertyIfNotNull(entry, "telephoneNumber", user.PhoneNumber);
                SetPropertyIfNotNull(entry, "mail", user.Email);

                entry.CommitChanges();

                _logger.LogInformation("Updated user {Username}", user.Username);
                return Result.Success("User updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Username}", user.Username);
                return Result.Failure($"Error updating user: {ex.Message}");
            }
            finally
            {
                if (entry != null)
                    ReturnToPool(entry);
            }
        }, cancellationToken);
    }

    private void SetPropertyIfNotNull(DirectoryEntry entry, string propertyName, string? value)
    {
        if (value != null)
        {
            if (entry.Properties.Contains(propertyName))
            {
                entry.Properties[propertyName].Value = value;
            }
            else
            {
                entry.Properties[propertyName].Add(value);
            }
        }
    }

    #endregion

    #region Group Operations

    /// <summary>
    /// ✨ OPTIMIZED: Get group members with batch querying
    /// </summary>
    public async Task<Result<IEnumerable<ADUser>>> GetGroupMembersAsync(
        string groupName,
        CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        DirectoryEntry? entry = null;
        SearchResultCollection? groupResults = null;

        try
        {
            _logger.LogInformation("Getting members of group: {GroupName} (optimized)", groupName);

            // STEP 1: Get all member DNs in one query
            entry = GetDirectoryEntry();
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = $"(&(objectCategory=group)(cn={EscapeLdapFilter(groupName)}))",
                PropertiesToLoad = { "member" }
            };

            groupResults = searcher.FindAll();
            if (groupResults.Count == 0)
            {
                return Result.Failure<IEnumerable<ADUser>>($"Group '{groupName}' not found");
            }

            var memberDns = new List<string>();
            var groupResult = groupResults[0];

            if (groupResult.Properties.Contains("member"))
            {
                foreach (var dn in groupResult.Properties["member"])
                {
                    if (dn is string memberDn)
                    {
                        memberDns.Add(memberDn);
                    }
                }
            }

            groupResult?.Dispose();

            if (memberDns.Count == 0)
            {
                return Result.Success<IEnumerable<ADUser>>(new List<ADUser>());
            }

            _logger.LogDebug("Group {GroupName} has {Count} members, fetching in batches", groupName, memberDns.Count);

            // STEP 2: Batch query all members
            var users = await BatchQueryUsersAsync(memberDns, cancellationToken);

            _logger.LogInformation("Retrieved {Count} members for group {GroupName}", users.Count, groupName);
            return Result.Success<IEnumerable<ADUser>>(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group members: {GroupName}", groupName);
            return Result.Failure<IEnumerable<ADUser>>($"Error: {ex.Message}");
        }
        finally
        {
            groupResults?.Dispose();
            if (entry != null)
                ReturnToPool(entry);
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// ✨ NEW: Batch query users by DNs
    /// </summary>
    private async Task<List<ADUser>> BatchQueryUsersAsync(
        List<string> distinguishedNames,
        CancellationToken cancellationToken)
    {
        var users = new ConcurrentBag<ADUser>();
        var batches = distinguishedNames
            .Select((dn, index) => new { dn, index })
            .GroupBy(x => x.index / BATCH_SIZE)
            .Select(g => g.Select(x => x.dn).ToList())
            .ToList();

        _logger.LogDebug("Processing {BatchCount} batches of {BatchSize}", batches.Count, BATCH_SIZE);

        // Process batches in parallel
        await Task.Run(() =>
        {
            Parallel.ForEach(
                batches,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _config.MaxParallelDegree,
                    CancellationToken = cancellationToken
                },
                batch =>
                {
                    DirectoryEntry? batchEntry = null;
                    SearchResultCollection? batchResults = null;

                    try
                    {
                        batchEntry = GetDirectoryEntry();

                        // Build OR filter for batch
                        var filterParts = batch.Select(dn =>
                            $"(distinguishedName={EscapeLdapFilter(dn)})");

                        var batchFilter = $"(&(objectClass=user)(objectCategory=person)(|{string.Join("", filterParts)}))";

                        using var batchSearcher = new DirectorySearcher(batchEntry)
                        {
                            Filter = batchFilter,
                            PageSize = BATCH_SIZE
                        };

                        LoadUserProperties(batchSearcher);
                        batchResults = batchSearcher.FindAll();

                        foreach (SearchResult result in batchResults)
                        {
                            try
                            {
                                users.Add(MapSearchResultToADUser(result));
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to map user from batch");
                            }
                            finally
                            {
                                result?.Dispose();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process batch");
                    }
                    finally
                    {
                        batchResults?.Dispose();
                        if (batchEntry != null)
                            ReturnToPool(batchEntry);
                    }
                });
        }, cancellationToken);

        return users.ToList();
    }

    public async Task<Result<IEnumerable<ADGroup>>> GetAllGroupsAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        DirectoryEntry? entry = null;
        SearchResultCollection? results = null;

        try
        {
            _logger.LogInformation("Getting all groups from Active Directory");

            entry = GetDirectoryEntry();
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = "(objectCategory=group)",
                PageSize = _config.PageSize
            };

            searcher.PropertiesToLoad.AddRange(new[]
            {
                "cn", "name", "distinguishedName", "description",
                "groupType", "mail", "managedBy", "member", "memberOf",
                "whenCreated", "whenChanged", "sAMAccountName"
            });

            results = searcher.FindAll();
            var groups = new List<ADGroup>();

            foreach (SearchResult result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var group = MapSearchResultToADGroup(result);
                    groups.Add(group);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to map group from search result");
                }
                finally
                {
                    result?.Dispose();
                }
            }

            _logger.LogInformation("Retrieved {Count} groups", groups.Count);
            return Result.Success<IEnumerable<ADGroup>>(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all groups");
            return Result.Failure<IEnumerable<ADGroup>>($"Error getting groups: {ex.Message}");
        }
        finally
        {
            results?.Dispose();
            if (entry != null)
                ReturnToPool(entry);
            _connectionLock.Release();
        }
    }

    public async Task<Result<IEnumerable<ADGroup>>> SearchGroupsAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllGroupsAsync(cancellationToken);
        }

        await _connectionLock.WaitAsync(cancellationToken);
        DirectoryEntry? entry = null;
        SearchResultCollection? results = null;

        try
        {
            _logger.LogInformation("Searching groups with term: {SearchTerm}", searchTerm);

            entry = GetDirectoryEntry();

            // Fuzzy search in multiple fields
            var filter = $"(&(objectCategory=group)(|(cn=*{EscapeLdapFilter(searchTerm)}*)(name=*{EscapeLdapFilter(searchTerm)}*)(description=*{EscapeLdapFilter(searchTerm)}*)(displayName=*{EscapeLdapFilter(searchTerm)}*)))";

            using var searcher = new DirectorySearcher(entry)
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

            results = searcher.FindAll();
            var groups = new List<ADGroup>();

            foreach (SearchResult result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    groups.Add(MapSearchResultToADGroup(result));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to map group");
                }
                finally
                {
                    result?.Dispose();
                }
            }

            _logger.LogInformation("Found {Count} groups matching search term", groups.Count);
            return Result.Success<IEnumerable<ADGroup>>(groups, $"Found {groups.Count} groups");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching groups: {SearchTerm}", searchTerm);
            return Result.Failure<IEnumerable<ADGroup>>($"Search failed: {ex.Message}");
        }
        finally
        {
            results?.Dispose();
            if (entry != null)
                ReturnToPool(entry);
            _connectionLock.Release();
        }
    }

    public async Task<Result<ADGroup>> GetGroupByNameAsync(
        string groupName,
        CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        DirectoryEntry? entry = null;
        SearchResultCollection? results = null;

        try
        {
            _logger.LogInformation("Getting group by name: {GroupName}", groupName);

            entry = GetDirectoryEntry();
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = $"(&(objectCategory=group)(cn={EscapeLdapFilter(groupName)}))"
            };

            searcher.PropertiesToLoad.AddRange(new[]
            {
                "cn", "name", "distinguishedName", "description",
                "groupType", "mail", "managedBy", "member", "memberOf",
                "whenCreated", "whenChanged", "sAMAccountName"
            });

            results = searcher.FindAll();

            if (results.Count == 0)
            {
                return Result.Failure<ADGroup>($"Group '{groupName}' not found");
            }

            var group = MapSearchResultToADGroup(results[0]);
            results[0]?.Dispose();

            _logger.LogInformation("Successfully retrieved group: {GroupName}", groupName);
            return Result.Success(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group by name: {GroupName}", groupName);
            return Result.Failure<ADGroup>($"Error getting group: {ex.Message}");
        }
        finally
        {
            results?.Dispose();
            if (entry != null)
                ReturnToPool(entry);
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// ✨ OPTIMIZED: Get user groups with single query
    /// </summary>
    public async Task<Result<IEnumerable<ADGroup>>> GetUserGroupsAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        DirectoryEntry? entry = null;
        SearchResultCollection? results = null;

        try
        {
            _logger.LogInformation("Getting groups for user: {Username} (optimized)", username);

            entry = GetDirectoryEntry();
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = $"(&(objectClass=user)(sAMAccountName={EscapeLdapFilter(username)}))",
                PropertiesToLoad = { "memberOf" }
            };

            results = searcher.FindAll();

            if (results.Count == 0)
            {
                return Result.Failure<IEnumerable<ADGroup>>($"User '{username}' not found");
            }

            var groupDns = new List<string>();
            var userResult = results[0];

            if (userResult.Properties.Contains("memberOf"))
            {
                foreach (var dn in userResult.Properties["memberOf"])
                {
                    if (dn is string groupDn)
                    {
                        groupDns.Add(groupDn);
                    }
                }
            }

            userResult?.Dispose();

            if (groupDns.Count == 0)
            {
                return Result.Success<IEnumerable<ADGroup>>(new List<ADGroup>());
            }

            // Batch query groups
            var groups = await BatchQueryGroupsAsync(groupDns, cancellationToken);

            _logger.LogInformation("User {Username} is member of {Count} groups", username, groups.Count);
            return Result.Success<IEnumerable<ADGroup>>(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user groups: {Username}", username);
            return Result.Failure<IEnumerable<ADGroup>>($"Error: {ex.Message}");
        }
        finally
        {
            results?.Dispose();
            if (entry != null)
                ReturnToPool(entry);
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// ✨ NEW: Batch query groups by DNs
    /// </summary>
    private async Task<List<ADGroup>> BatchQueryGroupsAsync(
        List<string> distinguishedNames,
        CancellationToken cancellationToken)
    {
        var groups = new ConcurrentBag<ADGroup>();
        var batches = distinguishedNames
            .Select((dn, index) => new { dn, index })
            .GroupBy(x => x.index / BATCH_SIZE)
            .Select(g => g.Select(x => x.dn).ToList())
            .ToList();

        await Task.Run(() =>
        {
            Parallel.ForEach(
                batches,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _config.MaxParallelDegree,
                    CancellationToken = cancellationToken
                },
                batch =>
                {
                    DirectoryEntry? batchEntry = null;
                    SearchResultCollection? batchResults = null;

                    try
                    {
                        batchEntry = GetDirectoryEntry();

                        var filterParts = batch.Select(dn =>
                            $"(distinguishedName={EscapeLdapFilter(dn)})");

                        var batchFilter = $"(&(objectCategory=group)(|{string.Join("", filterParts)}))";

                        using var batchSearcher = new DirectorySearcher(batchEntry)
                        {
                            Filter = batchFilter,
                            PageSize = BATCH_SIZE
                        };

                        batchSearcher.PropertiesToLoad.AddRange(new[]
                        {
                            "cn", "name", "distinguishedName", "description",
                            "groupType", "mail", "managedBy", "whenCreated", "whenChanged"
                        });

                        batchResults = batchSearcher.FindAll();

                        foreach (SearchResult result in batchResults)
                        {
                            try
                            {
                                groups.Add(MapSearchResultToADGroup(result));
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to map group from batch");
                            }
                            finally
                            {
                                result?.Dispose();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process group batch");
                    }
                    finally
                    {
                        batchResults?.Dispose();
                        if (batchEntry != null)
                            ReturnToPool(batchEntry);
                    }
                });
        }, cancellationToken);

        return groups.ToList();
    }

    public async Task<Result> AddUserToGroupAsync(
        string username,
        string groupName,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Adding user {Username} to group {GroupName}", username, groupName);

                using var context = GetPrincipalContext();
                using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                if (user == null)
                {
                    return Result.Failure($"User '{username}' not found");
                }

                using var group = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, groupName);

                if (group == null)
                {
                    return Result.Failure($"Group '{groupName}' not found");
                }

                if (user.IsMemberOf(group))
                {
                    return Result.Success($"User '{username}' is already a member of group '{groupName}'");
                }

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

    public async Task<Result> RemoveUserFromGroupAsync(
        string username,
        string groupName,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Removing user {Username} from group {GroupName}", username, groupName);

                using var context = GetPrincipalContext();
                using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                if (user == null)
                {
                    return Result.Failure($"User '{username}' not found");
                }

                using var group = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, groupName);

                if (group == null)
                {
                    return Result.Failure($"Group '{groupName}' not found");
                }

                if (!user.IsMemberOf(group))
                {
                    return Result.Success($"User '{username}' is not a member of group '{groupName}'");
                }

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

    public async Task<Result<ADGroup>> CreateGroupAsync(
        string groupName,
        string description,
        string groupScope,
        string groupType,
        string organizationalUnit,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Creating group: {GroupName}", groupName);

                using var context = string.IsNullOrWhiteSpace(organizationalUnit)
                    ? GetPrincipalContext()
                    : GetPrincipalContext(); // TODO: Handle OU-specific context

                using var existingGroup = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, groupName);
                if (existingGroup != null)
                {
                    return Result.Failure<ADGroup>($"Group '{groupName}' already exists");
                }

                using var newGroup = new GroupPrincipal(context)
                {
                    SamAccountName = groupName,
                    Name = groupName,
                    Description = description
                };

                newGroup.Save();

                using var createdGroup = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, groupName);
                if (createdGroup == null)
                {
                    return Result.Failure<ADGroup>("Group was created but could not be retrieved");
                }

                var adGroup = MapGroupPrincipalToADGroup(createdGroup);
                _logger.LogInformation("Group {GroupName} created successfully", groupName);
                return Result.Success(adGroup, "Group created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group: {GroupName}", groupName);
                return Result.Failure<ADGroup>($"Failed to create group: {ex.Message}");
            }
        }, cancellationToken);
    }

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

    public async Task<Result> UpdateGroupAsync(ADGroup group, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            DirectoryEntry? entry = null;
            try
            {
                _logger.LogInformation("Updating group: {GroupName}", group.Name);

                entry = GetDirectoryEntry(group.DistinguishedName);

                SetPropertyIfNotNull(entry, "description", group.Description);
                SetPropertyIfNotNull(entry, "mail", group.Email);
                SetPropertyIfNotNull(entry, "managedBy", group.ManagedBy);

                entry.CommitChanges();

                _logger.LogInformation("Successfully updated group: {GroupName}", group.Name);
                return Result.Success($"Group '{group.Name}' updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group: {GroupName}", group.Name);
                return Result.Failure($"Error updating group: {ex.Message}");
            }
            finally
            {
                if (entry != null)
                    ReturnToPool(entry);
            }
        }, cancellationToken);
    }

    #endregion

    #region Organizational Unit Operations

    public async Task<Result<IEnumerable<OrganizationalUnit>>> GetAllOUsAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        DirectoryEntry? entry = null;
        SearchResultCollection? results = null;

        try
        {
            _logger.LogInformation("Getting all Organizational Units");

            entry = GetDirectoryEntry();
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = "(objectClass=organizationalUnit)",
                PageSize = _config.PageSize
            };

            searcher.PropertiesToLoad.AddRange(new[] { "name", "distinguishedName", "description" });

            results = searcher.FindAll();
            var ous = new List<OrganizationalUnit>();

            foreach (SearchResult result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var ou = new OrganizationalUnit
                    {
                        Name = GetProperty(result, "name"),
                        DistinguishedName = GetProperty(result, "distinguishedName"),
                        Description = GetProperty(result, "description")
                    };
                    ous.Add(ou);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to map OU");
                }
                finally
                {
                    result?.Dispose();
                }
            }

            _logger.LogInformation("Retrieved {Count} OUs", ous.Count);
            return Result.Success<IEnumerable<OrganizationalUnit>>(ous);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting OUs");
            return Result.Failure<IEnumerable<OrganizationalUnit>>($"Error getting OUs: {ex.Message}");
        }
        finally
        {
            results?.Dispose();
            if (entry != null)
                ReturnToPool(entry);
            _connectionLock.Release();
        }
    }

    #endregion

    #region Helper Methods

    private void LoadUserProperties(DirectorySearcher searcher)
    {
        searcher.PropertiesToLoad.AddRange(new[]
        {
            "sAMAccountName", "userPrincipalName", "displayName", "mail",
            "givenName", "sn", "middleName", "initials", "cn", "name",
            "description", "department", "title", "company", "manager",
            "physicalDeliveryOfficeName", "telephoneNumber", "homePhone",
            "mobile", "facsimileTelephoneNumber", "distinguishedName",
            "userAccountControl", "lockoutTime", "whenCreated", "whenChanged",
            "lastLogonTimestamp", "lastLogon", "pwdLastSet", "accountExpires",
            "memberOf"
        });
    }

    private ADUser MapSearchResultToADUser(SearchResult result)
    {
        return new ADUser
        {
            Username = GetProperty(result, "sAMAccountName"),
            UserPrincipalName = GetProperty(result, "userPrincipalName"),
            DisplayName = GetProperty(result, "displayName"),
            Email = GetProperty(result, "mail"),
            FirstName = GetProperty(result, "givenName"),
            LastName = GetProperty(result, "sn"),
            MiddleName = GetProperty(result, "middleName"),
            Initials = GetProperty(result, "initials"),
            Department = GetProperty(result, "department"),
            Title = GetProperty(result, "title"),
            Company = GetProperty(result, "company"),
            Office = GetProperty(result, "physicalDeliveryOfficeName"),
            Manager = GetProperty(result, "manager"),
            PhoneNumber = GetProperty(result, "telephoneNumber"),
            HomePhone = GetProperty(result, "homePhone"),
            MobileNumber = GetProperty(result, "mobile"),
            FaxNumber = GetProperty(result, "facsimileTelephoneNumber"),
            DistinguishedName = GetProperty(result, "distinguishedName"),
            Description = GetProperty(result, "description"),
            IsEnabled = IsAccountEnabled(GetProperty(result, "userAccountControl")),
            IsLockedOut = IsAccountLockedOut(result),
            WhenCreated = GetDateProperty(result, "whenCreated"),
            WhenChanged = GetDateProperty(result, "whenChanged"),
            LastLogon = GetFileTimeProperty(result, "lastLogonTimestamp"),
            LastPasswordSet = GetFileTimeProperty(result, "pwdLastSet"),
            AccountExpires = GetAccountExpires(result),
            MemberOf = GetMultiValueProperty(result, "memberOf")
        };
    }

    private ADUser MapUserPrincipalToADUser(UserPrincipal principal)
    {
        return new ADUser
        {
            Username = principal.SamAccountName ?? string.Empty,
            UserPrincipalName = principal.UserPrincipalName ?? string.Empty,
            DisplayName = principal.DisplayName ?? string.Empty,
            Email = principal.EmailAddress ?? string.Empty,
            FirstName = principal.GivenName ?? string.Empty,
            LastName = principal.Surname ?? string.Empty,
            DistinguishedName = principal.DistinguishedName ?? string.Empty,
            Description = principal.Description ?? string.Empty,
            IsEnabled = principal.Enabled ?? false,
            IsLockedOut = principal.IsAccountLockedOut()
        };
    }

    private ADGroup MapSearchResultToADGroup(SearchResult result)
    {
        return new ADGroup
        {
            Name = GetProperty(result, "cn"),
            DistinguishedName = GetProperty(result, "distinguishedName"),
            Description = GetProperty(result, "description"),
            Email = GetProperty(result, "mail"),
            ManagedBy = GetProperty(result, "managedBy"),
            GroupScope = GetProperty(result, "groupType"),
            GroupType = GetProperty(result, "groupType"),
            WhenCreated = GetDateProperty(result, "whenCreated"),
            WhenChanged = GetDateProperty(result, "whenChanged"),
            Members = GetMultiValueProperty(result, "member"),
            MemberOf = GetMultiValueProperty(result, "memberOf")
        };
    }

    private ADGroup MapGroupPrincipalToADGroup(GroupPrincipal principal)
    {
        return new ADGroup
        {
            Name = principal.Name ?? string.Empty,
            DistinguishedName = principal.DistinguishedName ?? string.Empty,
            Description = principal.Description ?? string.Empty
        };
    }

    private string GetProperty(SearchResult result, string propertyName)
    {
        return result.Properties.Contains(propertyName) &&
               result.Properties[propertyName].Count > 0
            ? result.Properties[propertyName][0]?.ToString() ?? string.Empty
            : string.Empty;
    }

    private DateTime? GetDateProperty(SearchResult result, string propertyName)
    {
        if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
        {
            var value = result.Properties[propertyName][0];
            if (value is DateTime dt)
                return dt;
        }
        return null;
    }

    private DateTime? GetFileTimeProperty(SearchResult result, string propertyName)
    {
        if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
        {
            var value = result.Properties[propertyName][0];
            if (value is long fileTime && fileTime > 0)
            {
                try
                {
                    return DateTime.FromFileTimeUtc(fileTime);
                }
                catch
                {
                    return null;
                }
            }
        }
        return null;
    }

    private DateTime? GetAccountExpires(SearchResult result)
    {
        if (result.Properties.Contains("accountExpires") && result.Properties["accountExpires"].Count > 0)
        {
            var value = result.Properties["accountExpires"][0];
            if (value is long accountExpires && accountExpires > 0 && accountExpires != 9223372036854775807)
            {
                try
                {
                    return DateTime.FromFileTimeUtc(accountExpires);
                }
                catch
                {
                    return null;
                }
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

    private bool IsAccountEnabled(string userAccountControl)
    {
        if (string.IsNullOrWhiteSpace(userAccountControl))
            return false;

        if (int.TryParse(userAccountControl, out var uac))
        {
            const int ADS_UF_ACCOUNTDISABLE = 0x0002;
            return (uac & ADS_UF_ACCOUNTDISABLE) == 0;
        }

        return false;
    }

    private bool IsAccountLockedOut(SearchResult result)
    {
        if (result.Properties.Contains("lockoutTime") && result.Properties["lockoutTime"].Count > 0)
        {
            var value = result.Properties["lockoutTime"][0];
            if (value is long lockoutTime)
            {
                return lockoutTime > 0;
            }
        }
        return false;
    }

    private string EscapeLdapFilter(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00")
            .Replace("/", "\\2f");
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Dispose all pooled connections
        while (_connectionPool.TryTake(out var entry))
        {
            try
            {
                entry?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing pooled connection");
            }
        }

        _connectionLock?.Dispose();

        _logger.LogInformation("ADRepository disposed");
    }

    #endregion
}
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using ADManagement.Application.Configuration;
using ADManagement.Domain.Common;
using ADManagement.Domain.Entities;
using ADManagement.Domain.Exceptions;
using ADManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ADManagement.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Active Directory operations
/// </summary>

public class ADRepository : IADRepository
{
    private readonly ADConfiguration _config;
    private readonly ILogger<ADRepository> _logger;

    public ADRepository(ADConfiguration config, ILogger<ADRepository> logger)
    {
        _config = config;
        _logger = logger;
    }

    #region Connection

    public async Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var context = GetPrincipalContext();
                using var searcher = new PrincipalSearcher(new UserPrincipal(context));
                _ = searcher.FindAll().Take(1).ToList();

                _logger.LogInformation("Successfully connected to Active Directory: {Domain}", _config.Domain);
                return Result.Success("Connection successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Active Directory");
                return Result.Failure($"Connection failed: {ex.Message}");
            }
        }, cancellationToken);
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

                using var context = GetPrincipalContext();

                // Search by username
                using var searchUser = new UserPrincipal(context)
                {
                    SamAccountName = $"*{searchTerm}*"
                };
                using var searcher = new PrincipalSearcher(searchUser);

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

    public async Task<Result> ChangePasswordAsync(string username, string newPassword, CancellationToken cancellationToken = default)
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

                using var context = GetPrincipalContext();
                using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                if (user == null)
                {
                    throw new UserNotFoundException(username);
                }

                var groups = new List<string>();
                var groupCollection = user.GetGroups();

                foreach (var group in groupCollection)
                {
                    if (group?.Name != null)
                    {
                        groups.Add(group.Name);
                    }
                    group?.Dispose();
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create PrincipalContext for domain: {Domain}", _config.Domain);
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

        // Get groups
        try
        {
            var groups = user.GetGroups();
            adUser.MemberOf = groups.Select(g => g.Name ?? string.Empty).ToList();

            foreach (var group in groups)
            {
                group?.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve groups for user: {Username}", user.SamAccountName);
            adUser.MemberOf = new List<string>();
        }

        return adUser;
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

    #endregion
}
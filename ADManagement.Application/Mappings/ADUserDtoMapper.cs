using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using ADManagement.Application.DTOs;
using ADManagement.Domain.Entities;

namespace ADManagement.Application.Mappings;

/// <summary>
/// Unified mapper for all ADUser DTO conversions
/// Handles mapping from:
/// 1. Domain.Entities.ADUser (Entity) → ADUserDto (DTO)
/// 2. DirectoryEntry (AD Object) → ADUserDto (DTO)
/// 3. SearchResult (AD Query Result) → ADUserDto (DTO)
/// 4. UserPrincipal (Account Management API) → ADUserDto (DTO)
/// </summary>
public static class ADUserDtoMapper
{
    #region From Domain Entity to DTO

    /// <summary>
    /// Maps ADUser entity to ADUserDto
    /// </summary>
    public static ADUserDto ToDto(this ADUser user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        return new ADUserDto
        {
            // Core Identity
            SamAccountName = user.Username ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            DistinguishedName = user.DistinguishedName ?? string.Empty,
            
            // Note: ADUser entity doesn't have all enhanced properties
            // Map what's available from the entity
            
            // Personal Information
            GivenName = user.FirstName ?? string.Empty,
            Surname = user.LastName ?? string.Empty,
            Description = user.Description ?? string.Empty,

            // Contact Information
            Email = user.Email ?? string.Empty,
            TelephoneNumber = user.PhoneNumber ?? string.Empty,
            Mobile = user.MobileNumber ?? string.Empty,
            Fax = user.FaxNumber ?? string.Empty,

            // Organization Information
            Department = user.Department ?? string.Empty,
            Title = user.Title ?? string.Empty,
            Company = user.Company ?? string.Empty,
            Office = user.Office ?? string.Empty,
            Manager = user.Manager ?? string.Empty,

            // Address Information
            StreetAddress = user.StreetAddress ?? string.Empty,
            City = user.City ?? string.Empty,
            State = user.State ?? string.Empty,
            PostalCode = user.PostalCode ?? string.Empty,
            Country = user.Country ?? string.Empty,

            // Account Status
            IsEnabled = user.IsEnabled,
            IsLocked = user.IsLockedOut,
            AccountStatus = user.AccountStatus ?? string.Empty,

            // Timestamps
            LastLogon = user.LastLogon,
            PasswordLastSet = user.LastPasswordSet,
            AccountExpirationDate = user.AccountExpires,
            WhenCreated = user.WhenCreated,
            WhenChanged = user.WhenChanged,

            // Group Membership
            MemberOf = user.MemberOf?.ToList() ?? new List<string>()
        };
    }

    /// <summary>
    /// Maps collection of ADUser entities to collection of ADUserDto
    /// </summary>
    public static IEnumerable<ADUserDto> ToDto(this IEnumerable<ADUser> users)
    {
        if (users == null)
            throw new ArgumentNullException(nameof(users));

        return users.Select(ToDto);
    }

    #endregion

    #region From DirectoryEntry to DTO (Enhanced - Full Properties)

    /// <summary>
    /// Maps DirectoryEntry to enhanced ADUserDto with all available properties
    /// This is the most comprehensive mapping with 100+ properties
    /// </summary>
    public static ADUserDto FromDirectoryEntry(DirectoryEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        var dto = new ADUserDto
        {
            // Core Identity
            SamAccountName = GetStringProperty(entry, "sAMAccountName"),
            UserPrincipalName = GetStringProperty(entry, "userPrincipalName"),
            DisplayName = GetStringProperty(entry, "displayName"),
            DistinguishedName = GetStringProperty(entry, "distinguishedName"),
            CanonicalName = GetStringProperty(entry, "canonicalName"),
            ObjectGuid = GetGuidProperty(entry, "objectGUID"),
            ObjectSid = GetSidProperty(entry, "objectSid"),

            // Personal Information
            GivenName = GetStringProperty(entry, "givenName"),
            Surname = GetStringProperty(entry, "sn"),
            MiddleName = GetStringProperty(entry, "middleName"),
            Initials = GetStringProperty(entry, "initials"),
            CommonName = GetStringProperty(entry, "cn"),
            Name = GetStringProperty(entry, "name"),
            Description = GetStringProperty(entry, "description"),
            EmployeeId = GetStringProperty(entry, "employeeID"),
            EmployeeNumber = GetStringProperty(entry, "employeeNumber"),
            EmployeeType = GetStringProperty(entry, "employeeType"),

            // Contact Information
            Email = GetStringProperty(entry, "mail"),
            ProxyAddresses = GetMultiValuedProperty(entry, "proxyAddresses"),
            TelephoneNumber = GetStringProperty(entry, "telephoneNumber"),
            HomePhone = GetStringProperty(entry, "homePhone"),
            Mobile = GetStringProperty(entry, "mobile"),
            Pager = GetStringProperty(entry, "pager"),
            Fax = GetStringProperty(entry, "facsimileTelephoneNumber"),
            IpPhone = GetStringProperty(entry, "ipPhone"),
            WebPage = GetStringProperty(entry, "wWWHomePage"),
            Url = GetMultiValuedProperty(entry, "url"),

            // Organization Information
            Department = GetStringProperty(entry, "department"),
            Title = GetStringProperty(entry, "title"),
            Company = GetStringProperty(entry, "company"),
            Manager = GetStringProperty(entry, "manager"),
            DirectReports = GetMultiValuedProperty(entry, "directReports"),
            Division = GetStringProperty(entry, "division"),
            Office = GetStringProperty(entry, "physicalDeliveryOfficeName"),
            PhysicalDeliveryOfficeName = GetStringProperty(entry, "physicalDeliveryOfficeName"),

            // Address Information
            StreetAddress = GetStringProperty(entry, "streetAddress"),
            PostOfficeBox = GetStringProperty(entry, "postOfficeBox"),
            City = GetStringProperty(entry, "l"),
            State = GetStringProperty(entry, "st"),
            PostalCode = GetStringProperty(entry, "postalCode"),
            CountryCode = GetStringProperty(entry, "c"),
            Country = GetStringProperty(entry, "co"),

            // Account Status
            UserAccountControl = GetIntProperty(entry, "userAccountControl"),
            IsEnabled = !IsAccountDisabled(entry),
            IsLocked = IsAccountLockedOut(entry),
            LockoutTime = GetDateTimeProperty(entry, "lockoutTime"),
            PasswordExpired = IsPasswordExpired(entry),
            MustChangePassword = MustChangePasswordAtNextLogon(entry),
            PasswordNeverExpires = HasPasswordNeverExpires(entry),
            CannotChangePassword = HasCannotChangePassword(entry),
            AccountNotDelegated = HasFlag(entry, "userAccountControl", 0x100000),
            UseDESKeyOnly = HasFlag(entry, "userAccountControl", 0x200000),
            DoesNotRequirePreAuth = HasFlag(entry, "userAccountControl", 0x400000),
            TrustedForDelegation = HasFlag(entry, "userAccountControl", 0x80000),
            TrustedToAuthForDelegation = HasFlag(entry, "userAccountControl", 0x1000000),
            SmartCardLogonRequired = HasFlag(entry, "userAccountControl", 0x40000),

            // Timestamps
            WhenCreated = GetDateTimeProperty(entry, "whenCreated"),
            WhenChanged = GetDateTimeProperty(entry, "whenChanged"),
            LastLogonTimestamp = GetDateTimeProperty(entry, "lastLogonTimestamp"),
            LastLogon = GetDateTimeProperty(entry, "lastLogon"),
            LastLogoff = GetDateTimeProperty(entry, "lastLogoff"),
            BadPasswordTime = GetDateTimeProperty(entry, "badPasswordTime"),
            PasswordLastSet = GetDateTimeProperty(entry, "pwdLastSet"),
            AccountExpires = GetLongProperty(entry, "accountExpires"),
            AccountExpirationDate = GetAccountExpirationDate(entry),

            // Logon Information
            LogonCount = GetIntProperty(entry, "logonCount"),
            BadPasswordCount = GetIntProperty(entry, "badPwdCount"),
            LogonHours = GetByteArrayProperty(entry, "logonHours"),
            UserWorkstations = GetStringProperty(entry, "userWorkstations"),
            ScriptPath = GetStringProperty(entry, "scriptPath"),
            ProfilePath = GetStringProperty(entry, "profilePath"),
            HomeDirectory = GetStringProperty(entry, "homeDirectory"),
            HomeDrive = GetStringProperty(entry, "homeDrive"),

            // Group Membership
            PrimaryGroupId = GetIntProperty(entry, "primaryGroupID"),
            MemberOf = GetMultiValuedProperty(entry, "memberOf"),

            // Exchange Attributes
            MailNickname = GetStringProperty(entry, "mailNickname"),
            TargetAddress = GetStringProperty(entry, "targetAddress"),
            LegacyExchangeDN = GetStringProperty(entry, "legacyExchangeDN"),
            MsExchVersion = GetLongProperty(entry, "msExchVersion"),
            HideFromAddressLists = GetBoolProperty(entry, "msExchHideFromAddressLists"),

            // Custom Attributes
            ExtensionAttribute1 = GetStringProperty(entry, "extensionAttribute1"),
            ExtensionAttribute2 = GetStringProperty(entry, "extensionAttribute2"),
            ExtensionAttribute3 = GetStringProperty(entry, "extensionAttribute3"),
            ExtensionAttribute4 = GetStringProperty(entry, "extensionAttribute4"),
            ExtensionAttribute5 = GetStringProperty(entry, "extensionAttribute5"),
            ExtensionAttribute6 = GetStringProperty(entry, "extensionAttribute6"),
            ExtensionAttribute7 = GetStringProperty(entry, "extensionAttribute7"),
            ExtensionAttribute8 = GetStringProperty(entry, "extensionAttribute8"),
            ExtensionAttribute9 = GetStringProperty(entry, "extensionAttribute9"),
            ExtensionAttribute10 = GetStringProperty(entry, "extensionAttribute10"),
            ExtensionAttribute11 = GetStringProperty(entry, "extensionAttribute11"),
            ExtensionAttribute12 = GetStringProperty(entry, "extensionAttribute12"),
            ExtensionAttribute13 = GetStringProperty(entry, "extensionAttribute13"),
            ExtensionAttribute14 = GetStringProperty(entry, "extensionAttribute14"),
            ExtensionAttribute15 = GetStringProperty(entry, "extensionAttribute15")
        };

        // Compute account status
        dto.AccountStatus = ComputeAccountStatus(dto);

        return dto;
    }

    /// <summary>
    /// Maps collection of DirectoryEntry to ADUserDto collection
    /// </summary>
    public static IEnumerable<ADUserDto> FromDirectoryEntries(IEnumerable<DirectoryEntry> entries)
    {
        if (entries == null)
            throw new ArgumentNullException(nameof(entries));

        return entries.Select(FromDirectoryEntry);
    }

    #endregion

    #region From SearchResult to DTO

    /// <summary>
    /// Maps SearchResult to ADUserDto
    /// Used when working with DirectorySearcher
    /// </summary>
    public static ADUserDto FromSearchResult(SearchResult result)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        var dto = new ADUserDto
        {
            // Core Identity
            SamAccountName = GetProperty(result, "sAMAccountName"),
            UserPrincipalName = GetProperty(result, "userPrincipalName"),
            DisplayName = GetProperty(result, "displayName"),
            DistinguishedName = GetProperty(result, "distinguishedName"),
            ObjectGuid = GetGuidPropertyFromSearchResult(result, "objectGUID"),
            ObjectSid = GetSidPropertyFromSearchResult(result, "objectSid"),

            // Personal Information
            GivenName = GetProperty(result, "givenName"),
            Surname = GetProperty(result, "sn"),
            MiddleName = GetProperty(result, "middleName"),
            Initials = GetProperty(result, "initials"),
            Description = GetProperty(result, "description"),
            EmployeeId = GetProperty(result, "employeeID"),
            EmployeeNumber = GetProperty(result, "employeeNumber"),
            EmployeeType = GetProperty(result, "employeeType"),

            // Contact Information
            Email = GetProperty(result, "mail"),
            TelephoneNumber = GetProperty(result, "telephoneNumber"),
            HomePhone = GetProperty(result, "homePhone"),
            Mobile = GetProperty(result, "mobile"),
            Pager = GetProperty(result, "pager"),
            Fax = GetProperty(result, "facsimileTelephoneNumber"),
            IpPhone = GetProperty(result, "ipPhone"),

            // Organization Information
            Department = GetProperty(result, "department"),
            Title = GetProperty(result, "title"),
            Company = GetProperty(result, "company"),
            Manager = GetProperty(result, "manager"),
            Office = GetProperty(result, "physicalDeliveryOfficeName"),

            // Address Information
            StreetAddress = GetProperty(result, "streetAddress"),
            City = GetProperty(result, "l"),
            State = GetProperty(result, "st"),
            PostalCode = GetProperty(result, "postalCode"),
            Country = GetProperty(result, "co"),

            // Account Status
            UserAccountControl = GetInt32Property(result, "userAccountControl"),
            IsEnabled = !IsAccountDisabledFromSearchResult(result),
            IsLocked = GetInt64Property(result, "lockoutTime") > 0,

            // Timestamps
            WhenCreated = GetDateTimeProperty(result, "whenCreated"),
            WhenChanged = GetDateTimeProperty(result, "whenChanged"),
            LastLogon = ConvertFileTimeToDateTime(GetInt64Property(result, "lastLogon")),
            LastLogonTimestamp = ConvertFileTimeToDateTime(GetInt64Property(result, "lastLogonTimestamp")),
            PasswordLastSet = ConvertFileTimeToDateTime(GetInt64Property(result, "pwdLastSet")),
            AccountExpirationDate = ConvertFileTimeToDateTime(GetInt64Property(result, "accountExpires")),

            // Logon Information
            LogonCount = GetInt32Property(result, "logonCount"),
            BadPasswordCount = GetInt32Property(result, "badPwdCount"),

            // Group Membership
            MemberOf = GetMultiValueProperty(result, "memberOf")
        };

        // Compute flags
        dto.PasswordExpired = HasFlagFromSearchResult(result, "userAccountControl", 0x800000);
        dto.PasswordNeverExpires = HasFlagFromSearchResult(result, "userAccountControl", 0x10000);
        dto.CannotChangePassword = HasFlagFromSearchResult(result, "userAccountControl", 0x0040);
        dto.MustChangePassword = GetInt64Property(result, "pwdLastSet") == 0;

        dto.AccountStatus = ComputeAccountStatus(dto);

        return dto;
    }

    /// <summary>
    /// Maps collection of SearchResult to ADUserDto collection
    /// </summary>
    public static IEnumerable<ADUserDto> FromSearchResults(SearchResultCollection results)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));

        var list = new List<ADUserDto>();
        foreach (SearchResult result in results)
        {
            list.Add(FromSearchResult(result));
        }
        return list;
    }

    #endregion

    #region From UserPrincipal to DTO

    /// <summary>
    /// Maps UserPrincipal (Account Management API) to ADUserDto
    /// </summary>
    public static ADUserDto FromUserPrincipal(UserPrincipal user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var dto = new ADUserDto
        {
            // Core Identity
            SamAccountName = user.SamAccountName ?? string.Empty,
            UserPrincipalName = user.UserPrincipalName ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            DistinguishedName = user.DistinguishedName ?? string.Empty,
            GivenName = user.GivenName ?? string.Empty,
            Surname = user.Surname ?? string.Empty,
            Description = user.Description ?? string.Empty,
            Email = user.EmailAddress ?? string.Empty,
            TelephoneNumber = user.VoiceTelephoneNumber ?? string.Empty,

            // Account Status
            IsEnabled = user.Enabled ?? false,
            IsLocked = user.IsAccountLockedOut(),

            // Timestamps
            LastLogon = user.LastLogon,
            PasswordLastSet = user.LastPasswordSet,
            AccountExpirationDate = user.AccountExpirationDate
        };

        // Get additional properties from underlying DirectoryEntry
        try
        {
            if (user.GetUnderlyingObject() is DirectoryEntry entry)
            {
                // Get properties not available in UserPrincipal
                dto.Department = GetStringProperty(entry, "department");
                dto.Title = GetStringProperty(entry, "title");
                dto.Company = GetStringProperty(entry, "company");
                dto.Office = GetStringProperty(entry, "physicalDeliveryOfficeName");
                dto.Manager = GetStringProperty(entry, "manager");
                dto.Mobile = GetStringProperty(entry, "mobile");
                dto.Fax = GetStringProperty(entry, "facsimileTelephoneNumber");
                dto.StreetAddress = GetStringProperty(entry, "streetAddress");
                dto.City = GetStringProperty(entry, "l");
                dto.State = GetStringProperty(entry, "st");
                dto.PostalCode = GetStringProperty(entry, "postalCode");
                dto.Country = GetStringProperty(entry, "co");
                dto.MemberOf = GetMultiValuedProperty(entry, "memberOf");
                dto.UserAccountControl = GetIntProperty(entry, "userAccountControl");
                
                // Compute flags
                if (dto.UserAccountControl.HasValue)
                {
                    var uac = dto.UserAccountControl.Value;
                    dto.PasswordExpired = (uac & 0x800000) == 0x800000;
                    dto.PasswordNeverExpires = (uac & 0x10000) == 0x10000;
                    dto.CannotChangePassword = (uac & 0x0040) == 0x0040;
                }
            }
        }
        catch
        {
            // If we can't get underlying object, continue with what we have
        }

        dto.AccountStatus = ComputeAccountStatus(dto);

        return dto;
    }

    /// <summary>
    /// Maps collection of UserPrincipal to ADUserDto collection
    /// </summary>
    public static IEnumerable<ADUserDto> FromUserPrincipals(IEnumerable<UserPrincipal> users)
    {
        if (users == null)
            throw new ArgumentNullException(nameof(users));

        return users.Select(FromUserPrincipal);
    }

    #endregion

    #region Property Extraction Helpers (DirectoryEntry)

    private static string GetStringProperty(DirectoryEntry entry, string propertyName)
    {
        try
        {
            if (entry.Properties.Contains(propertyName) && entry.Properties[propertyName].Count > 0)
            {
                return entry.Properties[propertyName][0]?.ToString() ?? string.Empty;
            }
        }
        catch
        {
            // Ignore errors for missing properties
        }
        return string.Empty;
    }

    private static int? GetIntProperty(DirectoryEntry entry, string propertyName)
    {
        try
        {
            if (entry.Properties.Contains(propertyName) && entry.Properties[propertyName].Count > 0)
            {
                var value = entry.Properties[propertyName][0];
                if (value != null && int.TryParse(value.ToString(), out int result))
                {
                    return result;
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return null;
    }

    private static long? GetLongProperty(DirectoryEntry entry, string propertyName)
    {
        try
        {
            if (entry.Properties.Contains(propertyName) && entry.Properties[propertyName].Count > 0)
            {
                var value = entry.Properties[propertyName][0];
                if (value != null && long.TryParse(value.ToString(), out long result))
                {
                    return result;
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return null;
    }

    private static bool GetBoolProperty(DirectoryEntry entry, string propertyName)
    {
        try
        {
            if (entry.Properties.Contains(propertyName) && entry.Properties[propertyName].Count > 0)
            {
                var value = entry.Properties[propertyName][0];
                if (value != null)
                {
                    if (value is bool boolValue)
                        return boolValue;
                    
                    var stringValue = value.ToString()?.ToLower();
                    return stringValue == "true" || stringValue == "1";
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return false;
    }

    private static DateTime? GetDateTimeProperty(DirectoryEntry entry, string propertyName)
    {
        try
        {
            if (entry.Properties.Contains(propertyName) && entry.Properties[propertyName].Count > 0)
            {
                var value = entry.Properties[propertyName][0];
                
                if (value is DateTime dateTime)
                {
                    return dateTime;
                }
                
                // Handle FILETIME format
                if (value != null && long.TryParse(value.ToString(), out long fileTime))
                {
                    if (fileTime > 0 && fileTime != long.MaxValue)
                    {
                        try
                        {
                            return DateTime.FromFileTime(fileTime);
                        }
                        catch
                        {
                            // Invalid FILETIME
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return null;
    }

    private static Guid? GetGuidProperty(DirectoryEntry entry, string propertyName)
    {
        try
        {
            if (entry.Properties.Contains(propertyName) && entry.Properties[propertyName].Count > 0)
            {
                var value = entry.Properties[propertyName][0];
                
                if (value is byte[] bytes)
                {
                    return new Guid(bytes);
                }
                
                if (value is Guid guid)
                {
                    return guid;
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return null;
    }

    private static string GetSidProperty(DirectoryEntry entry, string propertyName)
    {
        try
        {
            if (entry.Properties.Contains(propertyName) && entry.Properties[propertyName].Count > 0)
            {
                var value = entry.Properties[propertyName][0];
                
                if (value is byte[] bytes)
                {
                    var sid = new System.Security.Principal.SecurityIdentifier(bytes, 0);
                    return sid.ToString();
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return string.Empty;
    }

    private static byte[]? GetByteArrayProperty(DirectoryEntry entry, string propertyName)
    {
        try
        {
            if (entry.Properties.Contains(propertyName) && entry.Properties[propertyName].Count > 0)
            {
                var value = entry.Properties[propertyName][0];
                
                if (value is byte[] bytes)
                {
                    return bytes;
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return null;
    }

    private static List<string> GetMultiValuedProperty(DirectoryEntry entry, string propertyName)
    {
        var result = new List<string>();
        
        try
        {
            if (entry.Properties.Contains(propertyName))
            {
                foreach (var value in entry.Properties[propertyName])
                {
                    if (value != null)
                    {
                        result.Add(value.ToString()!);
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        
        return result;
    }

    #endregion

    #region Property Extraction Helpers (SearchResult)

    private static string GetProperty(SearchResult result, string propertyName)
    {
        try
        {
            if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
            {
                return result.Properties[propertyName][0]?.ToString() ?? string.Empty;
            }
        }
        catch
        {
            // Ignore errors
        }
        return string.Empty;
    }

    private static int? GetInt32Property(SearchResult result, string propertyName)
    {
        try
        {
            if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
            {
                var value = result.Properties[propertyName][0];
                if (value != null && int.TryParse(value.ToString(), out int intValue))
                {
                    return intValue;
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return null;
    }

    private static long GetInt64Property(SearchResult result, string propertyName)
    {
        try
        {
            if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
            {
                var value = result.Properties[propertyName][0];
                if (value != null && long.TryParse(value.ToString(), out long longValue))
                {
                    return longValue;
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return 0;
    }

    private static DateTime? GetDateTimeProperty(SearchResult result, string propertyName)
    {
        try
        {
            if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
            {
                var value = result.Properties[propertyName][0];
                if (value is DateTime dateTime)
                {
                    return dateTime;
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return null;
    }

    private static Guid? GetGuidPropertyFromSearchResult(SearchResult result, string propertyName)
    {
        try
        {
            if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
            {
                var value = result.Properties[propertyName][0];
                if (value is byte[] bytes)
                {
                    return new Guid(bytes);
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return null;
    }

    private static string GetSidPropertyFromSearchResult(SearchResult result, string propertyName)
    {
        try
        {
            if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
            {
                var value = result.Properties[propertyName][0];
                if (value is byte[] bytes)
                {
                    var sid = new System.Security.Principal.SecurityIdentifier(bytes, 0);
                    return sid.ToString();
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return string.Empty;
    }

    private static List<string> GetMultiValueProperty(SearchResult result, string propertyName)
    {
        var values = new List<string>();
        try
        {
            if (result.Properties.Contains(propertyName))
            {
                foreach (var value in result.Properties[propertyName])
                {
                    if (value != null)
                    {
                        values.Add(value.ToString()!);
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return values;
    }

    private static DateTime? ConvertFileTimeToDateTime(long fileTime)
    {
        if (fileTime == 0 || fileTime == long.MaxValue || fileTime == 9223372036854775807)
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

    #region Account Status Helpers

    private static bool IsAccountDisabled(DirectoryEntry entry)
    {
        const int ADS_UF_ACCOUNTDISABLE = 0x0002;
        return HasFlag(entry, "userAccountControl", ADS_UF_ACCOUNTDISABLE);
    }

    private static bool IsAccountDisabledFromSearchResult(SearchResult result)
    {
        const int ADS_UF_ACCOUNTDISABLE = 0x0002;
        return HasFlagFromSearchResult(result, "userAccountControl", ADS_UF_ACCOUNTDISABLE);
    }

    private static bool IsAccountLockedOut(DirectoryEntry entry)
    {
        try
        {
            var lockoutTime = GetLongProperty(entry, "lockoutTime");
            return lockoutTime.HasValue && lockoutTime.Value > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPasswordExpired(DirectoryEntry entry)
    {
        const int ADS_UF_PASSWORD_EXPIRED = 0x800000;
        return HasFlag(entry, "userAccountControl", ADS_UF_PASSWORD_EXPIRED);
    }

    private static bool MustChangePasswordAtNextLogon(DirectoryEntry entry)
    {
        try
        {
            var pwdLastSet = GetLongProperty(entry, "pwdLastSet");
            return pwdLastSet == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasPasswordNeverExpires(DirectoryEntry entry)
    {
        const int ADS_UF_DONT_EXPIRE_PASSWD = 0x10000;
        return HasFlag(entry, "userAccountControl", ADS_UF_DONT_EXPIRE_PASSWD);
    }

    private static bool HasCannotChangePassword(DirectoryEntry entry)
    {
        const int ADS_UF_PASSWD_CANT_CHANGE = 0x0040;
        return HasFlag(entry, "userAccountControl", ADS_UF_PASSWD_CANT_CHANGE);
    }

    private static bool HasFlag(DirectoryEntry entry, string propertyName, int flag)
    {
        try
        {
            var value = GetIntProperty(entry, propertyName);
            return value.HasValue && (value.Value & flag) == flag;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasFlagFromSearchResult(SearchResult result, string propertyName, int flag)
    {
        try
        {
            var value = GetInt32Property(result, propertyName);
            return value.HasValue && (value.Value & flag) == flag;
        }
        catch
        {
            return false;
        }
    }

    private static DateTime? GetAccountExpirationDate(DirectoryEntry entry)
    {
        try
        {
            var accountExpires = GetLongProperty(entry, "accountExpires");
            
            if (!accountExpires.HasValue || accountExpires.Value == 0 || 
                accountExpires.Value == long.MaxValue || accountExpires.Value == 9223372036854775807)
            {
                return null;
            }

            return DateTime.FromFileTime(accountExpires.Value);
        }
        catch
        {
            return null;
        }
    }

    private static string ComputeAccountStatus(ADUserDto dto)
    {
        if (dto.IsLocked)
            return "Locked";
        if (!dto.IsEnabled)
            return "Disabled";
        if (dto.IsAccountExpired)
            return "Expired";
        if (dto.PasswordExpired)
            return "Password Expired";
        if (dto.MustChangePassword)
            return "Must Change Password";
        
        return "Active";
    }

    #endregion

    #region Properties to Load (for optimization)

    /// <summary>
    /// Gets the list of all properties that should be loaded from AD
    /// Use this with DirectorySearcher.PropertiesToLoad for better performance
    /// </summary>
    public static string[] GetAllPropertiesToLoad()
    {
        return new[]
        {
            // Core Identity
            "sAMAccountName", "userPrincipalName", "displayName", "distinguishedName",
            "canonicalName", "objectGUID", "objectSid",
            
            // Personal
            "givenName", "sn", "middleName", "initials", "cn", "name", "description",
            "employeeID", "employeeNumber", "employeeType",
            
            // Contact
            "mail", "proxyAddresses", "telephoneNumber", "homePhone", "mobile", "pager",
            "facsimileTelephoneNumber", "ipPhone", "wWWHomePage", "url",
            
            // Organization
            "department", "title", "company", "manager", "directReports", "division",
            "physicalDeliveryOfficeName",
            
            // Address
            "streetAddress", "postOfficeBox", "l", "st", "postalCode", "c", "co",
            
            // Account Status
            "userAccountControl", "lockoutTime",
            
            // Timestamps
            "whenCreated", "whenChanged", "lastLogonTimestamp", "lastLogon", "lastLogoff",
            "badPasswordTime", "pwdLastSet", "accountExpires",
            
            // Logon
            "logonCount", "badPwdCount", "logonHours", "userWorkstations", "scriptPath",
            "profilePath", "homeDirectory", "homeDrive",
            
            // Groups
            "primaryGroupID", "memberOf",
            
            // Exchange
            "mailNickname", "targetAddress", "legacyExchangeDN", "msExchVersion",
            "msExchHideFromAddressLists",
            
            // Custom
            "extensionAttribute1", "extensionAttribute2", "extensionAttribute3",
            "extensionAttribute4", "extensionAttribute5", "extensionAttribute6",
            "extensionAttribute7", "extensionAttribute8", "extensionAttribute9",
            "extensionAttribute10", "extensionAttribute11", "extensionAttribute12",
            "extensionAttribute13", "extensionAttribute14", "extensionAttribute15"
        };
    }

    /// <summary>
    /// Gets a minimal set of properties for list views
    /// </summary>
    public static string[] GetMinimalPropertiesToLoad()
    {
        return new[]
        {
            "sAMAccountName", "displayName", "mail", "title", "department",
            "userAccountControl", "lastLogonTimestamp", "distinguishedName"
        };
    }

    #endregion
}
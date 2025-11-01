namespace ADManagement.Application.Configuration;

/// <summary>
/// Configuration for Active Directory connection
/// ✨ OPTIMIZED with new performance settings
/// </summary>
public class ADConfiguration
{
    // ✅ Add this constant
    public const string SectionName = "ADConfiguration";
    public string Domain { get; set; } = string.Empty;
    public string? LdapServer { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Port { get; set; } = 389;
    public bool UseSSL { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int PageSize { get; set; } = 1000;
    public string? DefaultSearchOU { get; set; }

    // ✨ NEW: Performance optimization settings
    public int ConnectionPoolSize { get; set; } = 10;
    public int MaxConcurrentOperations { get; set; } = 10;
    public int SearchCacheSeconds { get; set; } = 300; // 5 minutes
    public int BatchSize { get; set; } = 100;
    public int MaxParallelDegree { get; set; } = 4;

    public string LdapPath => UseSSL
        ? $"LDAPS://{LdapServer ?? Domain}:{Port}"
        : $"LDAP://{LdapServer ?? Domain}:{Port}";
    // ✅ Add this validation method
    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Domain))
            errors.Add("Domain is required");

        //if (string.IsNullOrWhiteSpace(Username))
        //    errors.Add("Username is required");

        //if (string.IsNullOrWhiteSpace(Password))
        //    errors.Add("Password is required");

        if (Port <= 0 || Port > 65535)
            errors.Add("Port must be between 1 and 65535");

        if (PageSize <= 0)
            errors.Add("PageSize must be greater than 0");

        if (TimeoutSeconds <= 0)
            errors.Add("ConnectionTimeout must be greater than 0");

        if (ConnectionPoolSize <= 0)
            errors.Add("MaxPoolSize must be greater than 0");

        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"ADConfiguration validation failed: {string.Join(", ", errors)}");
        }
    }
}
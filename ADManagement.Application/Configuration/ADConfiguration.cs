namespace ADManagement.Application.Configuration;

/// <summary>
/// Configuration for Active Directory connection
/// ✨ OPTIMIZED with new performance settings
/// </summary>
public class ADConfiguration
{
    public string Domain { get; set; } = string.Empty;
    public string? LdapServer { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
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
}
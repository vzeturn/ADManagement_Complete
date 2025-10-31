namespace ADManagement.Application.Configuration;

/// <summary>
/// Configuration for Active Directory connection
/// </summary>
public class ADConfiguration
{
    public const string SectionName = "ADConfiguration";
    
    /// <summary>
    /// Domain name (e.g., corp.contoso.com)
    /// </summary>
    public string Domain { get; set; } = string.Empty;
    
    /// <summary>
    /// Username for AD authentication (optional, uses current user if empty)
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Password for AD authentication (optional, uses current user if empty)
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// LDAP server address (optional, auto-detected if empty)
    /// </summary>
    public string LdapServer { get; set; } = string.Empty;
    
    /// <summary>
    /// LDAP port (389 for LDAP, 636 for LDAPS)
    /// </summary>
    public int Port { get; set; } = 389;
    
    /// <summary>
    /// Use SSL/TLS for LDAP connection
    /// </summary>
    public bool UseSSL { get; set; } = false;
    
    /// <summary>
    /// Page size for large queries (1-5000)
    /// </summary>
    public int PageSize { get; set; } = 1000;
    
    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Default OU path for searches (optional)
    /// </summary>
    public string DefaultSearchOU { get; set; } = string.Empty;

    /// <summary>
    /// Cache duration in seconds for search results (0 = disabled)
    /// </summary>
    public int SearchCacheSeconds { get; set; } = 30;
    
    /// <summary>
    /// Gets the LDAP path
    /// </summary>
    public string LdapPath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(LdapServer))
            {
                var protocol = UseSSL ? "LDAPS" : "LDAP";
                return $"{protocol}://{LdapServer}:{Port}";
            }
            return $"LDAP://{Domain}";
        }
    }
    
    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Domain))
        {
            throw new InvalidOperationException("Domain is required in ADConfiguration");
        }
        
        if (Port < 1 || Port > 65535)
        {
            throw new InvalidOperationException("Port must be between 1 and 65535");
        }
        
        if (PageSize < 1 || PageSize > 5000)
        {
            throw new InvalidOperationException("PageSize must be between 1 and 5000");
        }
        
        if (TimeoutSeconds < 1)
        {
            throw new InvalidOperationException("TimeoutSeconds must be greater than 0");
        }
        
        // If username is provided, password should also be provided
        if (!string.IsNullOrWhiteSpace(Username) && string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException("Password is required when Username is provided");
        }
    }
}
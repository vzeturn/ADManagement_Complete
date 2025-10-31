using System.Net;
using ADManagement.Application.Configuration;
using ADManagement.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ADManagement.Infrastructure.Services;

public class CredentialProvider : ICredentialProvider
{
    private readonly ADConfiguration _config;
    private readonly ILogger<CredentialProvider> _logger;
    private readonly ICredentialService _credentialService;
    private NetworkCredential? _cachedCredential;

    public bool HasCredentials => _cachedCredential != null || (_config.Username?.Length > 0 && _config.Password?.Length > 0);

    public CredentialProvider(
        ADConfiguration config,
        ICredentialService credentialService,
        ILogger<CredentialProvider> logger)
    {
        _config = config;
        _credentialService = credentialService;
        _logger = logger;

        // Try load saved credentials on startup
        LoadSavedCredentials();
    }

    private void LoadSavedCredentials()
    {
        try
        {
            if (_credentialService.TryLoadCredentials(out var username, out var password))
            {
                _config.Username = username;
                _config.Password = password;
                _cachedCredential = new NetworkCredential(username, password);
                _logger.LogInformation("Loaded saved credentials for user: {Username}", username);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load saved credentials");
        }
    }

    public NetworkCredential? GetNetworkCredential()
    {
        if (_cachedCredential != null)
            return _cachedCredential;

        if (!string.IsNullOrEmpty(_config.Username) && !string.IsNullOrEmpty(_config.Password))
        {
            _cachedCredential = new NetworkCredential(_config.Username, _config.Password);
            return _cachedCredential;
        }

        return null;
    }

    public void SetCredentials(string username, string password)
    {
        _config.Username = username;
        _config.Password = password;
        _cachedCredential = new NetworkCredential(username, password);

        try
        {
            _credentialService.SaveCredentials(username, password);
            _logger.LogInformation("Saved credentials for user: {Username}", username);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save credentials");
        }
    }

    public void ClearCredentials()
    {
        _config.Username = string.Empty;
        _config.Password = string.Empty;
        _cachedCredential = null;

        try
        {
            _credentialService.DeleteCredentials();
            _logger.LogInformation("Cleared saved credentials");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear saved credentials");
        }
    }
}
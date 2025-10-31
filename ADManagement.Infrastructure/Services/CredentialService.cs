using ADManagement.Application.Interfaces;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace ADManagement.Infrastructure.Services;

/// <summary>
/// Secure credential storage implementation using DPAPI
/// </summary>
public class CredentialService : ICredentialService
{
    private readonly string _filePath;

    public CredentialService()
    {
        var appDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appDir, "ADManagement", "credentials");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "creds.dat");
    }

    public void SaveCredentials(string username, string password)
    {
        var plain = username + '\n' + password;
        var bytes = Encoding.UTF8.GetBytes(plain);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_filePath, encrypted);
    }

    public bool TryLoadCredentials(out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;

        try
        {
            if (!File.Exists(_filePath)) return false;
            
            var encrypted = File.ReadAllBytes(_filePath);
            var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var plain = Encoding.UTF8.GetString(bytes);
            var parts = plain.Split('\n');
            
            if (parts.Length >= 2)
            {
                username = parts[0];
                password = parts[1];
                return true;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    public void DeleteCredentials()
    {
        try
        {
            if (File.Exists(_filePath)) File.Delete(_filePath);
        }
        catch
        {
            // Ignore errors on delete
        }
    }
}
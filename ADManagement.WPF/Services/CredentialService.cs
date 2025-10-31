using System;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace ADManagement.WPF.Services;

/// <summary>
/// Simple credential storage per-user using DPAPI (ProtectedData) and Windows user profile directory.
/// Not suitable for high-security environments; recommend using Windows Credential Manager or Azure Key Vault in production.
/// </summary>
public class CredentialService : ICredentialService
{
    private readonly string _filePath;

    public CredentialService()
    {
        var appDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = System.IO.Path.Combine(appDir, "ADManagement", "credentials");
        System.IO.Directory.CreateDirectory(dir);
        _filePath = System.IO.Path.Combine(dir, "creds.dat");
    }

    public void SaveCredentials(string username, string password)
    {
        var plain = username + '\n' + password;
        var bytes = Encoding.UTF8.GetBytes(plain);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        System.IO.File.WriteAllBytes(_filePath, encrypted);
    }

    public bool TryLoadCredentials(out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;

        try
        {
            if (!System.IO.File.Exists(_filePath)) return false;
            var encrypted = System.IO.File.ReadAllBytes(_filePath);
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
            if (System.IO.File.Exists(_filePath)) System.IO.File.Delete(_filePath);
        }
        catch
        {
        }
    }
}
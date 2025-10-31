namespace ADManagement.Application.Interfaces;

public interface ICredentialService
{
    void SaveCredentials(string username, string password);
    bool TryLoadCredentials(out string username, out string password);
    void DeleteCredentials();
}
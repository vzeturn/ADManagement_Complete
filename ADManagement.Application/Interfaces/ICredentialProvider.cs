using System.Net;

namespace ADManagement.Application.Interfaces
{
    public interface ICredentialProvider
    {
        bool HasCredentials { get; }
        NetworkCredential? GetNetworkCredential();
        void SetCredentials(string username, string password);
        void ClearCredentials();
    }
}
namespace ADManagement.Application.Interfaces;

public interface IAuditService
{
    Task LogPasswordResetAsync(string username, string performedBy, bool success, string details = "");
}
using ADManagement.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ADManagement.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;

    public AuditService(ILogger<AuditService> logger)
    {
        _logger = logger;
    }

    public Task LogPasswordResetAsync(string username, string performedBy, bool success, string details = "")
    {
        // Simple audit: write structured log entry. In production, this could write to audit DB or SIEM.
        _logger.LogInformation("AUDIT PasswordReset | User={User} | By={By} | Success={Success} | Details={Details}", username, performedBy, success, details);
        return Task.CompletedTask;
    }
}
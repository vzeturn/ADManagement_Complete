using ADManagement.Application.Interfaces;
using ADManagement.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ADManagement.Application;

/// <summary>
/// Dependency Injection configuration for Application layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register Application Services
        services.AddScoped<IADUserService, ADUserService>();
        services.AddScoped<IExportService, ExportService>();
        // ⭐ Add this line
        services.AddScoped<IADGroupService, ADGroupService>();
        return services;
    }
}
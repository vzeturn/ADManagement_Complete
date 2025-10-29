using ADManagement.Application.Configuration;
using ADManagement.Domain.Interfaces;
using ADManagement.Infrastructure.Exporters;
using ADManagement.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ADManagement.Infrastructure;

/// <summary>
/// Dependency Injection configuration for Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configurations
        var adConfig = configuration.GetSection(ADConfiguration.SectionName).Get<ADConfiguration>() 
            ?? new ADConfiguration();
        adConfig.Validate();
        services.AddSingleton(adConfig);
        
        var exportConfig = configuration.GetSection(ExportConfiguration.SectionName).Get<ExportConfiguration>() 
            ?? new ExportConfiguration();
        services.AddSingleton(exportConfig);
        
        // Register repositories
        services.AddScoped<IADRepository, ADRepository>();
        
        // Register exporters
        services.AddScoped<IExcelExporter, ExcelExporter>();
        
        return services;
    }
}

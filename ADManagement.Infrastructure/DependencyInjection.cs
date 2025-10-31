using ADManagement.Application.Configuration;
using ADManagement.Domain.Interfaces;
using ADManagement.Infrastructure.Exporters;
using ADManagement.Infrastructure.Repositories;
using ADManagement.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        // Register AD Configuration with validation
        try
        {
            var adConfig = configuration
                .GetSection(ADConfiguration.SectionName)
                .Get<ADConfiguration>();

            if (adConfig == null)
            {
                throw new InvalidOperationException(
                    $"Configuration section '{ADConfiguration.SectionName}' not found in appsettings.json");
            }

            // Validate configuration
            adConfig.Validate();
            
            services.AddSingleton(adConfig);
        }
        catch (InvalidOperationException ex)
        {
            // Log and rethrow with more context
            throw new InvalidOperationException(
                "Failed to configure Active Directory settings. " +
                "Please ensure 'ADConfiguration' section exists in appsettings.json and contains valid values. " +
                $"Error: {ex.Message}", 
                ex);
        }

        // Register Export Configuration
        var exportConfig = configuration
            .GetSection(ExportConfiguration.SectionName)
            .Get<ExportConfiguration>() ?? new ExportConfiguration();
        
        services.AddSingleton(exportConfig);

        // Register repositories
        services.AddScoped<IADRepository, ADRepository>();

        // Register exporters
        services.AddScoped<IExcelExporter, ExcelExporter>();

        // Register infrastructure helper services
        services.AddScoped<ADConnectionDiagnosticsService>();

        return services;
    }

    /// <summary>
    /// Validates that all required infrastructure services are registered
    /// </summary>
    public static void ValidateInfrastructureServices(this IServiceProvider serviceProvider)
    {
        try
        {
            // Validate AD Configuration
            var adConfig = serviceProvider.GetRequiredService<ADConfiguration>();
            var logger = serviceProvider.GetService<ILogger<ADConfiguration>>();
            
            logger?.LogInformation("AD Configuration loaded: Domain={Domain}, SSL={UseSSL}, Port={Port}",
                adConfig.Domain, adConfig.UseSSL, adConfig.Port);

            // Validate Export Configuration
            var exportConfig = serviceProvider.GetRequiredService<ExportConfiguration>();
            logger?.LogInformation("Export Configuration loaded: OutputDirectory={Directory}",
                exportConfig.OutputDirectory);

            // Ensure output directory exists
            if (!string.IsNullOrWhiteSpace(exportConfig.OutputDirectory))
            {
                Directory.CreateDirectory(exportConfig.OutputDirectory);
            }

            // Validate repositories can be created
            _ = serviceProvider.GetRequiredService<IADRepository>();
            _ = serviceProvider.GetRequiredService<IExcelExporter>();

            logger?.LogInformation("All infrastructure services validated successfully");
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetService<ILogger<ADConfiguration>>();
            logger?.LogError(ex, "Infrastructure services validation failed");
            throw;
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using RedisCachePatterns.Configuration;
using Microsoft.Extensions.Configuration;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Shows how to register the library in an ASP.NET Core application.
/// </summary>
public class IntegrationExample
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register Redis cache services with custom configuration
        services.AddRedisCacheServices(
            configuration.GetConnectionString("Redis")!,
            options =>
            {
                options.DefaultExpirationSeconds = 3600; // 1 hour
                options.EnableCompression = true;        // Enable compression for efficiency
            });
    }
}

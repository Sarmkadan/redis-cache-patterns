#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Monitoring;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Redis connection
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
    ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION")
    ?? "localhost:6379";

builder.Services.AddSingleton<IRedisConnection>(sp =>
    new Infrastructure.Cache.RedisConnection(
        redisConnectionString,
        sp.GetRequiredService<ILogger<Infrastructure.Cache.RedisConnection>>()));

// Cache service
builder.Services.AddSingleton<ICacheService, Infrastructure.Cache.RedisCacheService>();

// Health check service
builder.Services.AddSingleton<HealthCheckService>();

// Configure the app
var app = builder.Build();

// Health endpoint for Docker health checks
app.MapGet("/health", async (HealthCheckService healthCheck) =>
{
    var status = await healthCheck.CheckHealthAsync();
    return Results.Ok(new {
        Status = status.Overall,
        RedisConnected = status.RedisConnected,
        CheckedAt = status.CheckedAt
    });
});

// Cache statistics endpoint
app.MapGet("/cache/stats", async (ICacheService cacheService) =>
{
    var stats = await cacheService.GetStatisticsAsync();
    return Results.Ok(stats);
});

// Run the application
app.Run();
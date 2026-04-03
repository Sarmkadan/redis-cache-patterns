#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Integration;

/// <summary>
/// Factory for creating configured HTTP clients with retry policies and logging
/// Provides centralized configuration for external API communication
/// </summary>
public class HttpClientFactory
{
    private readonly ILogger<HttpClientFactory> _logger;
    private readonly Dictionary<string, HttpClientConfiguration> _configurations = new();
    private readonly Dictionary<string, HttpClient> _clients = new();

    public HttpClientFactory(ILogger<HttpClientFactory> logger)
    {
        _logger = logger;
    }

    public HttpClientFactory RegisterClient(string name, HttpClientConfiguration config)
    {
        _configurations[name] = config;
        _logger.LogDebug("HTTP client registered: {ClientName}", name);
        return this;
    }

    public HttpClient GetClient(string name)
    {
        if (_clients.TryGetValue(name, out var client))
            return client;

        if (!_configurations.TryGetValue(name, out var config))
            throw new InvalidOperationException($"HTTP client configuration not found: {name}");

        var newClient = CreateClient(config);
        _clients[name] = newClient;
        return newClient;
    }

    private HttpClient CreateClient(HttpClientConfiguration config)
    {
        var client = new HttpClient();
        client.Timeout = config.Timeout;
        client.BaseAddress = config.BaseAddress;

        if (config.DefaultHeaders != null)
        {
            foreach (var (key, value) in config.DefaultHeaders)
            {
                client.DefaultRequestHeaders.Add(key, value);
            }
        }

        if (!string.IsNullOrEmpty(config.AuthToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", config.AuthToken);
        }

        _logger.LogInformation("HTTP client created with base address: {BaseAddress}", config.BaseAddress);
        return client;
    }

    public void Dispose()
    {
        foreach (var client in _clients.Values)
        {
            client?.Dispose();
        }
        _clients.Clear();
    }
}

/// <summary>
/// Configuration for HTTP client
/// </summary>
public class HttpClientConfiguration
{
    public Uri? BaseAddress { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, string>? DefaultHeaders { get; set; }
    public string? AuthToken { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

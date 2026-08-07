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
        if (logger == null)
            throw new ArgumentNullException(nameof(logger));
        _logger = logger;
    }

    public HttpClientFactory RegisterClient(string name, HttpClientConfiguration config)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Client name is null or empty.", nameof(name));
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        _logger.LogInformation("Registering HTTP client: {ClientName}", name);
        _configurations[name] = config;
        _logger.LogDebug("HTTP client registered: {ClientName}", name);
        return this;
    }

    public HttpClient GetClient(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Client name is null or empty.", nameof(name));
        _logger.LogInformation("Retrieving HTTP client: {ClientName}", name);

        if (_clients.TryGetValue(name, out var client))
        {
            _logger.LogInformation("Returning cached HTTP client: {ClientName}", name);
            return client;
        }

        if (!_configurations.TryGetValue(name, out var config))
            throw new InvalidOperationException($"HTTP client configuration not found: {name}");

        var newClient = CreateClient(config);
        _clients[name] = newClient;
        return newClient;
    }

    private HttpClient CreateClient(HttpClientConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        var client = new HttpClient();
        client.Timeout = config.Timeout;
        client.BaseAddress = config.BaseAddress;

        if (config.DefaultHeaders != null)
        {
            foreach (var (key, value) in config.DefaultHeaders)
            {
                if (string.IsNullOrEmpty(key))
                    throw new ArgumentException("Header key is null or empty.", nameof(key));
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
        _logger.LogInformation("Disposing HttpClientFactory and cleaning up {ClientCount} clients.", _clients.Count);
        foreach (var client in _clients.Values)
        {
            client?.Dispose();
        }
        _clients.Clear();
        _logger.LogInformation("HttpClientFactory disposed.");
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

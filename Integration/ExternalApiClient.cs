// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.Integration;

/// <summary>
/// Generic HTTP API client with error handling and retry logic
/// Used for communicating with external REST APIs
/// </summary>
public class ExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiClient> _logger;
    private readonly int _maxRetries;
    private readonly int _retryDelayMs;

    public ExternalApiClient(HttpClient httpClient, ILogger<ExternalApiClient> logger, int maxRetries = 3, int retryDelayMs = 1000)
    {
        _httpClient = httpClient;
        _logger = logger;
        _maxRetries = maxRetries;
        _retryDelayMs = retryDelayMs;
    }

    public async Task<T?> GetAsync<T>(string endpoint) where T : class
    {
        try
        {
            var response = await ExecuteWithRetryAsync(() => _httpClient.GetAsync(endpoint));
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonHelper.DeserializeSafe<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to GET from endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data) where T : class
    {
        try
        {
            var json = JsonHelper.Serialize(data);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await ExecuteWithRetryAsync(() => _httpClient.PostAsync(endpoint, content));
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonHelper.DeserializeSafe<T>(responseJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to POST to endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data) where T : class
    {
        try
        {
            var json = JsonHelper.Serialize(data);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await ExecuteWithRetryAsync(() => _httpClient.PutAsync(endpoint, content));
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonHelper.DeserializeSafe<T>(responseJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to PUT to endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            var response = await ExecuteWithRetryAsync(() => _httpClient.DeleteAsync(endpoint));
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to DELETE endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> operation)
    {
        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries - 1)
            {
                _logger.LogWarning("API call attempt {Attempt} failed, retrying in {DelayMs}ms: {Error}",
                    attempt + 1, _retryDelayMs, ex.Message);
                await Task.Delay(_retryDelayMs * (attempt + 1)); // Exponential backoff
            }
        }

        throw new InvalidOperationException($"API call failed after {_maxRetries} attempts");
    }
}

#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Integration;

/// <summary>
/// Handles incoming webhooks with signature verification and validation
/// Ensures webhook authenticity using HMAC signatures
/// </summary>
public class WebhookHandler
{
    private readonly ILogger<WebhookHandler> _logger;
    private readonly Dictionary<string, WebhookConfiguration> _configurations = new();
    private readonly List<WebhookEvent> _processedEvents = new();

    public WebhookHandler(ILogger<WebhookHandler> logger)
    {
        _logger = logger;
    }

    public WebhookHandler RegisterEndpoint(string name, WebhookConfiguration config)
    {
        _configurations[name] = config;
        _logger.LogInformation("Webhook endpoint registered: {Name}", name);
        return this;
    }

    public bool VerifySignature(string endpointName, string payload, string signature)
    {
        if (!_configurations.TryGetValue(endpointName, out var config))
        {
            _logger.LogWarning("Unknown webhook endpoint: {Endpoint}", endpointName);
            return false;
        }

        try
        {
            var expectedSignature = ComputeSignature(payload, config.Secret);
            var isValid = expectedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogWarning("Webhook signature verification failed for endpoint: {Endpoint}", endpointName);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook signature");
            return false;
        }
    }

    public async Task<bool> HandleWebhookAsync(string endpointName, string payload, WebhookEventHandler handler)
    {
        try
        {
            if (!_configurations.TryGetValue(endpointName, out var config))
            {
                _logger.LogError("Unknown webhook endpoint: {Endpoint}", endpointName);
                return false;
            }

            var @event = new WebhookEvent
            {
                Id = Guid.NewGuid().ToString(),
                Endpoint = endpointName,
                Payload = payload,
                ReceivedAt = DateTime.UtcNow
            };

            _processedEvents.Add(@event);
            await handler(@event).ConfigureAwait(false);

            _logger.LogInformation("Webhook processed successfully: {Endpoint} | EventId: {EventId}",
                endpointName, @event.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook from endpoint: {Endpoint}", endpointName);
            return false;
        }
    }

    public IEnumerable<WebhookEvent> GetProcessedEvents() => _processedEvents.AsReadOnly();

    private string ComputeSignature(string payload, string secret)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash).ToLower();
        }
    }
}

/// <summary>
/// Webhook configuration containing endpoint details and authentication
/// </summary>
public class WebhookConfiguration
{
    public string EndpointUrl { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string? Authentication { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Represents a received webhook event
/// </summary>
public class WebhookEvent
{
    public string Id { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
}

/// <summary>
/// Handler delegate for webhook events
/// </summary>
public delegate Task WebhookEventHandler(WebhookEvent @event);

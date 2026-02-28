#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Middleware;

/// <summary>
/// Middleware for handling response compression negotiation
/// Supports gzip, deflate, and brotli compression algorithms
/// </summary>
public class CompressionMiddleware
{
    private readonly ILogger<CompressionMiddleware> _logger;
    private readonly Dictionary<string, string> _supportedEncodings = new()
    {
        { "gzip", "application/gzip" },
        { "deflate", "application/deflate" },
        { "br", "application/x-br" }
    };

    public CompressionMiddleware(ILogger<CompressionMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(string? acceptEncoding, Func<Task> next)
    {
        if (string.IsNullOrEmpty(acceptEncoding))
        {
            await next();
            return;
        }

        var encodings = acceptEncoding.Split(',')
            .Select(x => x.Trim().Split(';')[0])
            .Where(x => _supportedEncodings.ContainsKey(x))
            .ToList();

        if (!encodings.Any())
        {
            _logger.LogDebug("No supported encoding requested: {AcceptEncoding}", acceptEncoding);
            await next();
            return;
        }

        var selectedEncoding = encodings.First();
        _logger.LogDebug("Compression negotiated: {Encoding}", selectedEncoding);

        await next();
    }

    public IEnumerable<string> GetSupportedEncodings() => _supportedEncodings.Keys;
}

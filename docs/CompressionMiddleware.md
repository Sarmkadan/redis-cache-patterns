# Compression Middleware

Documentation for the `CompressionMiddleware` class in the `RedisCachePatterns.Middleware` namespace, which inspects an `Accept-Encoding` value and negotiates a supported response encoding.

## Class: `CompressionMiddleware`

Recognizes gzip, deflate, and Brotli encoding tokens, logs the first supported encoding requested by the caller, and then invokes the next operation. The class performs negotiation only; it does not compress response content or set response headers.

### Constructor

```csharp
public CompressionMiddleware(ILogger<CompressionMiddleware> logger)
```

| Parameter | Description |
|-----------|-------------|
| `logger` | Logger used to record compression negotiation details at Debug level. |

### Methods

#### `InvokeAsync`

```csharp
public Task InvokeAsync(string? acceptEncoding, Func<Task> next)
```

Examines a comma-separated `Accept-Encoding` value, selects the first recognized encoding, and invokes the supplied continuation.

**Parameters:**
- `acceptEncoding`: The encoding preferences to inspect, or `null`. Parameters following a semicolon on each entry are ignored during matching.
- `next`: The asynchronous operation to invoke after negotiation.

**Returns:** A `Task` that completes when `next` completes.

**Behavior:**
1. If `acceptEncoding` is `null` or empty, invokes `next` immediately.
2. Splits the value on commas, trims each entry, and removes any portion beginning with a semicolon.
3. Retains entries that exactly match a supported encoding token.
4. If no supported token remains, writes a Debug log entry and invokes `next`.
5. Otherwise, selects the first supported token, writes a Debug log entry naming it, and invokes `next`.

Matching is case-sensitive. Quality values such as `q=0` are discarded rather than evaluated, so they do not affect selection.

#### `GetSupportedEncodings`

```csharp
public IEnumerable<string> GetSupportedEncodings()
```

Returns the supported encoding tokens in negotiation order:

- `gzip`
- `deflate`
- `br`

The returned sequence exposes the keys of the middleware's internal encoding dictionary.

## Usage Example

```csharp
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Middleware;

ILogger<CompressionMiddleware> logger = loggerFactory.CreateLogger<CompressionMiddleware>();
var middleware = new CompressionMiddleware(logger);

await middleware.InvokeAsync("br, gzip;q=0.8", async () =>
{
    await WriteResponseAsync();
});
```

In this example, `br` is the first supported token in the supplied value, so the middleware logs that it was negotiated before invoking `WriteResponseAsync`.

## Notes

- The continuation is invoked exactly once for every normal input path, including when the header value is absent or contains no supported encoding.
- Exceptions thrown by `next` are not caught and propagate to the caller.
- The constructor and `InvokeAsync` do not explicitly validate their arguments. Passing a null logger can cause a `NullReferenceException` when logging is required, and passing a null continuation causes a `NullReferenceException` when it is invoked.
- The middleware recognizes encoding tokens but does not use the associated media-type values, transform a response body, or add a `Content-Encoding` header.

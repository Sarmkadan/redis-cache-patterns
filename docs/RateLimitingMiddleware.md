# RateLimitingMiddleware

Middleware that enforces a sliding‑window rate limit using Redis as a backing store. Each incoming request is checked against a configurable maximum number of calls within a defined time window; excess requests receive a 429 (Too Many Requests) response.

## API

### RateLimitingMiddleware()
Initializes a new instance of the middleware. The instance starts with the default rate‑limit policy (`RateLimitPolicy.Default`). After construction, the `MaxRequests` and `WindowSeconds` properties can be adjusted to customize the limit for the current middleware instance.

### public async Task InvokeAsync(HttpContext context, RequestDelegate next)
Processes an HTTP request.

* **Parameters**
  * `context` – The `HttpContext` for the current request.
  * `next` – The delegate representing the remaining middleware pipeline.
* **Return value** – A `Task` that completes when the request has been processed.
* **Exceptions**
  * Throws `InvalidOperationException` if the middleware is unable to connect to the Redis instance used for storing timestamps.
  * May propagate any exception thrown by downstream middleware (`next`) or by Redis access code.

### public List<DateTime> Timestamps
Gets the list of request timestamps recorded for the current client (typically identified by IP address or API key) within the sliding window. The list is maintained in chronological order and is used internally to compute the request count.

* **Purpose** – Inspect or debug the current window’s request history.
* **Thread safety** – The list is not synchronized; concurrent accesses from multiple requests handling the same client must be externally synchronized.

### public int MaxRequests
Gets or sets the maximum number of requests allowed within the `WindowSeconds` period.

* **Valid values** – Positive integers; setting a value ≤ 0 results in all requests being rejected.
* **Default** – Derived from the policy used at construction (`RateLimitPolicy.Default.MaxRequests`).

### public int WindowSeconds
Gets or sets the size of the sliding window, in seconds.

* **Valid values** – Positive integers; a value of zero disables the window (no limit).
* **Default** – Derived from the policy used at construction (`RateLimitPolicy.Default.WindowSeconds`).

### public static RateLimitPolicy Default
A predefined policy providing a moderate limit suitable for general‑purpose APIs.

* **Fields** – `MaxRequests = 100`, `WindowSeconds = 60`.

### public static RateLimitPolicy Strict
A predefined policy enforcing a low request threshold, appropriate for sensitive endpoints.

* **Fields** – `MaxRequests = 10`, `WindowSeconds = 60`.

### public static RateLimitPolicy Lenient
A predefined policy allowing a high request volume, useful for public assets or health‑check endpoints.

* **Fields** – `MaxRequests = 1000`, `WindowSeconds = 60`.

## Usage

### Basic registration with the default policy
```csharp
using Microsoft.AspNetCore.Builder;
using RedisCachePatterns.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add the middleware to the pipeline.
builder.UseMiddleware<RateLimitingMiddleware>();

var app = builder.Build();
app.MapGet("/", () => "Hello World");
app.Run();
```
In this example each client is limited to 100 requests per minute (the values from `RateLimitPolicy.Default`).

### Customizing limits per‑endpoint
```csharp
using Microsoft.AspNetCore.Builder;
using RedisCachePatterns.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Apply a strict policy to the /api/ endpoint.
builder.UseWhen(context => context.Request.Path.StartsWithSegments("/api"),
    appBuilder => appBuilder.UseMiddleware<RateLimitingMiddleware>(options =>
    {
        options.MaxRequests = RateLimitPolicy.Strict.MaxRequests;
        options.WindowSeconds = RateLimitPolicy.Strict.WindowSeconds;
    }));

// Use a lenient policy for static files.
builder.UseWhen(context => context.Request.Path.StartsWithSegments("/static"),
    appBuilder => appBuilder.UseMiddleware<RateLimitingMiddleware>(options =>
    {
        options.MaxRequests = RateLimitPolicy.Lenient.MaxRequests;
        options.WindowSeconds = RateLimitPolicy.Lenient.WindowSeconds;
    }));

var app = builder.Build();
app.UseStaticFiles();
app.MapControllers();
app.Run();
```
Here the middleware is instantiated twice with different configurations: a strict limit for API routes and a lenient limit for static assets.

## Notes
* The `Timestamps` list is accessed and modified on every request. Because the middleware is typically registered as a singleton, concurrent requests for the same client can modify the list simultaneously, leading to race conditions. Implement external synchronization (e.g., a `lock` or use a thread‑safe collection) if direct inspection of `Timestamps` is required from multiple threads.
* The static `RateLimitPolicy` instances (`Default`, `Strict`, `Lenient`) are immutable after initialization; altering their fields has no effect on existing middleware instances.
* If the Redis store becomes unavailable, the middleware throws an `InvalidOperationException` from `InvokeAsync`. Applications should catch this exception at an appropriate level (e.g., using exception handling middleware) to avoid crashing the request pipeline.
* Setting `WindowSeconds` to zero disables the sliding‑window check, causing `MaxRequests` to be ignored; all requests will pass through unchanged. Conversely, setting `MaxRequests` to zero blocks all requests regardless of the window.

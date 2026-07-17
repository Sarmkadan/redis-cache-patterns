// ... (rest of the file remains the same)

## RateLimitingMiddleware

The `RateLimitingMiddleware` class provides rate limiting capabilities to prevent abuse and ensure fair usage of API endpoints. It uses a sliding window algorithm to track and limit the number of requests within a specified time window. This middleware can be easily integrated into the ASP.NET Core pipeline to enforce rate limiting policies.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Middleware;

// Create logger (typically from DI container)
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<RateLimitingMiddleware>();

// Create rate limiting policy
var policy = new RateLimitPolicy
{
    MaxRequests = 100,
    WindowSeconds = 60
};

// Create the rate limiting middleware
var middleware = new RateLimitingMiddleware(logger, policy);

// Example usage in an ASP.NET Core pipeline
app.UseMiddleware<RateLimitingMiddleware>();

// Or use directly
var clientId = "client123";
if (middleware.IsRequestAllowed(clientId))
{
    Console.WriteLine("Request allowed");
    // Record the request
    middleware.RecordRequest(clientId);
}
else
{
    Console.WriteLine("Rate limit exceeded");
}

// Available policies
var defaultPolicy = RateLimitPolicy.Default();
var strictPolicy = RateLimitPolicy.Strict();
var lenientPolicy = RateLimitPolicy.Lenient();
```
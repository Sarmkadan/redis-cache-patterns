# CachingHeaderMiddleware

The `CachingHeaderMiddleware` is an ASP.NET Core middleware component designed to standardize and apply HTTP caching headers to responses based on configurable policies. It intercepts the request pipeline to inject `Cache-Control` and related headers, enabling fine-grained control over client and proxy caching behavior without modifying individual endpoint logic. This component supports both global default settings and dynamic policy registration for specific scenarios.

## API

### Constructors

#### `public CachingHeaderMiddleware()`
Initializes a new instance of the `CachingHeaderMiddleware` class with default configuration values. The instance is ready to be registered in the application pipeline, though specific caching policies may need to be registered or properties adjusted before use.

### Methods

#### `public async Task InvokeAsync(HttpContext context)`
Executes the middleware logic for the current HTTP request. This method processes the incoming `HttpContext`, determines the applicable caching policy, and appends the appropriate headers to the response before passing control to the next delegate in the pipeline.
*   **Parameters**:
    *   `context`: The `HttpContext` for the current request.
*   **Returns**: A `Task` representing the asynchronous operation.
*   **Throws**: Throws `ArgumentNullException` if `context` is null. May propagate exceptions thrown by downstream middleware.

#### `public void RegisterPolicy(string name, Action<CachingHeaderMiddleware> configure)`
Registers a named caching policy that can be selected dynamically during request processing. This allows different endpoints or conditions to utilize distinct caching configurations using the same middleware instance.
*   **Parameters**:
    *   `name`: The unique identifier for the policy.
    *   `configure`: An action delegate to configure the middleware properties specifically for this policy.
*   **Returns**: Void.
*   **Throws**: Throws `ArgumentException` if a policy with the specified `name` already exists.

#### `public string GenerateHeaderValue()`
Generates the raw string value for the `Cache-Control` header based on the current property settings of the instance (e.g., `MaxAgeSeconds`, `IsPublic`, `NoStore`).
*   **Parameters**: None.
*   **Returns**: A formatted string representing the HTTP Cache-Control directives.
*   **Throws**: None.

### Properties

#### `public int MaxAgeSeconds`
Gets or sets the `max-age` directive in seconds, indicating the maximum amount of time a resource is considered fresh for clients.
*   **Default**: 0 (if not explicitly set).
*   **Remarks**: A value of -1 typically indicates no max-age directive should be emitted, depending on implementation logic.

#### `public int SMaxAgeSeconds`
Gets or sets the `s-maxage` directive in seconds, which overrides `max-age` for shared caches (proxies/CDNs).
*   **Default**: 0.
*   **Remarks**: If set to a positive value, it implies the response is cacheable by shared caches unless `NoStore` is true.

#### `public bool IsPublic`
Gets or sets a value indicating whether the `public` directive should be included.
*   **Default**: false.
*   **Remarks**: When true, the response may be cached by any cache, even if the response would normally be non-cacheable (e.g., authenticated requests).

#### `public bool NoCache`
Gets or sets a value indicating whether the `no-cache` directive should be included.
*   **Default**: false.
*   **Remarks**: When true, caches must revalidate with the origin server before using the cached copy.

#### `public bool NoStore`
Gets or sets a value indicating whether the `no-store` directive should be included.
*   **Default**: false.
*   **Remarks**: When true, caches are instructed not to store any part of the request or response. This usually overrides other caching directives.

#### `public bool MustRevalidate`
Gets or sets a value indicating whether the `must-revalidate` directive should be included.
*   **Default**: false.
*   **Remarks**: When true, the cache must not use the entry once it becomes stale without successful validation with the origin server.

## Usage

### Example 1: Global Default Configuration
This example demonstrates registering the middleware with a global default policy that caches public content for 5 minutes, requiring revalidation after expiration.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure the middleware instance with default values
var cachingMiddleware = new CachingHeaderMiddleware
{
    MaxAgeSeconds = 300,
    IsPublic = true,
    MustRevalidate = true
};

// Register the middleware logic manually or via extension if available
// Assuming a standard UseMiddleware pattern or direct invocation setup
builder.Services.AddSingleton(cachingMiddleware);

var app = builder.Build();

// Invoke the middleware in the pipeline
app.Use(async (context, next) =>
{
    await cachingMiddleware.InvokeAsync(context);
    await next();
});

app.Run();
```

### Example 2: Dynamic Policy Registration
This example shows how to register distinct policies for different content types and apply them conditionally within the pipeline.

```csharp
var middleware = new CachingHeaderMiddleware();

// Register a strict policy for sensitive data
middleware.RegisterPolicy("Sensitive", config =>
{
    config.NoStore = true;
    config.NoCache = true;
});

// Register an aggressive policy for static assets
middleware.RegisterPolicy("StaticAssets", config =>
{
    config.MaxAgeSeconds = 86400;
    config.SMaxAgeSeconds = 604800;
    config.IsPublic = true;
});

var app = WebApplication.Create();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/user"))
    {
        // Apply 'Sensitive' policy logic manually or via context items
        var header = middleware.GenerateHeaderValue(); // Note: Requires instance state update or overload
        // In a real scenario, RegisterPolicy might return a configured instance or 
        // the middleware checks context items to switch internal state.
        // For this pattern, we assume the middleware inspects context to select policy.
        context.Items["CachePolicy"] = "Sensitive";
    }
    
    await middleware.InvokeAsync(context);
    await next();
});

app.Run();
```

## Notes

*   **Thread Safety**: The properties (`MaxAgeSeconds`, `IsPublic`, etc.) are mutable and not thread-safe for concurrent modification. If a single instance of `CachingHeaderMiddleware` is shared across requests (as is common with singleton services), direct modification of these properties during `InvokeAsync` without synchronization will lead to race conditions. It is recommended to treat the instance as immutable after startup or use `RegisterPolicy` to encapsulate state changes safely if the internal implementation supports it.
*   **Directive Precedence**: When `NoStore` is set to `true`, it generally supersedes `MaxAgeSeconds`, `SMaxAgeSeconds`, and `IsPublic` according to HTTP specifications. The `GenerateHeaderValue` method should logically omit freshness directives when `NoStore` is active.
*   **Policy Selection**: The `RegisterPolicy` method stores configurations internally. The mechanism by which `InvokeAsync` selects a specific policy (e.g., via `HttpContext.Items`, headers, or path matching) is not exposed in the public API surface provided; consumers must ensure the correct context is established before invocation if multiple policies are registered.
*   **Zero Values**: Setting `MaxAgeSeconds` or `SMaxAgeSeconds` to 0 does not necessarily disable caching but indicates the resource is stale immediately, often triggering revalidation behavior depending on the presence of `MustRevalidate`.

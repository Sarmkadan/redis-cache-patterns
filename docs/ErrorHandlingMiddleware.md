# ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` class is an ASP.NET Core middleware component designed to intercept exceptions thrown during request processing, capture error details, and produce a structured error response. It exposes properties that describe the last error encountered, allowing downstream components or logging infrastructure to inspect the error state. The middleware itself does not terminate the request pipeline on success; it only acts when an exception occurs.

## API

### `public ErrorHandlingMiddleware()`

Initializes a new instance of the `ErrorHandlingMiddleware` class.  
No parameters are required. The middleware is typically registered in the application's request pipeline via `UseMiddleware<ErrorHandlingMiddleware>()`.

### `public async Task InvokeAsync(HttpContext context)`

Processes an incoming HTTP request.  
- **Parameters**:  
  - `context` – The `HttpContext` for the current request.  
- **Return value**: A `Task` representing the asynchronous operation.  
- **Behavior**: The method invokes the next middleware in the pipeline. If the next middleware throws an exception, the exception is caught, and the middleware populates its own properties (`ErrorId`, `StatusCode`, `Message`, `Details`, `Timestamp`) with information about the error. It then writes a JSON error response to the context. If no exception occurs, the middleware completes without modifying the response.  
- **Throws**: This method does not throw exceptions to the caller; it handles all exceptions internally. However, it may rethrow if the error response cannot be written (e.g., due to a disposed response stream).

### `public string ErrorId`

Gets or sets a unique identifier for the last error that was handled.  
This value is typically a GUID string. It is set when an exception is caught during `InvokeAsync`.

### `public int StatusCode`

Gets or sets the HTTP status code associated with the last error.  
Common values are 400, 404, 500, etc. The default is 0 if no error has occurred.

### `public string Message`

Gets or sets a human-readable message describing the last error.  
This is usually derived from the exception's `Message` property or a default message.

### `public string? Details`

Gets or sets optional detailed information about the last error.  
May contain the exception's stack trace, inner exception details, or other diagnostic data. Can be `null` if no details are available.

### `public DateTime Timestamp`

Gets or sets the UTC timestamp when the last error was captured.  
Set to `DateTime.UtcNow` at the moment the exception is caught.

## Usage

### Example 1: Registering the middleware in the pipeline

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Add ErrorHandlingMiddleware before other middleware that may throw
        app.UseMiddleware<ErrorHandlingMiddleware>();
        app.UseRouting();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}
```

### Example 2: Using the middleware to log error details

```csharp
public class ErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorLoggingMiddleware> _logger;

    public ErrorLoggingMiddleware(RequestDelegate next, ILogger<ErrorLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ErrorHandlingMiddleware errorHandler)
    {
        await _next(context);

        // After the pipeline, inspect the error handler's properties
        if (errorHandler.StatusCode != 0)
        {
            _logger.LogError(
                "Error {ErrorId}: {StatusCode} - {Message} at {Timestamp}",
                errorHandler.ErrorId,
                errorHandler.StatusCode,
                errorHandler.Message,
                errorHandler.Timestamp);
        }
    }
}
```

## Notes

- **Thread safety**: The `ErrorHandlingMiddleware` class is not thread-safe. Its mutable properties (`ErrorId`, `StatusCode`, `Message`, `Details`, `Timestamp`) are overwritten on each request that encounters an exception. If the middleware is registered as a singleton (the default for middleware), concurrent requests may race to read or write these properties. For reliable per-request error inspection, consider injecting a scoped service or using the `HttpContext.Items` collection instead of relying on the middleware instance's properties.
- **Edge cases**:  
  - If no exception occurs during a request, the properties retain their default values (`ErrorId` is `null`, `StatusCode` is `0`, `Message` is `null`, `Details` is `null`, `Timestamp` is `DateTime.MinValue`).  
  - If an exception is thrown after the middleware has already started writing the response, the middleware may not be able to replace the response body. In such cases, the exception is rethrown and the response is left in an incomplete state.  
  - The `Details` property may contain sensitive information (e.g., stack traces). Ensure that the middleware is configured to sanitize or omit details in production environments.

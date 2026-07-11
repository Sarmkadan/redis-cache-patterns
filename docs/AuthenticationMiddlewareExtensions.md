# AuthenticationMiddlewareExtensions

The `AuthenticationMiddlewareExtensions` class provides a set of static utility methods designed to simplify the extraction and validation of authentication context within middleware or application logic. By bridging the gap between standard authentication artifacts—such as `ClaimsPrincipal` objects or raw authentication tokens—and the application-specific `AuthContext`, these extensions ensure a consistent and type-safe approach to handling user identity and scope-based permissions throughout the request pipeline.

## API

### `public static AuthContext CreateContextFromPrincipal(ClaimsPrincipal principal)`
Creates an `AuthContext` instance from a validated `ClaimsPrincipal`.
*   **Parameters**: `principal` (a `ClaimsPrincipal` representing the authenticated user).
*   **Returns**: A populated `AuthContext` object.
*   **Throws**: `ArgumentNullException` if the `principal` is null.

### `public static AuthContext CreateContextFromToken(string token)`
Initializes an `AuthContext` by parsing a raw authentication token.
*   **Parameters**: `token` (a `string` containing the serialized authentication token).
*   **Returns**: A populated `AuthContext` object.
*   **Throws**: `ArgumentException` if the `token` is null, empty, or structurally invalid.

### `public static bool HasAnyScope(AuthContext context, string scope)`
Determines whether the provided authentication context contains the specified permission scope.
*   **Parameters**: `context` (the `AuthContext` to inspect), `scope` (the `string` identifier of the required scope).
*   **Returns**: `true` if the context contains the scope; otherwise, `false`.
*   **Throws**: `ArgumentNullException` if `context` is null.

### `public static string? GetUserEmail(AuthContext context)`
Retrieves the email address associated with the authenticated user context, if available.
*   **Parameters**: `context` (the `AuthContext` to inspect).
*   **Returns**: A `string` containing the email address if present; otherwise, `null`.
*   **Throws**: `ArgumentNullException` if `context` is null.

## Usage

### Extracting Context from Claims
```csharp
public async Task InvokeAsync(HttpContext context)
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var authContext = AuthenticationMiddlewareExtensions.CreateContextFromPrincipal(context.User);
        
        if (AuthenticationMiddlewareExtensions.HasAnyScope(authContext, "read:data"))
        {
            // Process request
        }
    }
}
```

### Retrieving User Information
```csharp
var authContext = AuthenticationMiddlewareExtensions.CreateContextFromToken(token);
string? email = AuthenticationMiddlewareExtensions.GetUserEmail(authContext);

if (!string.IsNullOrEmpty(email))
{
    _logger.LogInformation("Processing request for user: {Email}", email);
}
```

## Notes

*   **Thread Safety**: All methods within `AuthenticationMiddlewareExtensions` are `static` and stateless. They do not maintain internal state between calls and are inherently thread-safe, provided the `AuthContext` objects and other input parameters passed to them are handled correctly by the caller.
*   **Edge Cases**:
    *   `CreateContextFromToken` may throw exceptions related to token malformation or signature verification failures depending on the underlying implementation. Consumers should wrap calls to this method in appropriate error handling blocks.
    *   `GetUserEmail` returns `null` if the underlying token or principal does not contain an email claim, which is a common scenario in scoped or limited-access tokens. Always perform a null check before dereferencing the result.
    *   `HasAnyScope` is case-sensitive regarding the scope string. Ensure that scope identifiers used in the application match the exact casing defined in the authorization server.

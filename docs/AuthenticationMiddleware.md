# AuthenticationMiddleware

Middleware component that authenticates incoming HTTP requests using a configurable authentication scheme and populates the current request context with user identity information extracted from the request.

## API

### `AuthenticationMiddleware`

Constructor that initializes the middleware with the specified authentication scheme.

| Member | Description |
|--------|-------------|
| **Constructor** | Accepts the authentication scheme used to validate requests. The scheme must match the value provided in the `Authorization` header during authentication. |

### `InvokeAsync`

Invokes the middleware pipeline to authenticate the request.

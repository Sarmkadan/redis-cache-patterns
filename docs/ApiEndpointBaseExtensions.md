# ApiEndpointBaseExtensions

The `ApiEndpointBaseExtensions` static class provides convenience methods for creating consistent `ApiResponse<T>` instances from an `ApiEndpointBase`. Each method validates its extension target, delegates response creation to `ApiResponse<T>`, and supports reference-type payloads.

## API

### `ToSuccessResponse<T>`

Creates a successful response containing the supplied payload.

- **Parameters**:
  - `endpoint` (`ApiEndpointBase`): The endpoint used as the extension target.
  - `data` (`T`): The non-null payload to include in the response.
- **Type constraints**: `T` must be a reference type.
- **Return value**: An `ApiResponse<T>` with `IsSuccess` set to `true`, `Data` set to the supplied payload, and `StatusCode` set to `200`.
- **Exceptions**: Throws `ArgumentNullException` if `endpoint` or `data` is null.

### `ToFailureResponse<T>`

Creates a failed response with an error message and HTTP status code.

- **Parameters**:
  - `endpoint` (`ApiEndpointBase`): The endpoint used as the extension target.
  - `error` (`string`): The non-null, non-empty error message.
  - `statusCode` (`int`, optional): The HTTP status code for the failure. Defaults to `500`.
- **Type constraints**: `T` must be a reference type.
- **Return value**: An `ApiResponse<T>` with `IsSuccess` set to `false`, `Error` set to the supplied message, and `StatusCode` set to the supplied status code.
- **Exceptions**: Throws `ArgumentNullException` if `endpoint` is null. Throws `ArgumentException` if `error` is null or empty.

### `ToUnauthorizedResponse<T>`

Creates an unauthorized response with HTTP status code `401`.

- **Parameters**:
  - `endpoint` (`ApiEndpointBase`): The endpoint used as the extension target.
  - `error` (`string`, optional): The non-null, non-empty error message. Defaults to `"Unauthorized"`.
- **Type constraints**: `T` must be a reference type.
- **Return value**: An `ApiResponse<T>` with `IsSuccess` set to `false`, `Error` set to the supplied message, and `StatusCode` set to `401`.
- **Exceptions**: Throws `ArgumentNullException` if `endpoint` is null. Throws `ArgumentException` if `error` is null or empty.

### `ToNotFoundResponse<T>`

Creates a not-found response with HTTP status code `404`.

- **Parameters**:
  - `endpoint` (`ApiEndpointBase`): The endpoint used as the extension target.
  - `error` (`string`, optional): The non-null, non-empty error message. Defaults to `"Not found"`.
- **Type constraints**: `T` must be a reference type.
- **Return value**: An `ApiResponse<T>` with `IsSuccess` set to `false`, `Error` set to the supplied message, and `StatusCode` set to `404`.
- **Exceptions**: Throws `ArgumentNullException` if `endpoint` is null. Throws `ArgumentException` if `error` is null or empty.

## Usage

```csharp
public sealed class UserEndpoint : ApiEndpointBase
{
    public UserEndpoint(ILogger<UserEndpoint> logger, PerformanceMonitor performanceMonitor)
        : base(logger, performanceMonitor)
    {
    }

    public ApiResponse<User> GetUser(User? user)
    {
        if (user is null)
        {
            return this.ToNotFoundResponse<User>("User not found");
        }

        return this.ToSuccessResponse(user);
    }

    public ApiResponse<User> RejectRequest(bool isAuthenticated)
    {
        return isAuthenticated
            ? this.ToFailureResponse<User>("Request could not be processed", 400)
            : this.ToUnauthorizedResponse<User>();
    }
}
```

## Notes

- These methods create response objects only; they do not write an HTTP response or end request processing.
- The `endpoint` argument is used as the extension-method target and is validated for null, but its instance state is not otherwise accessed.
- Error messages must not be null or empty. A message containing only whitespace is accepted by the underlying validation.
- `Timestamp` is initialized by `ApiResponse<T>` when the response object is created. `RequestId` is not populated by these extensions.

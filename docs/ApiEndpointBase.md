# ApiEndpointBase

The `ApiEndpointBase` class acts as a standardized wrapper for API response data, providing a consistent structure for transmitting results, error information, and diagnostic metadata between the API layer and the consumer. It encapsulates the operational outcome of a request, including the HTTP status code, a unique request identifier, and a timestamp, alongside the actual payload or error details.

## API

*   `IsSuccess`: A `bool` indicating whether the request was completed successfully.
*   `Data`: A generic property of type `T?` containing the payload if the request was successful.
*   `Error`: A nullable `string?` containing descriptive error details if `IsSuccess` is false.
*   `StatusCode`: An `int` representing the HTTP status code associated with the response.
*   `Timestamp`: A `DateTime` value recording when the response object was initialized.
*   `RequestId`: A nullable `string?` containing a unique identifier for the request, useful for logging and distributed tracing.
*   `Success`: A static member that provides an `ApiResponse<T>` instance representing a successful request outcome.
*   `Failure`: A static member that provides an `ApiResponse<T>` instance representing a failed request outcome.
*   `Unauthorized`: A static member that provides an `ApiResponse<T>` instance representing an unauthorized access outcome.
*   `NotFound`: A static member that provides an `ApiResponse<T>` instance representing a 'not found' request outcome.

## Usage

```csharp
// Example of handling a not found scenario
public ApiResponse<User> GetUserById(int userId) {
    var user = _repository.Find(userId);
    if (user == null) {
        return ApiResponse<User>.NotFound;
    }
    
    var response = ApiResponse<User>.Success;
    response.Data = user;
    return response;
}
```

```csharp
// Example of returning a failure response
public ApiResponse<string> SubmitData(string input) {
    if (string.IsNullOrEmpty(input)) {
        return ApiResponse<string>.Failure;
    }
    
    // Process input ...
    
    var response = ApiResponse<string>.Success;
    response.Data = "Submission processed successfully.";
    return response;
}
```

## Notes

*   **Thread Safety**: Instances of `ApiResponse<T>` are typically treated as data transfer objects. While the properties themselves are mutable, it is recommended to treat these objects as immutable once they are prepared for return to ensure thread safety when consumed by multiple threads.
*   **Edge Cases**: When using the static `Success` member, ensure that the `Data` property is populated appropriately before returning the object. If `T` is a reference type, the `Data` property may be null if not explicitly set, which should be accounted for by the consumer.
*   **Diagnostic Metadata**: The `RequestId` and `Timestamp` fields are vital for effective monitoring and troubleshooting. It is recommended that the infrastructure or the factory methods responsible for creating `ApiResponse<T>` instances ensure these fields are populated to allow for accurate request correlation.

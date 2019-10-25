# BusinessException
The `BusinessException` type is a custom exception class designed to handle business logic errors in the `redis-cache-patterns` project. It provides a structured way to represent and handle exceptions that occur during the execution of business rules, allowing for more informative error messages and better error handling.

## API
The `BusinessException` type has several public members:
* `ErrorCode`: a string property that represents the error code associated with the exception.
* `BusinessException(string message)`: a constructor that initializes a new instance of the `BusinessException` class with a specified error message.
* `BusinessException(string message, string errorCode)`: a constructor that initializes a new instance of the `BusinessException` class with a specified error message and error code.
* `NotFoundException(string resourceType, int id)`: a constructor that initializes a new instance of the `NotFoundException` class, which is presumably a subclass of `BusinessException`, with a specified resource type and ID.
* `NotFoundException(string message)`: a constructor that initializes a new instance of the `NotFoundException` class with a specified error message.
* `ValidationException(string message)`: a constructor that initializes a new instance of the `ValidationException` class, which is presumably a subclass of `BusinessException`, with a specified error message.
* `ValidationException(Dictionary<string, List<string>> errors)`: a constructor that initializes a new instance of the `ValidationException` class with a dictionary of error messages.
* `Errors`: a dictionary property that represents a collection of error messages.
* `AddError`: a method that adds an error to the `Errors` dictionary.
* `Requested` and `Available`: integer properties that represent the requested and available quantities, respectively, presumably used in the context of inventory management.
* `InsufficientInventoryException`: a subclass of `BusinessException` that represents an exception that occurs when there is insufficient inventory.
* `ConcurrencyException(string message)`: a constructor that initializes a new instance of the `ConcurrencyException` class, which is presumably a subclass of `BusinessException`, with a specified error message.

## Usage
Here are two examples of using the `BusinessException` type:
```csharp
try
{
    // Attempt to retrieve a resource from the cache
    var resource = cache.GetResource("resourceType", 123);
    if (resource == null)
    {
        throw new NotFoundException("resourceType", 123);
    }
}
catch (NotFoundException ex)
{
    Console.WriteLine($"Resource not found: {ex.ErrorCode}");
}

try
{
    // Attempt to validate a request
    var errors = ValidateRequest(request);
    if (errors.Count > 0)
    {
        throw new ValidationException(errors);
    }
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation errors: {ex.Errors}");
}
```

## Notes
When using the `BusinessException` type, it is essential to consider the following edge cases and thread-safety remarks:
* The `ErrorCode` property should be used to provide a unique identifier for the error, allowing for easier error handling and logging.
* The `AddError` method should be used to add errors to the `Errors` dictionary in a thread-safe manner.
* The `Requested` and `Available` properties should be used in the context of inventory management to track the requested and available quantities.
* The `InsufficientInventoryException` subclass should be used to represent exceptions that occur when there is insufficient inventory.
* The `ConcurrencyException` subclass should be used to represent exceptions that occur due to concurrency issues.
* When throwing a `BusinessException`, it is crucial to provide a descriptive error message and error code to facilitate error handling and logging.

# ValidationHelper

A static utility class that provides common validation methods for strings, collections, and business objects, typically used to enforce consistent validation rules across application layers such as controllers, services, and data access components.

## API

### `ValidateUsername(string username)`

Validates a username according to application-specific rules.

- **Parameters**
  - `username` (string): The username to validate.
- **Throws**
  - `ArgumentException`: If `username` is `null`, empty, or fails username format rules (e.g., length, allowed characters).

### `ValidateEmail(string email)`

Validates an email address using standard format rules.

- **Parameters**
  - `email` (string): The email address to validate.
- **Throws**
  - `ArgumentException`: If `email` is `null`, empty, or does not match a valid email pattern.

### `ValidatePassword(string password)`

Validates a password against minimum complexity requirements.

- **Parameters**
  - `password` (string): The password to validate.
- **Throws**
  - `ArgumentException`: If `password` is `null`, empty, or does not meet minimum length or character diversity requirements.

### `ValidateProductName(string productName)`

Validates a product name for acceptable formatting and length.

- **Parameters**
  - `productName` (string): The product name to validate.
- **Throws**
  - `ArgumentException`: If `productName` is `null`, empty, or exceeds maximum length or contains disallowed characters.

### `ValidatePrice(decimal price)`

Validates a product price to ensure it is positive and within a reasonable range.

- **Parameters**
  - `price` (decimal): The price to validate.
- **Throws**
  - `ArgumentException`: If `price` is less than or equal to zero or exceeds a maximum allowed value.

### `ValidateQuantity(int quantity)`

Validates a product quantity to ensure it is positive and within a reasonable range.

- **Parameters**
  - `quantity` (int): The quantity to validate.
- **Throws**
  - `ArgumentException`: If `quantity` is less than or equal to zero or exceeds a maximum allowed value.

### `ValidateNotNull<T>(T value, string paramName)`

Ensures that a value is not `null`.

- **Parameters**
  - `value` (T): The value to validate.
  - `paramName` (string): The name of the parameter, used in the exception message.
- **Throws**
  - `ArgumentNullException`: If `value` is `null`.

### `ValidateNotNullOrEmpty(string value, string paramName)`

Ensures that a string is not `null` or empty.

- **Parameters**
  - `value` (string): The string to validate.
  - `paramName` (string): The name of the parameter, used in the exception message.
- **Throws**
  - `ArgumentException`: If `value` is `null` or empty.

### `GetValidationErrors()`

Collects and returns all validation errors encountered during validation operations.

- **Returns**
  - `Dictionary<string, List<string>>`: A dictionary where keys are validation context names (e.g., "Username", "Email") and values are lists of error messages for that context.
- **Remarks**
  - This method is typically used after batch validation to aggregate multiple validation failures into a single report.

## Usage

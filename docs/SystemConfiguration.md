# SystemConfiguration

Represents a configuration entry stored in a Redis cache. Each instance holds a key-value pair along with metadata such as data type, category, active status, and timestamps. The type provides generic methods to read and write strongly-typed values, as well as JSON-serialized values, and methods to toggle the active state.

## API

### `public int Id`
Gets or sets the unique identifier of the configuration entry.

### `public string Key`
Gets or sets the configuration key. This value is typically used as the Redis key or part of a composite key.

### `public string Value`
Gets or sets the raw string value of the configuration entry. This property is the underlying storage for all typed accessors.

### `public string DataType`
Gets or sets the name of the expected data type (e.g., `"int"`, `"string"`, `"json"`). This is metadata and does not enforce type safety at runtime.

### `public string? Description`
Gets or sets an optional description of the configuration entry. Can be `null`.

### `public bool IsActive`
Gets or sets a value indicating whether the configuration entry is currently active. Inactive entries may be ignored by consumers.

### `public DateTime CreatedAt`
Gets or sets the timestamp when the configuration entry was first created.

### `public DateTime? UpdatedAt`
Gets or sets the timestamp of the last update. `null` if never updated.

### `public string Category`
Gets or sets the category or group to which this configuration belongs (e.g., `"FeatureFlags"`, `"ConnectionStrings"`).

### `public T GetValue<T>()`
Attempts to convert the raw `Value` string to the specified type `T`.

- **Type parameters**: `T` – The target type. Must have a `TryParse` method or be a type supported by `Convert.ChangeType`.
- **Returns**: The converted value of type `T`.
- **Throws**: `InvalidCastException` if the conversion fails. `FormatException` if the string is not in the correct format for the target type. `NotSupportedException` if the type `T` is not supported.

### `public void SetValue<T>(T value)`
Converts the provided `value` to a string and assigns it to the `Value` property. Also updates `UpdatedAt` to the current UTC time.

- **Type parameters**: `T` – The type of the value to store.
- **Parameters**: `value` – The value to store. Can be `null` for nullable types.
- **Throws**: `ArgumentNullException` if `value` is `null` and `T` is a non-nullable value type.

### `public void SetJsonValue<T>(T value)`
Serializes the provided `value` to a JSON string using `System.Text.Json` and assigns it to the `Value` property. Also updates `UpdatedAt` to the current UTC time.

- **Type parameters**: `T` – The type of the value to serialize.
- **Parameters**: `value` – The value to serialize. Can be `null`.
- **Throws**: `ArgumentNullException` if `value` is `null` and `T` is a non-nullable value type. `JsonException` if serialization fails.

### `public void Disable()`
Sets `IsActive` to `false` and updates `UpdatedAt` to the current UTC time.

### `public void Enable()`
Sets `IsActive` to `true` and updates `UpdatedAt` to the current UTC time.

### `public override string ToString()`
Returns a string representation of the configuration entry, typically including the `Key`, `Value`, `IsActive`, and `Category`.

- **Returns**: A formatted string.

## Usage

### Example 1: Storing and retrieving a simple integer configuration

```csharp
var config = new SystemConfiguration
{
    Key = "app:maxRetries",
    Category = "Settings",
    DataType = "int",
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};

// Store an integer value
config.SetValue(5);

// Later, retrieve it
int maxRetries = config.GetValue<int>();
Console.WriteLine($"Max retries: {maxRetries}"); // Output: Max retries: 5
```

### Example 2: Working with JSON configuration and toggling active state

```csharp
var config = new SystemConfiguration
{
    Key = "feature:newDashboard",
    Category = "FeatureFlags",
    DataType = "json",
    IsActive = false,
    CreatedAt = DateTime.UtcNow
};

// Store a complex object as JSON
var dashboardSettings = new { Enabled = true, Theme = "dark" };
config.SetJsonValue(dashboardSettings);

// Enable the feature
config.Enable();

// Retrieve the JSON value back
var settings = config.GetValue<DashboardSettings>(); // Assume DashboardSettings is a POCO
Console.WriteLine($"Dashboard enabled: {settings.Enabled}"); // Output: Dashboard enabled: True
```

## Notes

- **Thread safety**: This type is not thread-safe. Concurrent reads and writes to the same instance from multiple threads may result in inconsistent state. External synchronization (e.g., a lock) is required when sharing an instance across threads.
- **Null handling**: The `Value` property can be `null`. Calling `GetValue<T>()` on a `null` string will throw a `FormatException` for most value types. `SetValue<T>(null)` is allowed only if `T` is a nullable type; otherwise an `ArgumentNullException` is thrown.
- **Type conversion**: `GetValue<T>()` relies on `Convert.ChangeType` and `TryParse` patterns. For custom types, ensure they implement `IConvertible` or provide a static `Parse` method. `SetJsonValue<T>()` uses `System.Text.Json` and requires the type to be serializable by that library.
- **Timestamp updates**: Both `SetValue<T>` and `SetJsonValue<T>` automatically update `UpdatedAt`. Direct assignment to `Value` does **not** update `UpdatedAt` – callers must manage timestamps manually if they bypass the setter methods.
- **Edge case – empty or whitespace strings**: A `Value` consisting only of whitespace may cause `GetValue<T>()` to throw a `FormatException` for numeric types. It is recommended to validate or trim values before storage.

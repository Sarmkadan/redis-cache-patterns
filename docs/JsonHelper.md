# JsonHelper

`JsonHelper` is a static utility class that provides a thin, opinionated wrapper over standard JSON serialization and deserialization operations. It centralizes common JSON handling patterns—serialization, safe deserialization with fallback defaults, validation, value extraction, and merging—into a single consistent API, reducing boilerplate and ensuring uniform error handling across the `redis-cache-patterns` project.

## API

### `Serialize<T>`
```csharp
public static string Serialize<T>(T value)
```
Serializes an object of type `T` to its JSON string representation using the library's default serializer settings.

- **Parameters**: `value` — the object to serialize.
- **Returns**: a JSON string.
- **Throws**: may throw `JsonException` or other serialization-related exceptions if `value` cannot be serialized (e.g., circular references, unsupported types).

### `Deserialize<T>`
```csharp
public static T? Deserialize<T>(string json)
```
Deserializes a JSON string to an instance of type `T`. Returns `null` if the input string is `null` or empty.

- **Parameters**: `json` — the JSON string to deserialize.
- **Returns**: an instance of `T`, or `null` if the input is null/empty.
- **Throws**: `JsonException` if the JSON is structurally invalid or cannot be mapped to type `T`.

### `DeserializeSafe<T>`
```csharp
public static T? DeserializeSafe<T>(string json)
```
Deserializes a JSON string to an instance of type `T` with a safe fallback. If deserialization fails for any reason (invalid JSON, type mismatch, null input), the method returns `default(T?)` instead of throwing.

- **Parameters**: `json` — the JSON string to deserialize.
- **Returns**: an instance of `T` on success, or `default(T?)` (typically `null` for reference types) on failure.
- **Throws**: does not throw; all exceptions are caught internally.

### `IsValidJson`
```csharp
public static bool IsValidJson(string json)
```
Determines whether a string represents syntactically valid JSON.

- **Parameters**: `json` — the string to validate.
- **Returns**: `true` if the string is valid JSON; `false` otherwise (including null or empty input).
- **Throws**: does not throw.

### `GetValue`
```csharp
public static object? GetValue(string json, string path)
```
Extracts a value from a JSON string using a path expression (e.g., `$.store.book[0].title`). Returns `null` if the path does not exist or the input is invalid.

- **Parameters**:
  - `json` — the JSON string to query.
  - `path` — the path expression identifying the value to retrieve.
- **Returns**: the value at the specified path as an `object?`, or `null` if not found.
- **Throws**: may throw `JsonException` if the JSON is malformed; path syntax errors depend on the underlying implementation.

### `Merge`
```csharp
public static string Merge(string originalJson, string patchJson)
```
Merges two JSON strings, applying the properties from `patchJson` onto `originalJson`. Properties in `patchJson` overwrite those in `originalJson` at matching paths; properties unique to `originalJson` are preserved.

- **Parameters**:
  - `originalJson` — the base JSON string.
  - `patchJson` — the JSON string containing properties to merge in.
- **Returns**: a new JSON string representing the merged result.
- **Throws**: `JsonException` if either input is invalid JSON, or if the merge operation fails due to structural incompatibility.

## Usage

### Example 1: Cache-Aside with Safe Deserialization
```csharp
// Retrieve from cache; gracefully handle corrupted or missing data
string? cachedJson = redis.StringGet("user:1001");

User? user = JsonHelper.DeserializeSafe<User>(cachedJson);

if (user is null)
{
    // Cache miss or corrupted data — fetch from primary store
    user = db.Users.Find(1001);
    redis.StringSet("user:1001", JsonHelper.Serialize(user));
}
```

### Example 2: Conditional Cache Update with JSON Validation and Merging
```csharp
string? existingJson = redis.StringGet("config:app");

if (existingJson is not null && JsonHelper.IsValidJson(existingJson))
{
    // Apply partial update without replacing the entire cached object
    string patchJson = JsonHelper.Serialize(new { Theme = "dark", Notifications = true });
    string mergedJson = JsonHelper.Merge(existingJson, patchJson);
    redis.StringSet("config:app", mergedJson);
}
else
{
    // No valid cache entry — seed with fresh data
    var fullConfig = new AppConfig { Theme = "dark", Notifications = true, Version = 2 };
    redis.StringSet("config:app", JsonHelper.Serialize(fullConfig));
}
```

## Notes

- **Null and empty handling**: `Deserialize<T>` and `IsValidJson` treat `null` and empty strings as non-valid, returning `null` and `false` respectively. `DeserializeSafe<T>` extends this by catching all exceptions, making it suitable for data from untrusted or potentially corrupted sources such as a Redis cache.
- **Path syntax for `GetValue`**: The path argument follows the conventions of the underlying JSON library (e.g., JsonPath syntax). An invalid path may return `null` or throw depending on the implementation; consult the library version in use for exact semantics.
- **Merge behavior**: `Merge` performs a shallow merge at the property level. Nested objects are replaced wholesale rather than recursively merged. Ensure both inputs are valid JSON; otherwise a `JsonException` is thrown.
- **Thread safety**: All methods are static and operate on immutable input strings. No shared mutable state is maintained. The class is safe to call concurrently from multiple threads without external synchronization.
- **Serialization defaults**: `Serialize<T>` uses the default serializer settings configured for the project. Custom converters, naming policies, or formatting rules applied at the serializer level affect all methods that accept or produce JSON strings.

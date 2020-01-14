# CacheExceptionExtensions

Provides a set of extension methods for `CacheException` that simplify error handling, diagnostics, and user‑friendly messaging in Redis‑based caching scenarios.

## API

### `public static string GetDetailedErrorMessage(this CacheException ex)`

**Purpose**  
Returns a verbose, diagnostic‑oriented string that includes the exception’s message, any inner exception details, and the full stack trace.

**Parameters**  
- `ex`: The `CacheException` to inspect. Must not be `null`.

**Return value**  
A string containing the detailed error information.

**Exceptions**  
- `ArgumentNullException` – Thrown when `ex` is `null`.

---

### `public static bool IsTransient(this CacheException ex)`

**Purpose**  
Determines whether the exception represents a transient fault (e.g., a temporary network glitch) that may succeed if the operation is retried.

**Parameters**  
- `ex`: The `CacheException` to evaluate. Must not be `null`.

**Return value**  
`true` if the exception is considered transient; otherwise `false`.

**Exceptions**  
- `ArgumentNullException` – Thrown when `ex` is `null`.

---

### `public static CacheException WithErrorCode(this CacheException ex, string errorCode)`

**Purpose**  
Creates a new `CacheException` instance that incorporates the supplied error code, typically stored in the exception’s `Data` collection for later inspection or logging.

**Parameters**  
- `ex`: The original `CacheException` to augment. Must not be `null`.  
- `errorCode`: A string identifier for the error (e.g., `"REDIS_TIMEOUT"`). Must not be `null` or empty.

**Return value**  
A new `CacheException` object with the error code attached; the original instance remains unchanged.

**Exceptions**  
- `ArgumentNullException` – Thrown when `ex` is `null`.  
- `ArgumentException` – Thrown when `errorCode` is `null` or empty.

---

### `public static string GetUserFriendlyMessage(this CacheException ex)`

**Purpose**  
Produces a concise, end‑user‑oriented message that omits technical details such as stack traces or inner exception information.

**Parameters**  
- `ex`: The `CacheException` to translate. Must not be `null`.

**Return value**  
A user‑friendly string suitable for display in UI or log messages intended for operators.

**Exceptions**  
- `ArgumentNullException` – Thrown when `ex` is `null`.

## Usage

### Example 1: Transient fault handling with retry

```csharp
try
{
    await cache.SetAsync(key, value);
}
catch (CacheException ce) when (ce.IsTransient())
{
    // Log the transient issue and retry after a short back‑off.
    logger.Warn(ce.GetDetailedErrorMessage(), "Transient cache error, retrying.");
    await Task.Delay(TimeSpan.FromSeconds(2));
    await cache.SetAsync(key, value); // retry
}
```

### Example 2: Enriching an exception with an error code before re‑throwing

```csharp
try
{
    await cache.GetAsync(key);
}
catch (CacheException ce)
{
    // Attach a domain‑specific error code and preserve the original exception as inner.
    var enriched = ce.WithErrorCode("CACHE_GET_FAIL")
                     .InnerException; // assumes WithErrorCode preserves inner exception via constructor
    throw enriched;
}
```

## Notes

- All methods are **pure** with respect to the input exception; they do not modify the original `CacheException` instance. `WithErrorCode` returns a new object, leaving the source unchanged.
- Passing `null` for the `ex` argument results in an `ArgumentNullException`; this behavior is consistent across the extension methods and helps catch programming errors early.
- The thread‑safety of these methods follows from their stateless nature: they only read the supplied exception and, in the case of `WithErrorCode`, allocate a new instance. Consequently, they can be invoked concurrently from multiple threads without external synchronization.
- `IsTransient` relies on internal logic that examines the exception’s `HResult`, message patterns, or known transient error codes. Future changes to the underlying detection criteria may affect the return value, but the method’s contract (returning a `bool`) remains stable.
- When using `WithErrorCode`, if the original exception already contains an entry with the same key in its `Data` dictionary, the new value will overwrite the existing one. Callers should choose error‑code keys unlikely to clash with existing data.

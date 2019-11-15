# HttpClientFactory

A factory for creating and managing named `HttpClient` instances with configurable defaults such as base address, timeout, headers, authentication, and retry behavior. It centralizes HTTP client configuration to avoid common pitfalls like socket exhaustion and DNS changes.

## API

### `public HttpClientFactory`

Initializes a new instance of the `HttpClientFactory` with default settings. The factory is designed to be reused and supports registration of named clients via `RegisterClient`.

### `public HttpClientFactory RegisterClient(string name)`

Registers a named HTTP client configuration with the factory.

- **Parameters**
  - `name` (string): A unique identifier for the client configuration.
- **Return Value**
  - Returns the current `HttpClientFactory` instance to support method chaining.
- **Exceptions**
  - Throws `ArgumentException` if `name` is null or whitespace.
  - Throws `InvalidOperationException` if a client with the same `name` is already registered.

### `public HttpClient GetClient(string name)`

Retrieves a configured `HttpClient` instance by name. If the client has not been registered, it throws an exception. The returned client is intended for short-lived use and should not be disposed by the caller.

- **Parameters**
  - `name` (string): The name of the registered client.
- **Return Value**
  - Returns an `HttpClient` instance configured with the registered settings.
- **Exceptions**
  - Throws `ArgumentException` if `name` is null or whitespace.
  - Throws `KeyNotFoundException` if no client is registered under the given `name`.

### `public void Dispose()`

Releases all managed resources used by the `HttpClientFactory`, including any registered `HttpClient` instances and their underlying handlers. After disposal, the factory cannot be used to retrieve or register clients.

### `public Uri? BaseAddress`

Gets or sets the default base address for all registered clients that do not have an explicit base address. Changing this value after clients have been registered does not affect existing clients.

- **Type**: `Uri?`
- **Default**: `null`

### `public TimeSpan Timeout`

Gets or sets the default request timeout for all registered clients that do not have an explicit timeout. Changing this value after clients have been registered does not affect existing clients.

- **Type**: `TimeSpan`
- **Default**: `30` seconds

### `public Dictionary<string, string>? DefaultHeaders`

Gets or sets a collection of default headers to be added to every request made by registered clients that do not have explicit headers. Changing this value after clients have been registered does not affect existing clients.

- **Type**: `Dictionary<string, string>?`
- **Default**: `null`

### `public string? AuthToken`

Gets or sets a default authorization token to be included in the `Authorization` header of every request made by registered clients that do not have an explicit token. Changing this value after clients have been registered does not affect existing clients.

- **Type**: `string?`
- **Default**: `null`

### `public int MaxRetries`

Gets or sets the default maximum number of retry attempts for failed requests made by registered clients that do not have an explicit retry policy. Changing this value after clients have been registered does not affect existing clients.

- **Type**: `int`
- **Default**: `3`

### `public int RetryDelayMs`

Gets or sets the default delay in milliseconds between retry attempts for failed requests made by registered clients that do not have an explicit retry policy. Changing this value after clients have been registered does not affect existing clients.

- **Type**: `int`
- **Default**: `1000` (1 second)

## Usage

### Example 1: Basic Usage with Default Configuration

# CacheCommand

`CacheCommand` implements cache management and diagnostic operations for the command-line interface. It dispatches requests to an `ICacheService`, optionally uses a `CacheWarmingService`, writes results to standard output, and writes validation or operation errors to standard error.

## API

### `public CacheCommand(ICacheService cacheService, ILogger<CacheCommand> logger, CacheWarmingService? warmingService = null)`

Creates a cache command handler.

- **Parameters**:
  - `cacheService`: The cache service used for statistics, key operations, expiration queries, and cache-aside loading.
  - `logger`: The logger used to record operation failures and cache-aside loading activity.
  - `warmingService`: An optional service used by the `warm` subcommand. When omitted, all other subcommands remain available.

### `public async Task<int> ExecuteAsync(Dictionary<string, string> options)`

Executes the subcommand specified by the `subcommand` option. If `subcommand` is absent, `stats` is used. Subcommand names are matched without regard to case.

- **Parameters**:
  - `options`: Command options keyed by their names without leading dashes.
- **Return value**:
  - `0` when the operation succeeds, when a flush is declined, or when cache warming has at least one successful strategy.
  - `1` for invalid input, an unknown subcommand, an operation failure, unavailable warming support, or a failed cache-aside preload. The `warm` subcommand returns `1` only when every attempted strategy failed.
- **Exceptions**:
  - The method expects a non-null options dictionary. Passing `null` fails before a subcommand can be executed.

## Subcommands

| Subcommand | Options | Behavior |
| --- | --- | --- |
| `stats` | None | Prints the total key count, memory usage in KB, hit rate, and capture timestamp. This is the default subcommand. |
| `flush` | `force` (optional) | Flushes the entire cache. Without `force`, prompts for confirmation and proceeds only for `y` or `yes`. The option's value is not inspected; its presence bypasses confirmation. |
| `keys` | `pattern` (optional, default `*`) | Prints keys matching the pattern. At most 100 keys are displayed, followed by the number omitted when applicable. |
| `get` | `key` (required) | Retrieves and prints a cached value, or reports that the key was not found. |
| `set` | `key` and `value` (required), `ttl` (optional) | Stores the value. A valid integer `ttl` is interpreted as seconds; a missing or non-integer value results in no expiration. |
| `delete` | `key` (required) | Removes the key from the cache. |
| `ttl` | `key` (required) | Prints the remaining expiration in whole seconds, or reports that the key has no expiration. |
| `warm` | None | Runs all strategies registered with the optional `CacheWarmingService` and prints the aggregate result. |
| `warm-aside` | One of `keys`, `file`, or `pattern`; `limit` applies to `pattern` | Preloads keys through `GetOrLoadAsync<object>` with a one-hour expiration. See the source rules below. |

### `warm-aside` source rules

The command selects one key source in this order: `file`, `pattern`, then `keys`.

- `keys` is a comma-separated list. Entries are trimmed and empty entries are discarded.
- `file` is a path to a text file containing one key per line. Blank lines are ignored. The resolved file must be within the current working directory; missing files and paths outside that directory fail the command.
- `pattern` retrieves matching keys from the cache. `limit` must be a positive integer to override the default of 1,000; invalid or non-positive values use the default.

Each selected key is loaded sequentially. Cache misses receive an object containing `Preloaded`, `Key`, and `Timestamp` values. The command returns `1` if any selected key fails to preload and prints all collected errors after processing the remaining keys.

## Usage

The options passed by `CommandParser` do not include leading dashes. For example, parsed CLI arguments such as `--subcommand keys --pattern product:*` produce the dictionary used below.

```csharp
var command = new CacheCommand(cacheService, logger, warmingService);

var exitCode = await command.ExecuteAsync(new Dictionary<string, string>
{
    ["subcommand"] = "keys",
    ["pattern"] = "product:*"
});
```

To preload a bounded set of matching keys through the cache-aside path:

```csharp
var exitCode = await command.ExecuteAsync(new Dictionary<string, string>
{
    ["subcommand"] = "warm-aside",
    ["pattern"] = "product:*",
    ["limit"] = "250"
});
```

## Notes

- Operational exceptions are logged, their messages are written to standard error, and the command returns `1`.
- `get` requests values as `object`; the displayed representation depends on the cache service's deserialization behavior and the value's `ToString()` implementation.
- `set` passes the supplied value as a string to the cache service.
- `warm` requires a `CacheWarmingService` supplied to the constructor. `warm-aside` uses only `ICacheService` and does not require that optional service.
- `flush --force` is destructive and does not prompt for confirmation.

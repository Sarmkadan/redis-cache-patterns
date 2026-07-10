# RedisClusterConnectionExtensions
The `RedisClusterConnectionExtensions` class provides a set of extension methods for working with Redis cluster connections in the `redis-cache-patterns` project. These methods enable developers to perform common Redis operations, such as checking key existence, getting and setting string values, deleting keys by pattern, and executing commands on all master nodes in the cluster. The methods are designed to be used with Redis cluster connections, allowing for efficient and scalable data storage and retrieval.

## API
* `public static async Task<bool> KeyExistsAsync`: Checks if a key exists in the Redis cluster. Returns `true` if the key exists, `false` otherwise. Throws if the Redis connection is not established or if an error occurs during the operation.
* `public static async Task<long> KeyExistsAsync`: Checks if a key exists in the Redis cluster and returns the number of keys that exist. Throws if the Redis connection is not established or if an error occurs during the operation.
* `public static async Task<string?> StringGetAsync`: Retrieves a string value from the Redis cluster by key. Returns the string value if the key exists, `null` otherwise. Throws if the Redis connection is not established or if an error occurs during the operation.
* `public static async Task<bool> StringSetAsync`: Sets a string value in the Redis cluster by key. Returns `true` if the operation is successful, `false` otherwise. Throws if the Redis connection is not established or if an error occurs during the operation.
* `public static async Task<long> DeleteByPatternAsync`: Deletes keys in the Redis cluster that match a given pattern. Returns the number of deleted keys. Throws if the Redis connection is not established or if an error occurs during the operation.
* `public static async Task<ClusterInfo> GetClusterHealthAsync`: Retrieves information about the health of the Redis cluster. Returns a `ClusterInfo` object containing information about the cluster's nodes and their status. Throws if the Redis connection is not established or if an error occurs during the operation.
* `public static async Task<Dictionary<string, string>> ExecuteOnAllMastersAsync`: Executes a command on all master nodes in the Redis cluster. Returns a dictionary containing the results of the command execution on each node. Throws if the Redis connection is not established or if an error occurs during the operation.

## Usage
The following examples demonstrate how to use the `RedisClusterConnectionExtensions` methods:
```csharp
// Example 1: Checking key existence and retrieving a string value
var connection = // establish a Redis cluster connection
var key = "myKey";
if (await connection.KeyExistsAsync(key))
{
    var value = await connection.StringGetAsync(key);
    Console.WriteLine($"Key {key} exists with value {value}");
}
else
{
    Console.WriteLine($"Key {key} does not exist");
}

// Example 2: Setting a string value and deleting keys by pattern
var connection = // establish a Redis cluster connection
var key = "myKey";
var value = "myValue";
await connection.StringSetAsync(key, value);
await connection.DeleteByPatternAsync("my*");
Console.WriteLine($"Key {key} set with value {value} and deleted keys matching pattern my*");
```

## Notes
When using the `RedisClusterConnectionExtensions` methods, consider the following:
* The methods are designed to work with Redis cluster connections, which provide a way to scale Redis horizontally.
* The methods throw exceptions if the Redis connection is not established or if an error occurs during the operation. It is essential to handle these exceptions properly to ensure the reliability of the application.
* The `ExecuteOnAllMastersAsync` method executes a command on all master nodes in the cluster, which can be a time-consuming operation. Use this method judiciously and consider the potential impact on the cluster's performance.
* The `RedisClusterConnectionExtensions` class is thread-safe, allowing multiple threads to access the methods concurrently. However, the underlying Redis connection may not be thread-safe, and it is essential to ensure that the connection is properly synchronized to avoid issues.

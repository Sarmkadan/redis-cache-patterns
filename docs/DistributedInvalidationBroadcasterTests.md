# DistributedInvalidationBroadcasterTests
The `DistributedInvalidationBroadcasterTests` class is designed to test the functionality of the `DistributedInvalidationBroadcaster` class, which is responsible for broadcasting invalidation messages to a Redis pub/sub channel and recording history entries. This class provides a set of test methods to ensure the correct behavior of the `DistributedInvalidationBroadcaster` class under various scenarios.

## API
The `DistributedInvalidationBroadcasterTests` class contains the following public members:
* `public DistributedInvalidationBroadcasterTests`: The constructor for the `DistributedInvalidationBroadcasterTests` class.
* `public async Task BroadcastAsync_PublishesToPubSubChannel`: Tests that the `BroadcastAsync` method publishes a message to the Redis pub/sub channel.
* `public async Task BroadcastAsync_RecordsHistoryEntry`: Tests that the `BroadcastAsync` method records a history entry.
* `public async Task BroadcastAsync_WithEmptyKey_ThrowsArgumentException`: Tests that the `BroadcastAsync` method throws an `ArgumentException` when the key is empty.
* `public async Task BroadcastPatternAsync_RecordsPatternInHistory`: Tests that the `BroadcastPatternAsync` method records a pattern in the history.
* `public async Task BroadcastPatternAsync_WithEmptyPattern_ThrowsArgumentException`: Tests that the `BroadcastPatternAsync` method throws an `ArgumentException` when the pattern is empty.
* `public async Task BroadcastAsync_WhenHistoryExceedsMax_OldestEntriesDropped`: Tests that the `BroadcastAsync` method drops the oldest entries when the history exceeds the maximum size.
* `public async Task BroadcastAsync_WhenStreamFallbackEnabled_AlsoPublishesToStream`: Tests that the `BroadcastAsync` method also publishes to a stream when the stream fallback is enabled.

## Usage
Here are two examples of using the `DistributedInvalidationBroadcasterTests` class:
```csharp
// Example 1: Testing the BroadcastAsync method
var broadcasterTests = new DistributedInvalidationBroadcasterTests();
await broadcasterTests.BroadcastAsync_PublishesToPubSubChannel();

// Example 2: Testing the BroadcastPatternAsync method
var broadcasterTests = new DistributedInvalidationBroadcasterTests();
await broadcasterTests.BroadcastPatternAsync_RecordsPatternInHistory();
```

## Notes
The `DistributedInvalidationBroadcasterTests` class is designed to be thread-safe, as it uses asynchronous methods to test the `DistributedInvalidationBroadcaster` class. However, it is still important to ensure that the test methods are executed in a controlled environment to avoid any potential conflicts. Additionally, the `ArgumentException` thrown by the `BroadcastAsync` and `BroadcastPatternAsync` methods when the key or pattern is empty can be caught and handled by the calling code to provide a more robust error handling mechanism. It is also worth noting that the `BroadcastAsync` method will drop the oldest entries when the history exceeds the maximum size, which can be configured according to the specific requirements of the application.

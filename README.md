// ... (rest of the file remains the same)

## CacheMonitor

The `CacheMonitor` class is responsible for tracking cache performance and health. It provides methods to retrieve cache statistics, print statistics, track cache entries, and calculate average hit rates. 

Here is an example of how to use the `CacheMonitor` class:

```csharp
var cacheMonitor = new CacheMonitor(cacheService, logger);
var stats = await cacheMonitor.GetStatisticsAsync();
await cacheMonitor.PrintStatisticsAsync();

// Track a cache entry
cacheMonitor.TrackEntry(new CacheEntry { HitRate = 0.8, SizeInBytes = 1024 });

// Get tracked entries
var entries = cacheMonitor.GetTrackedEntries();

// Get average hit rate
var averageHitRate = await cacheMonitor.GetAverageHitRateAsync();

// Get total cache size
var totalCacheSize = cacheMonitor.GetTotalCacheSize();

// Get entries by hit rate
var highHitRateEntries = cacheMonitor.GetEntriesByHitRate(0.5);

// Get cold entries
var coldEntries = cacheMonitor.GetColdEntries(TimeSpan.FromHours(1));
```

## CompressionUtil

`CompressionUtil` provides high‑performance helpers for compressing and decompressing data using GZIP while reusing buffers from `ArrayPool<byte>.Shared` to minimise allocations and GC pressure. It also offers utilities to evaluate compression effectiveness.

```csharp
using RedisCachePatterns.Utilities;

string original = "Hello, world!";

// Compress a string to a byte array
byte[] compressed = CompressionUtil.CompressString(original);

// Decompress back to the original string
string roundTrip = CompressionUtil.DecompressString(compressed);

// Compress a raw byte span
byte[] rawData = Encoding.UTF8.GetBytes(original);
byte[] compressedBytes = CompressionUtil.CompressBytes(rawData);

// Decompress the raw bytes
byte[] decompressed = CompressionUtil.DecompressBytes(compressedBytes);

// Evaluate compression ratio and whether it is worthwhile
double ratio = CompressionUtil.GetCompressionRatio(original.Length, compressed.Length);
bool worthwhile = CompressionUtil.IsCompressionWorthwhile(original.Length, compressed.Length);
```

#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Redis cache configuration settings
/// </summary>
public class CacheConfiguration
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public int DatabaseId { get; set; } = 0;
    public int ConnectTimeoutMs { get; set; } = 5000;
    public int SyncTimeoutMs { get; set; } = 5000;
    public bool EnableCompression { get; set; } = false;
    public int MaxCacheSizeBytes { get; set; } = 1024 * 1024 * 100; // 100MB
    public string EvictionPolicy { get; set; } = "allkeys-lru"; // LRU eviction

    public static CacheConfiguration FromEnvironment()
    {
        return new CacheConfiguration
        {
            ConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379",
            DatabaseId = int.TryParse(Environment.GetEnvironmentVariable("REDIS_DATABASE"), out var db) ? db : 0,
            ConnectTimeoutMs = int.TryParse(Environment.GetEnvironmentVariable("REDIS_CONNECT_TIMEOUT"), out var ct) ? ct : 5000,
            SyncTimeoutMs = int.TryParse(Environment.GetEnvironmentVariable("REDIS_SYNC_TIMEOUT"), out var st) ? st : 5000,
            EnableCompression = bool.TryParse(Environment.GetEnvironmentVariable("REDIS_COMPRESSION"), out var comp) && comp,
            MaxCacheSizeBytes = int.TryParse(Environment.GetEnvironmentVariable("REDIS_MAX_SIZE"), out var size) ? size : 1024 * 1024 * 100,
            EvictionPolicy = Environment.GetEnvironmentVariable("REDIS_EVICTION_POLICY") ?? "allkeys-lru"
        };
    }

    public override string ToString()
    {
        return $"CacheConfig[Connection: {ConnectionString}, DB: {DatabaseId}, " +
               $"ConnectTimeout: {ConnectTimeoutMs}ms, SyncTimeout: {SyncTimeoutMs}ms]";
    }
}

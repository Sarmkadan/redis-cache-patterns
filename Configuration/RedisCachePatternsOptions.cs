#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Redis cache configuration settings with validation
/// </summary>
public sealed class RedisCachePatternsOptions
{
    public const string SectionName = "RedisCachePatterns";

    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;

    [Range(0, 16)]
    public int DatabaseId { get; set; } = 0;

    [Range(100, 30000)]
    public int ConnectTimeoutMs { get; set; } = 5000;

    [Range(100, 30000)]
    public int SyncTimeoutMs { get; set; } = 5000;

    public bool EnableCompression { get; set; } = false;

    [Range(1024, int.MaxValue)]
    public int MaxCacheSizeBytes { get; set; } = 1024 * 1024 * 100; // 100MB

    [Required(AllowEmptyStrings = false)]
    public string EvictionPolicy { get; set; } = "allkeys-lru";

    public DistributedInvalidationOptions DistributedInvalidation { get; set; } = new();
}

#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Services;

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Fluent builder for configuring cache service with policies, strategies, and features
/// Enables declarative configuration of caching behavior
/// </summary>
public class CacheConfigurationBuilder
{
    private readonly List<Domain.CachePolicy> _policies = new();
    private TimeSpan _defaultExpiration = TimeSpan.FromHours(1);
    private bool _compressionEnabled = false;
    private int _compressionThreshold = 1024;
    private bool _warmingEnabled = false;
    private bool _monitoringEnabled = false;

    public CacheConfigurationBuilder WithDefaultExpiration(TimeSpan expiration)
    {
        _defaultExpiration = expiration;
        return this;
    }

    public CacheConfigurationBuilder WithDefaultExpiration(int seconds)
    {
        _defaultExpiration = TimeSpan.FromSeconds(seconds);
        return this;
    }

    public CacheConfigurationBuilder AddPolicy(Domain.CachePolicy policy)
    {
        _policies.Add(policy);
        return this;
    }

    public CacheConfigurationBuilder AddPolicy(string keyPattern, TimeSpan expiration)
    {
        _policies.Add(new Domain.CachePolicy
        {
            Key = keyPattern,
            DefaultExpiration = expiration
        });
        return this;
    }

    public CacheConfigurationBuilder EnableCompression(int thresholdBytes = 1024)
    {
        _compressionEnabled = true;
        _compressionThreshold = thresholdBytes;
        return this;
    }

    public CacheConfigurationBuilder EnableWarming()
    {
        _warmingEnabled = true;
        return this;
    }

    public CacheConfigurationBuilder EnableMonitoring()
    {
        _monitoringEnabled = true;
        return this;
    }

    public CacheServiceOptions Build()
    {
        return new CacheServiceOptions
        {
            DefaultExpiration = _defaultExpiration,
            Policies = _policies,
            CompressionEnabled = _compressionEnabled,
            CompressionThresholdBytes = _compressionThreshold,
            WarmingEnabled = _warmingEnabled,
            MonitoringEnabled = _monitoringEnabled
        };
    }
}

/// <summary>
/// Behavior options for the cache service (expiration, policies, features)
/// </summary>
public class CacheServiceOptions
{
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);
    public List<Domain.CachePolicy> Policies { get; set; } = new();
    public bool CompressionEnabled { get; set; }
    public int CompressionThresholdBytes { get; set; } = 1024;
    public bool WarmingEnabled { get; set; }
    public bool MonitoringEnabled { get; set; }

    public override string ToString() =>
        $"Cache options: DefaultExpiration={DefaultExpiration.TotalSeconds:F0}s, " +
        $"Compression={CompressionEnabled}, Warming={WarmingEnabled}, Monitoring={MonitoringEnabled}";
}

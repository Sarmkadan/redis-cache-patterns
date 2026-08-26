namespace RedisCachePatterns.Examples;

/// <summary>
/// Demonstrates cache monitoring, metrics collection, health checks,
/// and observability features for production visibility.
/// </summary>
public class MonitoringAndMetricsExample
{
    private readonly ICacheService _cacheService;
    private readonly CacheMetricsCollector _metricsCollector;
    private readonly HealthCheckService _healthCheck;

    public DateTime Timestamp => DateTime.UtcNow;
    public double HitRate => _metricsCollector.GetHitRateAsync().Result;
    public double MissRate => _metricsCollector.GetMissRateAsync().Result;
    public double AverageResponseTimeMs => _metricsCollector.GetAverageResponseTimeAsync().Result;
    public double MaxResponseTimeMs => _metricsCollector.GetMaxResponseTimeAsync().Result;
    public long TotalKeys => _metricsCollector.GetTotalKeysAsync().Result;
    public double MemoryUsageMb => _metricsCollector.GetEstimatedMemoryAsync().Result;
    public long GetOperations => _metricsCollector.GetGetOperationsAsync().Result;
    public long SetOperations => _metricsCollector.GetSetOperationsAsync().Result;
    public long ErrorCount => _metricsCollector.GetErrorCountAsync().Result;

    public override string ToString() => $"MonitoringAndMetricsExample {{ Timestamp = {Timestamp}, HitRate = {HitRate}, MissRate = {MissRate}, AverageResponseTimeMs = {AverageResponseTimeMs}, MaxResponseTimeMs = {MaxResponseTimeMs}, TotalKeys = {TotalKeys} }}";

    // ... rest of the class remains the same ...
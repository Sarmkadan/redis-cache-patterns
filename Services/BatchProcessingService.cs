#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Services;

/// <summary>
/// Batch processing service for efficient handling of bulk operations
/// Groups operations and processes them in batches to improve performance
/// </summary>
public class BatchProcessingService<T>
{
    private readonly ILogger<BatchProcessingService<T>> _logger;
    private readonly int _batchSize;
    private readonly TimeSpan _flushInterval;
    private readonly Func<List<T>, Task> _processBatchFn;
    private ConcurrentQueue<T> _queue = new();
    private Timer? _flushTimer;

    public BatchProcessingService(
        Func<List<T>, Task> processBatchFn,
        ILogger<BatchProcessingService<T>> logger,
        int batchSize = 100,
        TimeSpan? flushInterval = null)
    {
        _processBatchFn = processBatchFn;
        _logger = logger;
        _batchSize = batchSize;
        _flushInterval = flushInterval ?? TimeSpan.FromSeconds(30);
    }

    public void Enqueue(T item)
    {
        _queue.Enqueue(item);

        if (_queue.Count >= _batchSize)
        {
            _ = FlushAsync();
        }
    }

    public void Start()
    {
        _flushTimer = new Timer(_ => _ = FlushAsync(), null, _flushInterval, _flushInterval);
        _logger.LogInformation("Batch processing service started with batchSize={Size}, interval={IntervalMs}ms",
            _batchSize, _flushInterval.TotalMilliseconds);
    }

    public void Stop()
    {
        _flushTimer?.Dispose();
        _logger.LogInformation("Batch processing service stopped");
    }

    public async Task FlushAsync()
    {
        var batch = new List<T>();
        while (batch.Count < _batchSize && _queue.TryDequeue(out var item))
        {
            batch.Add(item);
        }

        if (batch.Count == 0) return;

        try
        {
            await _processBatchFn(batch);
            _logger.LogInformation("Batch processed: {Count} items", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch of {Count} items", batch.Count);
        }
    }

    public int GetQueueSize() => _queue.Count;

    public void Dispose()
    {
        Stop();
    }
}

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Idempotency helper for ensuring operations are executed only once
/// Prevents duplicate processing of requests in distributed systems
/// </summary>
public class IdempotencyHelper
{
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _records = new();
    private readonly TimeSpan _recordRetention;

    public IdempotencyHelper(TimeSpan? recordRetention = null)
    {
        _recordRetention = recordRetention ?? TimeSpan.FromHours(24);
    }

    /// <summary>
    /// Checks if operation with given idempotency key has been processed
    /// </summary>
    public bool IsProcessed(string idempotencyKey)
    {
        if (_records.TryGetValue(idempotencyKey, out var record))
        {
            return (DateTime.UtcNow - record.ProcessedAt) < _recordRetention;
        }
        return false;
    }

    /// <summary>
    /// Marks operation as processed and stores result for idempotent retrieval
    /// </summary>
    public void MarkAsProcessed<T>(string idempotencyKey, T result)
    {
        _records[idempotencyKey] = new IdempotencyRecord
        {
            Key = idempotencyKey,
            Result = result,
            ProcessedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets previously stored result for idempotent operation
    /// </summary>
    public T? GetResult<T>(string idempotencyKey)
    {
        if (_records.TryGetValue(idempotencyKey, out var record))
        {
            return (T?)record.Result;
        }
        return default;
    }

    /// <summary>
    /// Executes operation idempotently - returns cached result if key was already processed
    /// </summary>
    public async Task<T> ExecuteIdempotentlyAsync<T>(
        string idempotencyKey,
        Func<Task<T>> operation)
    {
        if (IsProcessed(idempotencyKey))
        {
            var cached = GetResult<T>(idempotencyKey);
            if (cached != null) return cached;
        }

        var result = await operation();
        MarkAsProcessed(idempotencyKey, result);
        return result;
    }

    /// <summary>
    /// Cleans up expired idempotency records
    /// </summary>
    public int CleanupExpiredRecords()
    {
        var expiredKeys = _records
            .Where(x => (DateTime.UtcNow - x.Value.ProcessedAt) > _recordRetention)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _records.TryRemove(key, out _);
        }

        return expiredKeys.Count;
    }

    private class IdempotencyRecord
    {
        public string Key { get; set; } = string.Empty;
        public object? Result { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}

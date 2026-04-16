#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Services;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Helper for managing distributed locks with automatic renewal.
/// Implements <see cref="IAsyncDisposable"/> so that an <c>await using</c> block
/// guarantees the lock is released even when an exception is thrown inside the
/// protected section.
/// </summary>
public class DistributedLockHelper : IDisposable, IAsyncDisposable
{
    private readonly ICacheService _cacheService;
    private readonly string _lockKey;
    private readonly string _lockValue;
    private readonly TimeSpan _lockDuration;
    private readonly string _instanceId;
    private bool _isLocked = false;
    private CancellationTokenSource? _renewalTokenSource;

    public DistributedLockHelper(ICacheService cacheService, string lockKey, string? lockValue = null, TimeSpan? duration = null)
    {
        _cacheService = cacheService;
        _lockKey = lockKey;
        _lockValue = lockValue ?? Guid.NewGuid().ToString();
        _lockDuration = duration ?? TimeSpan.FromSeconds(10);
        _instanceId = Environment.MachineName;
    }

    public async Task<bool> AcquireAsync()
    {
        _isLocked = await _cacheService.AcquireLockAsync(_lockKey, _lockValue, _lockDuration);

        if (_isLocked)
        {
            // Start renewal task to keep lock alive
            StartRenewal();
        }

        return _isLocked;
    }

    public async Task<bool> ReleaseAsync()
    {
        StopRenewal();

        if (_isLocked)
        {
            var released = await _cacheService.ReleaseLockAsync(_lockKey, _lockValue);
            _isLocked = false;
            return released;
        }

        return false;
    }

    public bool IsLocked => _isLocked;

    public string LockValue => _lockValue;

    private void StartRenewal()
    {
        _renewalTokenSource = new CancellationTokenSource();
        var renewalInterval = TimeSpan.FromMilliseconds(_lockDuration.TotalMilliseconds * 0.5); // Renew halfway through TTL

        _ = Task.Run(async () =>
        {
            var token = _renewalTokenSource.Token;
            while (!token.IsCancellationRequested && _isLocked)
            {
                try
                {
                    await Task.Delay(renewalInterval, token);
                    if (!token.IsCancellationRequested)
                    {
                        // Hotfix: Check if lock still exists in Redis before attempting renewal
                        // This prevents renewal attempts on locks that were already released or expired
                        var lockStillExists = await _cacheService.ExistsAsync(_lockKey);
                        if (lockStillExists)
                        {
                            await _cacheService.RenewLockAsync(_lockKey, _lockValue, _lockDuration);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        });
    }

    private void StopRenewal()
    {
        _renewalTokenSource?.Cancel();
        _renewalTokenSource?.Dispose();
    }

    /// <summary>
    /// Acquires the lock and executes <paramref name="action"/> in a protected region,
    /// releasing the lock in a <c>finally</c> block regardless of whether the action
    /// succeeds or throws.
    /// </summary>
    /// <param name="action">The delegate to execute while holding the lock.</param>
    /// <returns>
    /// <c>true</c> if the lock was acquired and the action executed;
    /// <c>false</c> if the lock could not be acquired.
    /// </returns>
    public async Task<bool> ExecuteAsync(Func<Task> action)
    {
        if (!await AcquireAsync())
            return false;

        try
        {
            await action();
            return true;
        }
        finally
        {
            await ReleaseAsync();
        }
    }

    /// <summary>
    /// Acquires the lock and executes <paramref name="action"/> in a protected region,
    /// releasing the lock in a <c>finally</c> block regardless of outcome.
    /// </summary>
    /// <typeparam name="TResult">The return type of <paramref name="action"/>.</typeparam>
    /// <param name="action">The delegate to execute while holding the lock.</param>
    /// <returns>
    /// The result of <paramref name="action"/>, or <c>default</c> if the lock was not acquired.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the lock cannot be acquired.</exception>
    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action)
    {
        if (!await AcquireAsync())
            throw new InvalidOperationException($"Could not acquire distributed lock for key: {_lockKey}");

        try
        {
            return await action();
        }
        finally
        {
            await ReleaseAsync();
        }
    }

    /// <summary>
    /// Releases the lock asynchronously. Enables use in <c>await using</c> blocks so the
    /// lock is guaranteed to be released even when exceptions propagate out of the guarded
    /// section.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        StopRenewal();
        if (_isLocked)
        {
            await ReleaseAsync();
        }
    }

    public void Dispose()
    {
        StopRenewal();
        if (_isLocked)
        {
            _ = ReleaseAsync().ConfigureAwait(false);
        }
    }
}

#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Services;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Helper for managing distributed locks with automatic renewal (watchdog pattern).
/// Implements <see cref="IAsyncDisposable"/> so that an <c>await using</c> block
/// guarantees the lock is released even when an exception is thrown inside the
/// protected section.
///
/// <para>
/// This implementation provides automatic lock renewal to prevent work from outliving
/// the lock TTL. The watchdog renews the lock at TTL/3 intervals while the lock is held.
/// If renewal fails (due to network issues, Redis unavailability, or lock expiration),
/// the <see cref="LockLostToken"/> is triggered, allowing callers to abort operations that are no
/// longer protected by the lock.
/// </para>
/// </summary>
/// <remarks>
/// Usage patterns:
/// <list type="bullet">
/// <item><description><c>await using var helper = new DistributedLockHelper(...);</c> - ensures proper disposal</description></item>
/// <item><description>Check <see cref="LockLostToken"/> to detect if lock was lost during operation</description></item>
/// <item><description>Use <see cref="ExecuteAsync"/> for automatic lock management</description></item>
/// </list>
/// </remarks>
public class DistributedLockHelper : IDisposable, IAsyncDisposable
{
    private readonly ICacheService _cacheService;
    private readonly string _lockKey;
    private readonly string _lockValue;
    private readonly TimeSpan _lockDuration;
    private readonly string _instanceId;
    private bool _isLocked = false;
    private CancellationTokenSource? _renewalTokenSource;
    private CancellationTokenSource? _lockLostTokenSource;
    private readonly object _lock = new object();

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

    /// <summary>
    /// Gets a <see cref="CancellationToken"/> that is triggered when the lock is lost due to renewal failure.
    /// This allows callers to abort operations that are no longer protected by the lock.
    /// </summary>
    public CancellationToken LockLostToken => _lockLostTokenSource?.Token ?? CancellationToken.None;

    private void StartRenewal()
    {
        _renewalTokenSource = new CancellationTokenSource();
        _lockLostTokenSource = new CancellationTokenSource();
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
                        // RenewLockAsync is atomic: it only extends the TTL when our lockValue
                        // still matches. No separate ExistsAsync check is needed (that would
                        // introduce its own TOCTOU window between the existence check and the
                        // renewal command).
                        var renewed = await _cacheService.RenewLockAsync(_lockKey, _lockValue, _lockDuration);

                        if (!renewed)
                        {
                            // Lock renewal failed - it was likely lost (expired or stolen)
                            LockLost();
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Renewal failed due to network issues or other errors
                    LockLost();
                    break;
                }
            }
        });
    }

    private void LockLost()
    {
        lock (_lock)
        {
            if (_isLocked && _lockLostTokenSource != null && !_lockLostTokenSource.IsCancellationRequested)
            {
                _isLocked = false;
                _lockLostTokenSource.Cancel();
            }
        }
    }

    private void StopRenewal()
    {
        var tokenSource = _renewalTokenSource;
        _renewalTokenSource = null;
        if (tokenSource == null)
            return;

        tokenSource.Cancel();
        tokenSource.Dispose();
    }

    private void StopLockLostNotification()
    {
        var tokenSource = _lockLostTokenSource;
        _lockLostTokenSource = null;
        if (tokenSource == null)
            return;

        tokenSource.Dispose();
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
    /// Releases the lock asynchronously and stops the renewal watchdog.
    /// Enables use in <c>await using</c> blocks so the lock is guaranteed to be released
    /// even when exceptions propagate out of the guarded section.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous release operation.</returns>
    public async ValueTask DisposeAsync()
    {
        StopRenewal();
        StopLockLostNotification();
        if (_isLocked)
        {
            await ReleaseAsync();
        }
    }

    /// <summary>
    /// Releases the lock synchronously and stops the renewal watchdog.
    /// </summary>
    public void Dispose()
    {
        StopRenewal();
        StopLockLostNotification();
        if (_isLocked)
        {
            _ = ReleaseAsync().ConfigureAwait(false);
        }
    }
}

#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Services;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Helper for managing distributed locks with automatic renewal
/// </summary>
public class DistributedLockHelper : IDisposable
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

    public void Dispose()
    {
        StopRenewal();
        if (_isLocked)
        {
            _ = ReleaseAsync().ConfigureAwait(false);
        }
    }
}

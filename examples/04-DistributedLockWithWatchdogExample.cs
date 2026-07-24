#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;
using System;
using System.Threading.Tasks;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Demonstrates distributed locking with auto-renewal (watchdog pattern).
/// Shows how to use the LockLostToken to detect when a lock is lost and
/// safely abort operations that are no longer protected.
/// </summary>
public class DistributedLockWithWatchdogExample
{
    private readonly ICacheService _cacheService;
    private readonly string _instanceId;

    public DistributedLockWithWatchdogExample(ICacheService cacheService)
    {
        _cacheService = cacheService;
        _instanceId = Environment.MachineName;
    }

    /// <summary>
    /// Processes a long-running operation with lock watchdog protection.
    /// If the lock is lost during processing (due to Redis issues or network problems),
    /// the operation is safely aborted.
    /// </summary>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="workDuration">How long the operation should take.</param>
    /// <returns>True if completed successfully, false if aborted due to lock loss.</returns>
    public async Task<bool> ProcessWithWatchdogAsync(string operationId, TimeSpan workDuration)
    {
        var lockKey = $"operation:{operationId}";
        var lockDuration = TimeSpan.FromSeconds(30); // Initial TTL

        Console.WriteLine($"[{_instanceId}] Starting operation {operationId} with lock watchdog");

        // Create lock helper with watchdog
        await using var lockHelper = new DistributedLockHelper(
            _cacheService,
            lockKey,
            duration: lockDuration
        );

        // Try to acquire the lock
        var acquired = await lockHelper.AcquireAsync();
        if (!acquired)
        {
            Console.WriteLine($"[{_instanceId}] ✗ Failed to acquire lock for {operationId}");
            return false;
        }

        Console.WriteLine($"[{_instanceId}] ✓ Lock acquired for {operationId}");
        Console.WriteLine($"[{_instanceId}] Watchdog will renew lock every {lockDuration.TotalSeconds / 3:F1}s");

        // Check if lock is lost during operation
        if (lockHelper.LockLostToken.IsCancellationRequested)
        {
            Console.WriteLine($"[{_instanceId}] ⚠ Lock was already lost before starting work!");
            return false;
        }

        try
        {
            // Perform the work
            Console.WriteLine($"[{_instanceId}] Starting work on {operationId}...");

            // Simulate work that might take longer than the initial TTL
            await Task.Delay(workDuration);

            // Check periodically if lock was lost
            for (int i = 0; i < 5; i++)
            {
                if (lockHelper.LockLostToken.IsCancellationRequested)
                {
                    Console.WriteLine($"[{_instanceId}] ⚠⚠ LOCK LOST! Aborting operation {operationId}");
                    return false; // Operation was interrupted - lock no longer held
                }

                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            Console.WriteLine($"[{_instanceId}] ✓ Operation {operationId} completed successfully");
            return true;
        }
        catch (OperationCanceledException) when (lockHelper.LockLostToken.IsCancellationRequested)
        {
            Console.WriteLine($"[{_instanceId}] ⚠ Operation cancelled due to lock loss");
            return false;
        }
        finally
        {
            // Lock is automatically released by DisposeAsync
            Console.WriteLine($"[{_instanceId}] Lock released for {operationId}");
        }
    }

    /// <summary>
    /// Safe operation execution with automatic lock management and watchdog.
    /// Uses the ExecuteAsync method which handles acquisition, renewal, and release automatically.
    /// </summary>
    /// <param name="resourceId">The resource identifier to lock.</param>
    /// <returns>True if the operation completed, false if lock acquisition failed.</returns>
    public async Task<bool> SafeProcessResourceAsync(string resourceId)
    {
        var lockKey = $"resource-lock:{resourceId}";
        var lockDuration = TimeSpan.FromSeconds(15);

        Console.WriteLine($"[{_instanceId}] Attempting to process resource {resourceId} with auto-renewal");

        // Use ExecuteAsync for automatic lock management
        // If lock is lost during execution, the method will throw OperationCanceledException
        try
        {
            var success = await lockHelper.ExecuteAsync(async () =>
            {
                Console.WriteLine($"[{_instanceId}] Processing resource {resourceId}...");

                // Simulate work that might outlive the initial TTL
                await Task.Delay(TimeSpan.FromSeconds(25)); // Longer than lock TTL

                Console.WriteLine($"[{_instanceId}] ✓ Resource {resourceId} processed");
            });

            return success;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[{_instanceId}] ✗ Failed to acquire lock: {ex.Message}");
            return false;
        }
        catch (OperationCanceledException) when (lockHelper.LockLostToken.IsCancellationRequested)
        {
            Console.WriteLine($"[{_instanceId}] ⚠⚠ Operation cancelled - lock was lost during execution!");
            return false;
        }
    }

    /// <summary>
    /// Manual watchdog monitoring with custom cancellation.
    /// Shows advanced usage where you monitor lock status and trigger your own cancellation.
    /// </summary>
    public async Task ProcessWithCustomCancellationAsync(string taskId, TimeSpan expectedDuration)
    {
        var lockKey = $"custom-task:{taskId}";
        var lockDuration = TimeSpan.FromSeconds(20);

        Console.WriteLine($"[{_instanceId}] Starting custom task {taskId}");

        await using var lockHelper = new DistributedLockHelper(
            _cacheService,
            lockKey,
            duration: lockDuration
        );

        var acquired = await lockHelper.AcquireAsync();
        if (!acquired)
        {
            Console.WriteLine($"[{_instanceId}] Could not acquire lock");
            return;
        }

        // Create a combined cancellation token that triggers on lock loss
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            CancellationToken.None,
            lockHelper.LockLostToken
        );

        try
        {
            Console.WriteLine($"[{_instanceId}] Task acquired, starting work...");

            // Simulate work
            var workTask = Task.Delay(expectedDuration, cts.Token);

            // Monitor lock status
            while (!workTask.IsCompleted)
            {
                if (cts.IsCancellationRequested)
                {
                    Console.WriteLine($"[{_instanceId}] ⚠ Task cancelled - lock lost!");
                    return;
                }

                await Task.Delay(1000);
            }

            Console.WriteLine($"[{_instanceId}] ✓ Task {taskId} completed");
        }
        catch (TaskCanceledException) when (cts.IsCancellationRequested)
        {
            Console.WriteLine($"[{_instanceId}] Task was cancelled due to lock loss");
        }
        finally
        {
            Console.WriteLine("Task cleanup complete");
        }
    }
}
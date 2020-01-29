#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace RedisCachePatterns.Services;

/// <summary>
/// Extension methods for BatchProcessingService to provide additional functionality
/// </summary>
public static class BatchProcessingServiceExtensions
{
    /// <summary>
    /// Enqueues multiple items to the batch processing service
    /// </summary>
    /// <typeparam name="T">Type of items in the batch</typeparam>
    /// <param name="service">The batch processing service instance</param>
    /// <param name="items">Collection of items to enqueue</param>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/></exception>
    public static void EnqueueRange<T>(this BatchProcessingService<T> service, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            service.Enqueue(item);
        }
    }

    /// <summary>
    /// Gets the current queue size without dequeuing any items
    /// </summary>
    /// <typeparam name="T">Type of items in the batch</typeparam>
    /// <param name="service">The batch processing service instance</param>
    /// <returns>Current number of items in the queue</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/></exception>
    public static int GetQueueSize<T>(this BatchProcessingService<T> service)
    {
        ArgumentNullException.ThrowIfNull(service);

        return service.GetQueueSize();
    }

    /// <summary>
    /// Clears all items from the queue by processing them (no batch processing occurs)
    /// </summary>
    /// <remarks>
    /// This method flushes the queue, which processes all items through the batch processor.
    /// Due to the internal implementation of <see cref="BatchProcessingService{T}"/>, items cannot be
    /// removed from the queue without processing them. If you need to discard items without processing,
    /// consider draining the queue by calling <see cref="FlushAsync"/> directly and ignoring the results.
    /// </remarks>
    /// <typeparam name="T">Type of items in the batch</typeparam>
    /// <param name="service">The batch processing service instance</param>
    /// <returns>Number of items that were processed from the queue</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/></exception>
    public static int ClearQueue<T>(this BatchProcessingService<T> service)
    {
        ArgumentNullException.ThrowIfNull(service);

        var initialSize = service.GetQueueSize();

        if (initialSize == 0)
        {
            return 0;
        }

        // Flush processes all items in the queue
        service.FlushAsync().GetAwaiter().GetResult();

        return initialSize;
    }

    /// <summary>
    /// Enqueues an item only if the queue size is below the specified threshold
    /// </summary>
    /// <typeparam name="T">Type of items in the batch</typeparam>
    /// <param name="service">The batch processing service instance</param>
    /// <param name="item">Item to enqueue</param>
    /// <param name="maxQueueSize">Maximum allowed queue size before rejecting the item</param>
    /// <returns><see langword="true"/> if item was enqueued; <see langword="false"/> if queue is full</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxQueueSize"/> is negative</exception>
    public static bool EnqueueIfBelowThreshold<T>(this BatchProcessingService<T> service, T item, int maxQueueSize)
    {
        ArgumentNullException.ThrowIfNull(service);

        if (maxQueueSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxQueueSize), maxQueueSize, "Max queue size cannot be negative");
        }

        if (service.GetQueueSize() < maxQueueSize)
        {
            service.Enqueue(item);
            return true;
        }

        return false;
    }
}
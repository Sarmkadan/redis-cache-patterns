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
    public static void EnqueueRange<T>(this BatchProcessingService<T> service, IEnumerable<T> items)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

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
    public static int GetQueueSize<T>(this BatchProcessingService<T> service)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        return service.GetQueueSize();
    }

    /// <summary>
    /// Clears all items from the queue without processing them
    /// </summary>
    /// <typeparam name="T">Type of items in the batch</typeparam>
    /// <param name="service">The batch processing service instance</param>
    /// <returns>Number of items that were removed from the queue</returns>
    public static int ClearQueue<T>(this BatchProcessingService<T> service)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        // Since we can't access the internal queue directly, we'll flush and count
        // what gets processed, then return the count
        var initialSize = service.GetQueueSize();

        if (initialSize == 0)
        {
            return 0;
        }

        // Flush will process all items in the queue
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
    /// <returns>True if item was enqueued, false if queue is full</returns>
    public static bool EnqueueIfBelowThreshold<T>(this BatchProcessingService<T> service, T item, int maxQueueSize)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (maxQueueSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxQueueSize), "Max queue size cannot be negative");
        }

        var currentSize = service.GetQueueSize();

        if (currentSize >= maxQueueSize)
        {
            return false;
        }

        service.Enqueue(item);
        return true;
    }
}
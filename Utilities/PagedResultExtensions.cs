#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Extension methods for <see cref="PagedResult{T}"/> from <see cref="PageHelper"/>
/// </summary>
public static class PagedResultExtensions
{
    /// <summary>
    /// Returns true when the current page contains no items
    /// </summary>
    /// <param name="source">The paged result to check</param>
    public static bool IsEmpty<T>(this PagedResult<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Items.Count == 0;
    }

    /// <summary>
    /// Returns true when the current page is the last page (or there are no pages at all)
    /// </summary>
    /// <param name="source">The paged result to check</param>
    public static bool IsLastPage<T>(this PagedResult<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return !source.HasNextPage;
    }

    /// <summary>
    /// Projects each item of the current page while preserving paging metadata
    /// (<see cref="PagedResult{T}.PageNumber"/>, <see cref="PagedResult{T}.PageSize"/>,
    /// <see cref="PagedResult{T}.TotalCount"/>)
    /// </summary>
    /// <typeparam name="T">Source item type</typeparam>
    /// <typeparam name="TResult">Projected item type</typeparam>
    /// <param name="source">The paged result to project</param>
    /// <param name="selector">Projection applied to every item on the current page</param>
    /// <returns>A new paged result with the projected items and the same paging metadata</returns>
    public static PagedResult<TResult> Select<T, TResult>(
        this PagedResult<T> source,
        Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return new PagedResult<TResult>
        {
            Items = source.Items.Select(selector).ToList(),
            PageNumber = source.PageNumber,
            PageSize = source.PageSize,
            TotalCount = source.TotalCount
        };
    }
}

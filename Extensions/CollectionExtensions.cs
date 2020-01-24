#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Extensions;

/// <summary>
/// Extension methods for collections and enumerables
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Determines whether the specified enumerable is null or empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The enumerable to check.</param>
    /// <returns><see langword="true"/> if the enumerable is null or empty; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/></exception>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return !source.Any();
    }

    /// <summary>
    /// Returns an empty enumerable if the source is null; otherwise, returns the source.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The enumerable to check.</param>
    /// <returns>An empty enumerable if <paramref name="source"/> is null; otherwise, the source enumerable.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/></exception>
    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source;
    }

    /// <summary>
    /// Filters out null values from the enumerable.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The enumerable to filter.</param>
    /// <returns>An enumerable containing only non-null values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/></exception>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(item => item is not null)!;
    }

    /// <summary>
    /// Returns distinct elements based on a key selector function.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source.</typeparam>
    /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>An enumerable containing only the first occurrence of each distinct element.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="keySelector"/> is null.</exception>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        var seen = new HashSet<TKey>();
        foreach (var item in source)
        {
            var key = keySelector(item);
            if (seen.Add(key))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Splits the source enumerable into batches of the specified size.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="batchSize">The maximum number of elements per batch. Must be greater than zero.</param>
    /// <returns>An enumerable of batches, each containing at most <paramref name="batchSize"/> elements.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="batchSize"/> is less than or equal to zero.</exception>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero");
        }

        var list = new List<T>(batchSize);
        foreach (var item in source)
        {
            list.Add(item);
            if (list.Count == batchSize)
            {
                yield return list;
                list = new List<T>(batchSize);
            }
        }

        if (list.Count > 0)
        {
            yield return list;
        }
    }

    /// <summary>
    /// Groups elements by a key and returns a dictionary mapping keys to lists of elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source.</typeparam>
    /// <typeparam name="TKey">The type of key to group by.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>A dictionary where each key maps to a list of elements that share that key.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="keySelector"/> is null.</exception>
    public static Dictionary<TKey, List<T>> GroupByToDictionary<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return source
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static readonly Random _random = new Random();

    /// <summary>
    /// Returns a new enumerable with elements in random order.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <returns>A new enumerable containing all elements in random order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/></exception>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var list = source.ToList();
        var count = list.Count;

        for (int i = count - 1; i > 0; i--)
        {
            int randomIndex = _random.Next(i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }

        return list;
    }

    /// <summary>
    /// Returns an enumerable of tuples containing each element and its index.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <returns>An enumerable of value tuples containing the item and its index.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/></exception>
    public static IEnumerable<(T Item, int Index)> WithIndex<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        int index = 0;
        foreach (var item in source)
        {
            yield return (item, index++);
        }
    }
}

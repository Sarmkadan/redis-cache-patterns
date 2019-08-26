// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Pagination utility for handling large result sets
/// Supports offset-based and cursor-based pagination patterns
/// </summary>
public static class PageHelper
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MinPageSize = 1;

    /// <summary>
    /// Validates and normalizes page parameters
    /// </summary>
    public static (int PageNumber, int PageSize) ValidatePaginationParams(int? pageNumber = null, int? pageSize = null)
    {
        var validPageNumber = Math.Max(pageNumber ?? 1, 1);
        var validPageSize = Math.Clamp(pageSize ?? DefaultPageSize, MinPageSize, MaxPageSize);
        return (validPageNumber, validPageSize);
    }

    /// <summary>
    /// Applies pagination to a collection
    /// </summary>
    public static PagedResult<T> Paginate<T>(IEnumerable<T> items, int pageNumber, int pageSize)
    {
        var (validPageNumber, validPageSize) = ValidatePaginationParams(pageNumber, pageSize);
        var itemList = items.ToList();
        var totalCount = itemList.Count;
        var skipCount = (validPageNumber - 1) * validPageSize;

        return new PagedResult<T>
        {
            Items = itemList.Skip(skipCount).Take(validPageSize).ToList(),
            PageNumber = validPageNumber,
            PageSize = validPageSize,
            TotalCount = totalCount,
            TotalPages = (totalCount + validPageSize - 1) / validPageSize
        };
    }

    /// <summary>
    /// Applies pagination with sort order
    /// </summary>
    public static PagedResult<T> Paginate<T>(
        IEnumerable<T> items,
        int pageNumber,
        int pageSize,
        Func<IEnumerable<T>, IEnumerable<T>> sortFn)
    {
        var sorted = sortFn(items);
        return Paginate(sorted, pageNumber, pageSize);
    }

    /// <summary>
    /// Gets offset value for database queries
    /// </summary>
    public static int GetOffset(int pageNumber, int pageSize)
    {
        var (validPageNumber, validPageSize) = ValidatePaginationParams(pageNumber, pageSize);
        return (validPageNumber - 1) * validPageSize;
    }
}

/// <summary>
/// Result of a paginated query
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = PageHelper.DefaultPageSize;
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public override string ToString() =>
        $"Page {PageNumber}/{TotalPages} ({Items.Count}/{TotalCount} items)";
}

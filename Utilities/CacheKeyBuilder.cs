#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Utility for building consistent cache keys.
/// Uses string.Create with Span&lt;char&gt; to construct keys in a single allocation
/// rather than StringBuilder's internal buffer + final ToString copy.
/// </summary>
public static class CacheKeyBuilder
{
    private const char Sep = ':';

    /// <summary>
    /// Builds a colon-delimited cache key from an arbitrary list of parts.
    /// Computes the final length upfront and fills a single allocated string
    /// via Span&lt;char&gt;, avoiding intermediate StringBuilder allocations.
    /// </summary>
    public static string BuildKey(params object?[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (parts.Length == 0) return string.Empty;
        if (parts.Length == 1) return parts[0]?.ToString() ?? "null";

        var segments = new string[parts.Length];
        int totalLen = parts.Length - 1; // one separator between each segment
        for (int i = 0; i < parts.Length; i++)
        {
            segments[i] = parts[i]?.ToString() ?? "null";
            totalLen += segments[i].Length;
        }

        return string.Create(totalLen, segments, static (span, segs) =>
        {
            int pos = 0;
            for (int i = 0; i < segs.Length; i++)
            {
                if (i > 0) span[pos++] = ':';
                segs[i].AsSpan().CopyTo(span[pos..]);
                pos += segs[i].Length;
            }
        });
    }

    // Specific methods use string interpolation which the runtime optimises into
    // a single DefaultInterpolatedStringHandler pass — no boxing, no extra alloc.

    public static string User(int userId) => $"user:{userId}";

    public static string UserByUsername(string username)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);
        return $"user:username:{username}";
    }

    public static string UserByEmail(string email)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);
        return $"user:email:{email}";
    }

    public static string UsersByRole(string role)
    {
        ArgumentException.ThrowIfNullOrEmpty(role);
        return $"users:role:{role}";
    }

    public static string Product(int productId) => $"product:{productId}";

    public static string ProductBySku(string sku)
    {
        ArgumentException.ThrowIfNullOrEmpty(sku);
        return $"product:sku:{sku}";
    }

    public static string ProductsByCategory(string category)
    {
        ArgumentException.ThrowIfNullOrEmpty(category);
        return $"products:category:{category}";
    }

    public static string ProductSearch(string term)
    {
        ArgumentException.ThrowIfNullOrEmpty(term);
        return $"products:search:{term}";
    }

    public static string Order(int orderId) => $"order:{orderId}";

    public static string OrderByNumber(string orderNumber)
    {
        ArgumentException.ThrowIfNullOrEmpty(orderNumber);
        return $"order:number:{orderNumber}";
    }

    public static string OrdersByUser(int userId) => $"orders:user:{userId}";

    public static string OrdersByStatus(string status)
    {
        ArgumentException.ThrowIfNullOrEmpty(status);
        return $"orders:status:{status}";
    }

    public static string Inventory(int inventoryId) => $"inventory:{inventoryId}";

    public static string InventoryByProductAndWarehouse(int productId, string warehouse)
    {
        ArgumentException.ThrowIfNullOrEmpty(warehouse);
        return $"inventory:product:{productId}:warehouse:{warehouse}";
    }

    public static string InventoryByProduct(int productId) => $"inventory:product:{productId}";

    public static string DistributedLock(string lockName)
    {
        ArgumentException.ThrowIfNullOrEmpty(lockName);
        return $"lock:{lockName}";
    }

    public static string GeneratePattern(string prefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(prefix);
        return $"{prefix}:*";
    }
}

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Utility for building consistent cache keys
/// </summary>
public static class CacheKeyBuilder
{
    private const string SEPARATOR = ":";

    public static string BuildKey(params object?[] parts)
    {
        var key = new StringBuilder();
        for (int i = 0; i < parts.Length; i++)
        {
            if (i > 0) key.Append(SEPARATOR);
            key.Append(parts[i]?.ToString() ?? "null");
        }
        return key.ToString();
    }

    public static string User(int userId) => BuildKey("user", userId);

    public static string UserByUsername(string username) => BuildKey("user", "username", username);

    public static string UserByEmail(string email) => BuildKey("user", "email", email);

    public static string UsersByRole(string role) => BuildKey("users", "role", role);

    public static string Product(int productId) => BuildKey("product", productId);

    public static string ProductBySku(string sku) => BuildKey("product", "sku", sku);

    public static string ProductsByCategory(string category) => BuildKey("products", "category", category);

    public static string ProductSearch(string term) => BuildKey("products", "search", term);

    public static string Order(int orderId) => BuildKey("order", orderId);

    public static string OrderByNumber(string orderNumber) => BuildKey("order", "number", orderNumber);

    public static string OrdersByUser(int userId) => BuildKey("orders", "user", userId);

    public static string OrdersByStatus(string status) => BuildKey("orders", "status", status);

    public static string Inventory(int inventoryId) => BuildKey("inventory", inventoryId);

    public static string InventoryByProductAndWarehouse(int productId, string warehouse) =>
        BuildKey("inventory", "product", productId, "warehouse", warehouse);

    public static string InventoryByProduct(int productId) => BuildKey("inventory", "product", productId);

    public static string DistributedLock(string lockName) => BuildKey("lock", lockName);

    public static string GeneratePattern(string prefix) => $"{prefix}:*";
}

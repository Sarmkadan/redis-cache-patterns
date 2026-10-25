# ProductRepositoryExtensions

The `ProductRepositoryExtensions` class is a static collection of extension methods for `ProductRepository`. It adds two category-scoped query helpers: listing low-stock products in a category, and searching products by name within a category. The class is in the `RedisCachePatterns.Infrastructure.Repositories` namespace, the same as `ProductRepository`, so no extra `using` is needed for it. The methods only read data and do not change the repository.

The methods are declared on the concrete `ProductRepository` type, not on `IProductRepository`. A variable typed as `IProductRepository` must be cast to `ProductRepository` before these methods can be called.

## API

### GetLowStockProductsInCategoryAsync

```csharp
public static async Task<IEnumerable<Product>> GetLowStockProductsInCategoryAsync(this ProductRepository repository, string category)
```

Returns the products in one category whose stock is at or below their reorder level. The method calls `GetByCategoryAsync` and filters the result with `Product.IsLowStock()`, which is `true` when `StockQuantity <= ReorderLevel`.

- **repository**: The `ProductRepository` instance.
- **category**: The category to filter by. The match ignores case. Inactive products are excluded by `GetByCategoryAsync`, so they are never returned.
- **Returns**: An enumerable of low-stock products in the category. The enumerable is empty when nothing matches. The filter is deferred; see the notes below.
- **Throws**: `ArgumentNullException` when `repository` is null. `ArgumentNullException` when `category` is null. `ArgumentException` when `category` is empty. See the notes about the XML documentation for the null case.

### SearchProductsByNameInCategoryAsync

```csharp
public static async Task<IEnumerable<Product>> SearchProductsByNameInCategoryAsync(this ProductRepository repository, string category, string searchTerm)
```

Returns the products in one category whose `Name` contains the search term. The method calls `GetByCategoryAsync` and filters the result with `string.Contains` using `StringComparison.OrdinalIgnoreCase`. Only `Name` is searched; `Description` is not.

- **repository**: The `ProductRepository` instance.
- **category**: The category to filter by. The match ignores case. Inactive products are excluded.
- **searchTerm**: The text to look for in product names. It is not trimmed, so leading or trailing spaces count as part of the match. A whitespace-only term passes the check and matches names that contain spaces.
- **Returns**: An enumerable of matching products. The enumerable is empty when nothing matches.
- **Throws**: `ArgumentNullException` when `repository` is null. `ArgumentNullException` when `category` or `searchTerm` is null. `ArgumentException` when `category` or `searchTerm` is empty.

## Usage

### Listing low-stock products for a reorder report

```csharp
using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Repositories;

public class ReorderReport(ProductRepository repository)
{
    public async Task<List<Product>> GetElectronicsToReorderAsync()
    {
        IEnumerable<Product> lowStock = await repository.GetLowStockProductsInCategoryAsync("Electronics");
        return lowStock.ToList();
    }
}
```

The `ToList()` call takes a snapshot. Without it, the filter runs again every time the result is enumerated.

### Searching products by name in a category

```csharp
IEnumerable<Product> matches = await repository.SearchProductsByNameInCategoryAsync("Electronics", "laptop");

foreach (Product product in matches)
{
    Console.WriteLine($"{product.Name}: {product.Price}");
}
```

A product whose `Description` mentions "laptop" but whose `Name` does not will not be returned. `ProductRepository.SearchByNameAsync` searches both fields, but it is not limited to one category.

## Notes

*   **Exceptions are deferred:** The methods are `async`, so argument checks do not throw when the method is called. The exception is stored in the returned `Task` and thrown when the task is awaited.
*   **Null `category` throws `ArgumentNullException`:** The XML documentation of `GetLowStockProductsInCategoryAsync` and `SearchProductsByNameInCategoryAsync` says that a null or empty argument throws `ArgumentException`. The code calls `ArgumentException.ThrowIfNullOrEmpty`, which throws `ArgumentNullException` for null and `ArgumentException` for empty. Callers that catch only `ArgumentException` will miss the null case.
*   **Low-stock results are filtered lazily:** `GetLowStockProductsInCategoryAsync` returns a LINQ `Where` over the list loaded by `GetByCategoryAsync`. `IsLowStock()` runs each time the result is enumerated, so a stock change made between the call and the enumeration is reflected in the result. Call `ToList()` to take a snapshot.
*   **Category and name matching ignore case:** `GetByCategoryAsync` compares categories with `StringComparison.OrdinalIgnoreCase`, and the name search uses the same comparison, so `electronics` and `Electronics` select the same products.
*   **Only active products are returned:** Both methods build on `GetByCategoryAsync`, which keeps only products where `IsActive` is `true`.
*   **Name-only search:** `SearchProductsByNameInCategoryAsync` does not search `Description`, which differs from `ProductRepository.SearchByNameAsync`.
*   **Filtering happens in memory:** The repository keeps its data in memory (`_data`, guarded by `_lock`), so the extensions add no I/O of their own.
*   **No caching:** The extensions do not read from or write to Redis. Nothing in the current tree calls them, so they have no caller yet.

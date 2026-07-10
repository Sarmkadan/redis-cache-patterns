# CollectionExtensions

A static class that provides a set of convenience extension methods for working with `IEnumerable<T>` instances. The methods simplify common patterns such as null‑checking, filtering, batching, grouping, shuffling, and indexing while preserving lazy evaluation where appropriate.

## API

### `public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)`
- **Purpose**: Determines whether the supplied sequence is `null` or contains no elements.
- **Parameters**: 
  - `source`: The sequence to test.
- **Return value**: `true` if `source` is `null` or empty; otherwise `false`.
- **Exceptions**: None.

### `public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)`
- **Purpose**: Returns the original sequence if it is not `null`; otherwise returns an empty enumerable of the same type.
- **Parameters**: 
  - `source`: The sequence that may be `null`.
- **Return value**: `source` when non‑`null`; otherwise `Enumerable.Empty<T>()`.
- **Exceptions**: None.

### `public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source) where T : class`
- **Purpose**: Filters out `null` elements from the sequence.
- **Parameters**: 
  - `source`: The sequence to filter.
- **Return value**: An `IEnumerable<T>` containing only the non‑`null` items from `source`.
- **Exceptions**: 
  - `ArgumentNullException` if `source` is `null`.

### `public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)`
- **Purpose**: Returns distinct elements based on a key selector, preserving the first occurrence of each key.
- **Parameters**: 
  - `source`: The sequence to de‑duplicate.
  - `keySelector`: A function that extracts the key used for comparison.
- **Return value**: An `IEnumerable<T>` with duplicate keys removed.
- **Exceptions**: 
  - `ArgumentNullException` if `source` or `keySelector` is `null`.

### `public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)`
- **Purpose**: Partitions the input sequence into consecutive batches of a specified size.
- **Parameters**: 
  - `source`: The sequence to batch.
  - `size`: The maximum number of elements per batch (must be > 0).
- **Return value**: An `IEnumerable<IEnumerable<T>>` where each inner enumerable yields up to `size` elements.
- **Exceptions**: 
  - `ArgumentNullException` if `source` is `null`.
  - `ArgumentOutOfRangeException` if `size` is less than or equal to zero.

### `public static Dictionary<TKey, List<T>> GroupByToDictionary<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)`
- **Purpose**: Groups elements by a key and places each group into a `List<T>` stored in a dictionary.
- **Parameters**: 
  - `source`: The sequence to group.
  - `keySelector`: A function that extracts the grouping key.
- **Return value**: A `Dictionary<TKey, List<T>>` mapping each distinct key to a list of the corresponding elements.
- **Exceptions**: 
  - `ArgumentNullException` if `source` or `keySelector` is `null`.

### `public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)`
- **Purpose**: Returns the elements of the sequence in a random order (Fisher‑Yates shuffle).
- **Parameters**: 
  - `source`: The sequence to shuffle.
- **Return value**: An `IEnumerable<T>` yielding the shuffled elements.
- **Exceptions**: 
  - `ArgumentNullException` if `source` is `null`.

### `public static IEnumerable<(T Item, int Index)> WithIndex<T>(this IEnumerable<T> source)`
- **Purpose**: Projects each element into a tuple containing the element and its zero‑based index.
- **Parameters**: 
  - `source`: The sequence to index.
- **Return value**: An `IEnumerable<(T Item, int Index)>` where `Item` is the element and `Index` is its position.
- **Exceptions**: 
  - `ArgumentNullException` if `source` is `null`.

## Usage

```csharp
using System.Collections.Generic;
using System.Linq;

// Example 1: Guard against null collections and safely batch items.
IEnumerable<string> names = GetNamesFromSource(); // may return null
var safeNames = names.EmptyIfNull();               // never null
var batches = safeNames.Batch(3);                  // IEnumerable<IEnumerable<string>>

foreach (var batch in batches)
{
    Console.WriteLine(string.Join(", ", batch));
}
```

```csharp
using System.Collections.Generic;

// Example 2: Remove nulls, get distinct values by a property, and enumerate with index.
List<Product> products = GetProducts(); // may contain nulls
var indexed = products
    .WhereNotNull()
    .DistinctBy(p => p.Sku)
    .WithIndex();

foreach (var (item, index) in indexed)
{
    Console.WriteLine($"{index}: {item.Name} (SKU: {item.Sku})");
}
```

## Notes

- All extension methods are **stateless**; they do not retain references to the source sequence beyond the duration of enumeration. Consequently, they are thread‑safe as long as the underlying `source` is not modified concurrently while being enumerated.
- Methods that return `IEnumerable<T>` (`EmptyIfNull`, `WhereNotNull`, `DistinctBy`, `Batch`, `Shuffle`, `WithIndex`) use **deferred execution**; the actual work occurs when the returned sequence is iterated.
- `DistinctBy` buffers encountered keys in a `HashSet<TKey>` to achieve O(1) look‑ups, which may cause significant memory consumption for large sequences with many distinct keys.
- `Shuffle` materializes the source into an array internally to perform the Fisher‑Yates algorithm, thus enumerating the source immediately despite returning a lazy‑looking sequence.
- `Batch` does not copy elements; each batch enumerates a slice of the original sequence. If the source is mutated after a batch has been enumerated but before another batch is consumed, the observed contents may change.
- `GroupByToDictionary` eagerly builds the dictionary and lists; therefore the entire source is consumed upon the first call to the method. 
- When `source` is `null`, methods that check for null (`WhereNotNull`, `DistinctBy`, `Batch`, `GroupByToDictionary`, `Shuffle`, `WithIndex`) throw `ArgumentNullException`; `IsNullOrEmpty` and `EmptyIfNull` handle null gracefully without throwing. 
- The generic type constraints are intentionally minimal; where a method requires reference‑type semantics (e.g., `WhereNotNull`), the constraint `where T : class` is applied. Other methods work for both reference and value types.

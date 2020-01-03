# PageHelper

`PageHelper` is a utility class that provides methods and properties for paginating collections of data. It simplifies the process of splitting large datasets into discrete pages, validating pagination parameters, and calculating offsets for database queries. The class is designed to work with in-memory collections and supports generic types for flexibility.

## API

### `ValidatePaginationParams`

Validates pagination parameters to ensure they are within acceptable bounds.

- **Parameters**
  - `pageNumber` (int): The requested page number (1-based).
  - `pageSize` (int): The number of items per page.
- **Return Value**
  - A tuple `(int PageNumber, int PageSize)` where both values are adjusted to be within valid ranges (pageNumber ≥ 1, pageSize ≥ 1 and ≤ 1000).
- **Throws**
  - `ArgumentOutOfRangeException`: If `pageSize` is less than 1 or greater than 1000.

### `Paginate<T>(IEnumerable<T> source, int pageNumber, int pageSize)`

Paginates an in-memory collection of items.

- **Parameters**
  - `source` (IEnumerable<T>): The collection to paginate.
  - `pageNumber` (int): The requested page number (1-based).
  - `pageSize` (int): The number of items per page.
- **Return Value**
  - A `PagedResult<T>` containing the paginated items, along with metadata such as total count, page number, and page size.
- **Throws**
  - `ArgumentNullException`: If `source` is `null`.
  - `ArgumentOutOfRangeException`: If `pageNumber` or `pageSize` are invalid (see `ValidatePaginationParams`).

### `Paginate<T>(IEnumerable<T> source, int offset, int pageSize)`

Paginates an in-memory collection of items using an offset-based approach.

- **Parameters**
  - `source` (IEnumerable<T>): The collection to paginate.
  - `offset` (int): The number of items to skip before starting the current page.
  - `pageSize` (int): The number of items per page.
- **Return Value**
  - A `PagedResult<T>` containing the paginated items, along with metadata such as total count, page number, and page size.
- **Throws**
  - `ArgumentNullException`: If `source` is `null`.
  - `ArgumentOutOfRangeException`: If `pageSize` is less than 1 or greater than 1000, or if `offset` is negative.

### `GetOffset(int pageNumber, int pageSize)`

Calculates the offset (number of items to skip) for a given page number and page size.

- **Parameters**
  - `pageNumber` (int): The requested page number (1-based).
  - `pageSize` (int): The number of items per page.
- **Return Value**
  - The calculated offset (0-based index).
- **Throws**
  - `ArgumentOutOfRangeException`: If `pageNumber` or `pageSize` are invalid (see `ValidatePaginationParams`).

### `Items` (List<T>)

Gets the list of items for the current page.

- **Type**
  - `List<T>`
- **Remarks**
  - This property is part of the `PagedResult<T>` return type and is read-only.

### `PageNumber` (int)

Gets the current page number (1-based).

- **Type**
  - `int`
- **Remarks**
  - This property is part of the `PagedResult<T>` return type and is read-only.

### `PageSize` (int)

Gets the number of items per page.

- **Type**
  - `int`
- **Remarks**
  - This property is part of the `PagedResult<T>` return type and is read-only.

### `TotalCount` (int)

Gets the total number of items in the source collection.

- **Type**
  - `int`
- **Remarks**
  - This property is part of the `PagedResult<T>` return type and is read-only.

### `ToString()`

Returns a string representation of the paginated result.

- **Return Value**
  - A string in the format `"Page {PageNumber} of {TotalPages} (PageSize={PageSize}, TotalCount={TotalCount})"`.
- **Remarks**
  - This method is overridden from `object` and is part of the `PagedResult<T>` return type.

## Usage

### Example 1: Basic Pagination

```csharp
var items = Enumerable.Range(1, 100).ToList(); // Simulate a source collection
var result = PageHelper.Paginate(items, pageNumber: 2, pageSize: 10);

Console.WriteLine(result.ToString());
// Output: "Page 2 of 10 (PageSize=10, TotalCount=100)"

foreach (var item in result.Items)
{
    Console.WriteLine(item);
}
```

### Example 2: Offset-Based Pagination

```csharp
var items = Enumerable.Range(1, 100).ToList(); // Simulate a source collection
var result = PageHelper.Paginate(items, offset: 20, pageSize: 10);

Console.WriteLine(result.ToString());
// Output: "Page 3 of 10 (PageSize=10, TotalCount=100)"

foreach (var item in result.Items)
{
    Console.WriteLine(item);
}
```

## Notes

- **Edge Cases**:
  - If `pageNumber` exceeds the total number of pages, the last page is returned.
  - If `pageSize` is larger than the total number of items, a single page is returned.
  - Empty collections result in a `PagedResult<T>` with `TotalCount = 0` and an empty `Items` list.

- **Thread Safety**:
  - The methods and properties are stateless and do not maintain any shared mutable state. They are safe to use concurrently from multiple threads. However, the `PagedResult<T>` return type contains a `List<T>` which is not thread-safe if modified after creation. Consumers should treat the returned `Items` list as read-only.

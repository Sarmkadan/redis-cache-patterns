# InvalidateKeyRequestExtensions

Extension methods for `InvalidateKeyRequest` that provide a fluent interface for constructing cache invalidation requests with common patterns, such as invalidating keys by user, product, or session, or specifying manual purge reasons and sources.

## API

### `WithCacheKey`
Adds or replaces the cache key to be invalidated in the request.

- **Parameters**
  - `request` (`InvalidateKeyRequest`): The request to modify.
  - `cacheKey` (`string`): The cache key to invalidate.
- **Return Value**
  - `InvalidateKeyRequest`: The modified request for fluent chaining.
- **Throws**
  - `ArgumentNullException`: If `request` is `null`.
  - `ArgumentNullException`: If `cacheKey` is `null`.

---

### `ForUser`
Marks the request to invalidate all cache keys associated with a specific user.

- **Parameters**
  - `request` (`InvalidateKeyRequest`): The request to modify.
  - `userId` (`string`): The user identifier.
- **Return Value**
  - `InvalidateKeyRequest`: The modified request for fluent chaining.
- **Throws**
  - `ArgumentNullException`: If `request` is `null`.
  - `ArgumentNullException`: If `userId` is `null`.

---

### `ForProduct`
Marks the request to invalidate all cache keys associated with a specific product.

- **Parameters**
  - `request` (`InvalidateKeyRequest`): The request to modify.
  - `productId` (`string`): The product identifier.
- **Return Value**
  - `InvalidateKeyRequest`: The modified request for fluent chaining.
- **Throws**
  - `ArgumentNullException`: If `request` is `null`.
  - `ArgumentNullException`: If `productId` is `null`.

---
### `ForSession`
Marks the request to invalidate all cache keys associated with a specific session.

- **Parameters**
  - `request` (`InvalidateKeyRequest`): The request to modify.
  - `sessionId` (`string`): The session identifier.
- **Return Value**
  - `InvalidateKeyRequest`: The modified request for fluent chaining.
- **Throws**
  - `ArgumentNullException`: If `request` is `null`.
  - `ArgumentNullException`: If `sessionId` is `null`.

---
### `WithReason`
Specifies the reason for the manual cache purge.

- **Parameters**
  - `request` (`InvalidateKeyRequest`): The request to modify.
  - `reason` (`string`): The reason for the purge.
- **Return Value**
  - `InvalidateKeyRequest`: The modified request for fluent chaining.
- **Throws**
  - `ArgumentNullException`: If `request` is `null`.
  - `ArgumentNullException`: If `reason` is `null`.

---
### `WithSource`
Specifies the source of the cache invalidation request.

- **Parameters**
  - `request` (`InvalidateKeyRequest`): The request to modify.
  - `source` (`string`): The source of the invalidation.
- **Return Value**
  - `InvalidateKeyRequest`: The modified request for fluent chaining.
- **Throws**
  - `ArgumentNullException`: If `request` is `null`.
  - `ArgumentNullException`: If `source` is `null`.

---
### `IsManualPurge`
Determines whether the request represents a manual cache purge.

- **Parameters**
  - `request` (`InvalidateKeyRequest`): The request to evaluate.
- **Return Value**
  - `bool`: `true` if the request is a manual purge; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `request` is `null`.

---
### `IsDataUpdate`
Determines whether the request represents a data update that may require cache invalidation.

- **Parameters**
  - `request` (`InvalidateKeyRequest`): The request to evaluate.
- **Return Value**
  - `bool`: `true` if the request is a data update; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `request` is `null`.

## Usage

### Example 1: Invalidate a specific cache key

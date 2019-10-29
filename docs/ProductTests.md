# ProductTests

Unit tests for `Product` domain logic, covering stock management, pricing, ratings, and availability checks. These tests validate business rules around reorder levels, price updates, discount calculations, and activation states.

## API

### `IsLowStock_WhenStockEqualsReorderLevel_ReturnsTrue`
Verifies that a product is considered low stock when its current stock exactly matches the reorder level. No parameters or return value beyond the boolean assertion.

### `IsLowStock_WhenStockExceedsReorderLevel_ReturnsFalse`
Confirms that a product is not low stock when its stock exceeds the reorder level. No parameters or return value beyond the boolean assertion.

### `UpdateStock_WhenReductionExceedsAvailable_ThrowsInvalidOperationException`
Ensures that reducing stock below zero throws an `InvalidOperationException`. No parameters or return value beyond the exception assertion.

### `UpdateStock_WithPositiveQuantity_IncreasesStockAndSetsTimestamp`
Validates that adding a positive quantity increases the stock and updates the last modified timestamp. No parameters or return value beyond the state assertions.

### `UpdatePrice_WithNegativeValue_ThrowsArgumentException`
Confirms that setting a negative price throws an `ArgumentException`. No parameters or return value beyond the exception assertion.

### `UpdatePrice_WithValidValue_UpdatesPriceAndSetsTimestamp`
Validates that a positive price update modifies the price and updates the last modified timestamp. No parameters or return value beyond the state assertions.

### `CalculateDiscount_With10PercentOff_ReturnsNinetyPercentOfPrice`
Checks that a 10% discount reduces the price to 90% of its original value. No parameters or return value beyond the numeric assertion.

### `CalculateDiscount_WithZeroPercent_ReturnsOriginalPrice`
Ensures that a 0% discount leaves the price unchanged. No parameters or return value beyond the numeric assertion.

### `SetRating_WithValueAboveFive_ThrowsArgumentException`
Confirms that setting a rating above 5 throws an `ArgumentException`. No parameters or return value beyond the exception assertion.

### `SetRating_WithNegativeValue_ThrowsArgumentException`
Validates that setting a negative rating throws an `ArgumentException`. No parameters or return value beyond the exception assertion.

### `IsAvailable_WhenDeactivated_ReturnsFalseRegardlessOfStock`
Ensures that a deactivated product is unavailable regardless of stock levels. No parameters or return value beyond the boolean assertion.

### `IsAvailable_WhenActiveWithPositiveStock_ReturnsTrue`
Confirms that an active product with positive stock is available. No parameters or return value beyond the boolean assertion.

### `Activate_AfterDeactivate_RestoresAvailability`
Validates that reactivating a product restores its availability state. No parameters or return value beyond the state assertions.

## Usage

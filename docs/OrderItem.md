# OrderItem

Represents a line item in an order, tracking product details, pricing, and discounting logic. Used to persist order contents in a cache-friendly structure while maintaining business rules for discounts and quantity adjustments.

## API

### `Id`
Gets the unique identifier for this order item. Read-only; set during construction or persistence.

### `OrderId`
Gets or sets the identifier of the parent order. Must match the owning order’s identifier when persisted.

### `ProductId`
Gets or sets the identifier of the associated product. Must reference a valid product in the system.

### `Product`
Gets or sets the product details for this item. May be `null` if the product is unavailable or not loaded.

### `Quantity`
Gets or sets the number of units of the product in this order item. Must be a positive integer.

### `UnitPrice`
Gets or sets the base price per unit of the product at the time of order. Must be a non-negative decimal.

### `DiscountPercent`
Gets or sets the discount percentage applied to this item. Must be between `0` and `100` inclusive.

### `AddedAt`
Gets or sets the timestamp when this item was added to the order. Typically set during construction.

### `GetDiscountAmount()`
Calculates and returns the monetary discount applied to this item based on `UnitPrice`, `Quantity`, and `DiscountPercent`.

**Returns**
`decimal` – The total discount amount for this item.

**Throws**
`InvalidOperationException` – If `UnitPrice` or `Quantity` is negative.

### `ApplyDiscount(decimal discountPercent)`
Applies a new discount percentage to this item, updating `DiscountPercent` and recalculating derived values.

**Parameters**
- `discountPercent` (`decimal`) – The new discount percentage to apply. Must be between `0` and `100`.

**Throws**
`ArgumentOutOfRangeException` – If `discountPercent` is outside the valid range.

### `UpdateQuantity(int newQuantity)`
Updates the quantity of this item, ensuring it remains a positive integer.

**Parameters**
- `newQuantity` (`int`) – The new quantity to set. Must be positive.

**Throws**
`ArgumentOutOfRangeException` – If `newQuantity` is zero or negative.

### `ToString()`
Returns a string representation of this order item, including product identifier, quantity, unit price, and discount details.

**Returns**
`string` – A human-readable summary of the item.

## Usage

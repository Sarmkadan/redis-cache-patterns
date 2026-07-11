# OrderItemExtensions

The `OrderItemExtensions` static class provides a set of extension methods for the `OrderItem` type, simplifying common calculations and formatting tasks required during order processing and checkout workflows. These methods encapsulate logic for tax calculation, discount tracking, price presentation, and shipping eligibility checks.

## API

### GetTotalPriceWithTax
Calculates the total price of an order item including the specified tax rate.

- **Signature**: `public static decimal GetTotalPriceWithTax(this OrderItem item, decimal taxRate)`
- **Parameters**:
  - `item`: The `OrderItem` instance.
  - `taxRate`: The tax rate as a decimal (e.g., 0.08m for 8%).
- **Returns**: The total price including tax.
- **Throws**: `ArgumentNullException` if `item` is null.

### GetTotalSavings
Calculates the total monetary savings for an order item based on price reductions.

- **Signature**: `public static decimal GetTotalSavings(this OrderItem item)`
- **Parameters**:
  - `item`: The `OrderItem` instance.
- **Returns**: The total amount saved.
- **Throws**: `ArgumentNullException` if `item` is null.

### FormatPriceBreakdown
Generates a human-readable string representation of the order item's price breakdown, including base price and applied discounts.

- **Signature**: `public static string FormatPriceBreakdown(this OrderItem item)`
- **Parameters**:
  - `item`: The `OrderItem` instance.
- **Returns**: A formatted string detailing the price components.
- **Throws**: `ArgumentNullException` if `item` is null.

### QualifiesForFreeShipping
Determines whether an order item qualifies for free shipping based on a provided threshold.

- **Signature**: `public static bool QualifiesForFreeShipping(this OrderItem item, decimal freeShippingThreshold)`
- **Parameters**:
  - `item`: The `OrderItem` instance.
  - `freeShippingThreshold`: The minimum price required to qualify for free shipping.
- **Returns**: `true` if the item price meets or exceeds the threshold; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `item` is null.

## Usage

### Example 1: Calculating Final Price and Checking Shipping Eligibility

```csharp
var item = new OrderItem { Price = 50.00m, Discount = 5.00m };
decimal taxRate = 0.07m;

// Calculate total with tax
decimal finalPrice = item.GetTotalPriceWithTax(taxRate);

// Check for free shipping
bool freeShipping = item.QualifiesForFreeShipping(45.00m);

Console.WriteLine($"Final Price: {finalPrice:C}");
Console.WriteLine($"Free Shipping: {freeShipping}");
```

### Example 2: Displaying Price Information

```csharp
var item = new OrderItem { Price = 100.00m, Discount = 20.00m };

// Get formatted breakdown
string breakdown = item.FormatPriceBreakdown();

// Get total savings
decimal savings = item.GetTotalSavings();

Console.WriteLine($"Breakdown: {breakdown}");
Console.WriteLine($"Total Savings: {savings:C}");
```

## Notes

- **Edge Cases**: All methods throw an `ArgumentNullException` if the `OrderItem` instance is null. Ensure proper null checking before calling these extensions.
- **Thread Safety**: These extension methods are stateless and do not modify the `OrderItem` instance itself; they are thread-safe, provided the `OrderItem` object being accessed is not concurrently modified by other threads.

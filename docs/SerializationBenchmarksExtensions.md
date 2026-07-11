# SerializationBenchmarksExtensions

`SerializationBenchmarksExtensions` provides a set of static utility methods designed to facilitate the benchmarking and validation of serialization and deserialization processes for domain entities within the `redis-cache-patterns` project. These methods offer standardized routines for serializing collections of products and performing round-trip validation, ensuring data integrity across different serialization scenarios.

## API

### SerializeProductMultiple
Serializes an `IEnumerable<Product>` collection into a serialized string representation.

*   **Parameters:** `IEnumerable<Product> products` - The collection of products to serialize.
*   **Return Value:** A `string` containing the serialized representation of the input collection.
*   **Exceptions:** Throws `ArgumentNullException` if the `products` parameter is null.

### DeserializeProductMultiple
Deserializes a string representation back into a `Product` instance.

*   **Parameters:** `string data` - The serialized string to deserialize.
*   **Return Value:** A `Product?` instance, or `null` if the input string does not contain valid product data.
*   **Exceptions:** Throws `JsonException` or equivalent serialization exceptions if the input string format is invalid.

### RoundTripProduct
Performs a round-trip serialization and deserialization on a `Product` instance to verify data integrity.

*   **Parameters:** `Product product` - The product instance to validate.
*   **Return Value:** A `Product?` instance resulting from the serialization and subsequent deserialization.
*   **Exceptions:** Throws `InvalidOperationException` if the round-trip process fails to produce a valid product instance.

### RoundTripOrder
Performs a round-trip serialization and deserialization on an `Order` instance to verify data integrity.

*   **Parameters:** `Order order` - The order instance to validate.
*   **Return Value:** An `Order?` instance resulting from the serialization and subsequent deserialization.
*   **Exceptions:** Throws `InvalidOperationException` if the round-trip process fails to produce a valid order instance.

## Usage

```csharp
// Example 1: Serializing and deserializing multiple products
var products = new List<Product> { new Product(1, "Widget"), new Product(2, "Gadget") };
string serializedData = SerializationBenchmarksExtensions.SerializeProductMultiple(products);
Product? deserializedProduct = SerializationBenchmarksExtensions.DeserializeProductMultiple(serializedData);

// Example 2: Verifying round-trip integrity for an order
var order = new Order(101, DateTime.UtcNow);
Order? validatedOrder = SerializationBenchmarksExtensions.RoundTripOrder(order);

if (validatedOrder != null && validatedOrder.Id == order.Id)
{
    Console.WriteLine("Order integrity verified.");
}
```

## Notes

*   **Thread Safety:** These methods are implemented as static, stateless operations. Provided that the underlying serialization libraries and domain model objects are used correctly, these methods are thread-safe and can be invoked concurrently.
*   **Edge Cases:**
    *   Passing a null `IEnumerable<Product>` to `SerializeProductMultiple` will result in an `ArgumentNullException`.
    *   Providing an empty string or malformed data to deserialization methods may result in `null` returns or exceptions, depending on the specific serialization implementation used.
    *   Round-trip operations rely on the completeness of the `Product` and `Order` models; ensure that all necessary properties are correctly handled by the serializer to avoid data loss during the round-trip process.

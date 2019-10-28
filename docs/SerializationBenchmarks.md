# SerializationBenchmarks

A utility class for benchmarking serialization and deserialization performance of `Product` and `Order` objects, typically used to evaluate efficiency of different serialization strategies in high-throughput caching scenarios.

## API

### `Setup`
Initializes the benchmarking environment, including any required serializers or test data. This method should be called once before running any serialization or deserialization benchmarks.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: May throw if initialization fails (e.g., serializer setup error, missing dependencies).

---

### `SerializeProduct`
Serializes a `Product` instance into a string representation using the configured serializer.

- **Parameters**:
  - `product` (`Product`): The product instance to serialize.
- **Return value**: `string` – The serialized string representation of the product.
- **Throws**:
  - `ArgumentNullException` if `product` is `null`.
  - May throw serialization-specific exceptions (e.g., `JsonException`, `NotSupportedException`) on failure.

---

### `DeserializeProduct`
Deserializes a string back into a `Product` instance.

- **Parameters**:
  - `serialized` (`string`): The serialized string to deserialize.
- **Return value**: `Product?` – The deserialized `Product` instance, or `null` if deserialization fails or input is invalid.
- **Throws**: May throw serialization-specific exceptions during parsing (e.g., `JsonException`), but returns `null` on logical deserialization failure.

---

### `SerializeOrder`
Serializes an `Order` instance into a string representation using the configured serializer.

- **Parameters**:
  - `order` (`Order`): The order instance to serialize.
- **Return value**: `string` – The serialized string representation of the order.
- **Throws**:
  - `ArgumentNullException` if `order` is `null`.
  - May throw serialization-specific exceptions on failure.

---
### `DeserializeOrder`
Deserializes a string back into an `Order` instance.

- **Parameters**:
  - `serialized` (`string`): The serialized string to deserialize.
- **Return value**: `Order?` – The deserialized `Order` instance, or `null` if deserialization fails or input is invalid.
- **Throws**: May throw serialization-specific exceptions during parsing, but returns `null` on logical deserialization failure.

## Usage

### Example 1: Basic Serialization and Deserialization

# ProductCommand

`ProductCommand` implements product-management operations for the command-line interface. It dispatches product subcommands to a `ProductService`, writes successful results to standard output, and writes validation or operation errors to standard error.

## API

### `public ProductCommand(ProductService productService, ILogger<ProductCommand> logger)`

Creates a product command handler.

- **Parameters**:
  - `productService`: The service used to create, retrieve, update, delete, and query products.
  - `logger`: The logger used to record operation failures.

### `public async Task<int> ExecuteAsync(Dictionary<string, string> options)`

Executes the subcommand specified by the `subcommand` option. If `subcommand` is absent, `list` is used. Subcommand names are matched without regard to case.

- **Parameters**:
  - `options`: Command options keyed by their names without leading dashes.
- **Return value**:
  - `0` when the selected operation completes successfully.
  - `1` for missing required input, an unknown subcommand, a missing product during update, or an operation failure.
- **Exceptions**:
  - The method expects a non-null options dictionary. Passing `null` fails before a subcommand can be executed.

## Subcommands

| Subcommand | Options | Behavior |
| --- | --- | --- |
| `list` | None | Prints a product-table heading and the placeholder text `(Cached product listing)`. This is the default subcommand; it does not currently retrieve or display products. |
| `low-stock` | None | Retrieves low-stock products and prints each product's ID, name, current stock quantity, and reorder level, followed by the total count. |
| `create` | `name`, `sku`, and `price` (required); `description`, `category`, `stock`, and `reorder` (optional) | Creates a product and prints its assigned ID and SKU. |
| `update` | `id` (required); `price` and `stock` (optional) | Retrieves the product, applies valid supplied changes, persists it, and prints a success message. |
| `delete` | `id` (required) | Requests deletion of the product and prints a deletion message. |

### `create` option handling

`name` and `sku` must be present, and `price` must parse as a `decimal` using the current culture. The command does not reject empty `name` or `sku` values when those keys are present.

- `description` and `category` default to empty strings.
- `stock` defaults to `0`; a non-integer value also becomes `0`.
- `reorder` defaults to `10`; a non-integer value also becomes `10`.

### `update` option handling

`id` must parse as an `int`. If the service cannot find that product, the command prints `Product {id} not found` to standard output and returns `1`.

When supplied, `price` is applied only if it parses as a `decimal`, and `stock` is applied only if it parses as an `int`. An invalid optional value is silently ignored. The `stock` option represents the desired final quantity: the command subtracts the current quantity and passes the resulting delta to `Product.UpdateStock`.

## Usage

The options passed by `CommandParser` do not include leading dashes. For example, a parsed invocation such as `product --subcommand create --name Keyboard --sku KB-100 --price 49.99` produces the dictionary used below.

```csharp
var command = new ProductCommand(productService, logger);

var exitCode = await command.ExecuteAsync(new Dictionary<string, string>
{
    ["subcommand"] = "create",
    ["name"] = "Keyboard",
    ["sku"] = "KB-100",
    ["price"] = "49.99",
    ["description"] = "Compact mechanical keyboard",
    ["category"] = "Accessories",
    ["stock"] = "25",
    ["reorder"] = "5"
});
```

To set both the price and final stock quantity of an existing product:

```csharp
var exitCode = await command.ExecuteAsync(new Dictionary<string, string>
{
    ["subcommand"] = "update",
    ["id"] = "42",
    ["price"] = "44.99",
    ["stock"] = "30"
});
```

## Notes

- Operational exceptions are logged, their messages are written to standard error, and the command returns `1`.
- Missing create options produce `Required: --name, --sku, --price`; a missing or invalid update/delete ID produces `--id parameter required`.
- Numeric parsing uses the process's current culture.
- Updating a product with neither a valid `price` nor a valid `stock` still calls `UpdateProductAsync` and reports success.
- The delete command does not inspect the Boolean result from `DeleteProductAsync`; if the service returns normally, the command prints `Product {id} deleted` and returns `0` even when no product was deleted.
- The options dictionary uses case-sensitive key lookup. When options originate from `CommandParser`, their names are normalized to lowercase.

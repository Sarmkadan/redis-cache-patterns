# AppConstants

`AppConstants` provides application-wide values for cache configuration, validation limits, business rules, and pagination defaults. Constants are organized into nested static classes according to their purpose.

## API

### `Cache`

Contains default Redis settings, cache limits, expiration durations, and cache key patterns.

#### `const string DEFAULT_CONNECTION`

The default Redis connection string. The value is `"localhost:6379"`.

#### `const int DEFAULT_DATABASE`

The default Redis database number. The value is `0`.

#### `const int LOCK_TIMEOUT_SECONDS`

The lock timeout in seconds. The value is `10`.

#### `const int MAX_CACHE_SIZE`

The maximum cache size in bytes. The value is `104857600` (100 MB).

#### `Cache.Expiration`

Contains standard cache-entry lifetimes. These values are static read-only `TimeSpan` fields.

##### `TimeSpan USER`

The expiration duration for cached users. The value is 1 hour.

##### `TimeSpan PRODUCT`

The expiration duration for cached products. The value is 2 hours.

##### `TimeSpan ORDER`

The expiration duration for cached orders. The value is 1 hour.

##### `TimeSpan INVENTORY`

The expiration duration for cached inventory. The value is 30 minutes.

##### `TimeSpan LIST`

The expiration duration for cached lists. The value is 30 minutes.

##### `TimeSpan SEARCH`

The expiration duration for cached search results. The value is 10 minutes.

#### `Cache.KeyPatterns`

Contains templates used to construct cache keys. Braced segments are placeholders for runtime values.

##### `const string USER`

The user key pattern. The value is `"user:{id}"`.

##### `const string USER_USERNAME`

The username lookup key pattern. The value is `"user:username:{username}"`.

##### `const string PRODUCT`

The product key pattern. The value is `"product:{id}"`.

##### `const string PRODUCT_SKU`

The product SKU lookup key pattern. The value is `"product:sku:{sku}"`.

##### `const string ORDER`

The order key pattern. The value is `"order:{id}"`.

##### `const string INVENTORY`

The inventory key pattern. The value is `"inventory:{id}"`.

##### `const string LOCK`

The distributed lock key pattern. The value is `"lock:{key}"`.

### `Validation`

Contains limits used when validating users, passwords, products, and prices.

#### `const int MIN_USERNAME_LENGTH`

The minimum username length. The value is `3`.

#### `const int MAX_USERNAME_LENGTH`

The maximum username length. The value is `50`.

#### `const int MIN_PASSWORD_LENGTH`

The minimum password length. The value is `8`.

#### `const int MIN_PRODUCT_NAME_LENGTH`

The minimum product name length. The value is `2`.

#### `const int MAX_PRODUCT_NAME_LENGTH`

The maximum product name length. The value is `255`.

#### `const decimal MIN_PRICE`

The minimum product price. The value is `0`.

#### `const decimal MAX_PRICE`

The maximum product price. The value is `999999.99`.

### `Business`

Contains default values and limits for inventory and order calculations.

#### `const int DEFAULT_REORDER_LEVEL`

The default inventory level at which reordering is required. The value is `10`.

#### `const int MIN_ORDER_ITEMS`

The minimum number of items allowed in an order. The value is `1`.

#### `const decimal TAX_RATE`

The tax rate applied to orders. The value is `0.08` (8%).

#### `const decimal STANDARD_SHIPPING`

The standard shipping charge. The value is `10.00`.

### `Pagination`

Contains default and maximum values for paginated requests.

#### `const int DEFAULT_PAGE_SIZE`

The default number of items per page. The value is `20`.

#### `const int MAX_PAGE_SIZE`

The maximum number of items allowed per page. The value is `100`.

#### `const int DEFAULT_PAGE_NUMBER`

The default page number. The value is `1`.

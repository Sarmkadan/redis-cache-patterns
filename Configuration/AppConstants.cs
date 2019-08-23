// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Application-wide constants
/// </summary>
public static class AppConstants
{
    public static class Cache
    {
        public const string DEFAULT_CONNECTION = "localhost:6379";
        public const int DEFAULT_DATABASE = 0;
        public const int LOCK_TIMEOUT_SECONDS = 10;
        public const int MAX_CACHE_SIZE = 1024 * 1024 * 100; // 100MB

        public static class Expiration
        {
            public static readonly TimeSpan USER = TimeSpan.FromHours(1);
            public static readonly TimeSpan PRODUCT = TimeSpan.FromHours(2);
            public static readonly TimeSpan ORDER = TimeSpan.FromHours(1);
            public static readonly TimeSpan INVENTORY = TimeSpan.FromMinutes(30);
            public static readonly TimeSpan LIST = TimeSpan.FromMinutes(30);
            public static readonly TimeSpan SEARCH = TimeSpan.FromMinutes(10);
        }

        public static class KeyPatterns
        {
            public const string USER = "user:{id}";
            public const string USER_USERNAME = "user:username:{username}";
            public const string PRODUCT = "product:{id}";
            public const string PRODUCT_SKU = "product:sku:{sku}";
            public const string ORDER = "order:{id}";
            public const string INVENTORY = "inventory:{id}";
            public const string LOCK = "lock:{key}";
        }
    }

    public static class Validation
    {
        public const int MIN_USERNAME_LENGTH = 3;
        public const int MAX_USERNAME_LENGTH = 50;
        public const int MIN_PASSWORD_LENGTH = 8;
        public const int MIN_PRODUCT_NAME_LENGTH = 2;
        public const int MAX_PRODUCT_NAME_LENGTH = 255;
        public const decimal MIN_PRICE = 0m;
        public const decimal MAX_PRICE = 999999.99m;
    }

    public static class Business
    {
        public const int DEFAULT_REORDER_LEVEL = 10;
        public const int MIN_ORDER_ITEMS = 1;
        public const decimal TAX_RATE = 0.08m; // 8%
        public const decimal STANDARD_SHIPPING = 10.00m;
    }

    public static class Pagination
    {
        public const int DEFAULT_PAGE_SIZE = 20;
        public const int MAX_PAGE_SIZE = 100;
        public const int DEFAULT_PAGE_NUMBER = 1;
    }
}

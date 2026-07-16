## CacheEntry

The `CacheEntry` class represents metadata about a cached entry, providing monitoring, analytics, and management capabilities for cache operations. It tracks access patterns, hit/miss ratios, expiration status, and supports tag-based categorization for efficient cache invalidation and analysis. This type is essential for cache-aside patterns, monitoring dashboards, and implementing cache warming strategies.

### Usage Example

```csharp
using RedisCachePatterns.Domain;

// Create a new cache entry for tracking a product
var productCacheEntry = new CacheEntry
{
    Key = "product:123",
    DataType = "Product",
    SizeInBytes = 1024,
    Status = "active",
    Tags = "electronics,premium"
};

// Record cache access events
productCacheEntry.RecordHit();  // Cache hit occurred
productCacheEntry.RecordHit();  // Another cache hit
productCacheEntry.RecordMiss(); // Cache miss occurred

// Check cache performance metrics
Console.WriteLine($"Hit Rate: {productCacheEntry.HitRate:F1}%");  // Output: Hit Rate: 66.7%
Console.WriteLine($"Access Count: {productCacheEntry.AccessCount}");     // Output: Access Count: 3
Console.WriteLine($"Hit Count: {productCacheEntry.HitCount}");           // Output: Hit Count: 2
Console.WriteLine($"Miss Count: {productCacheEntry.MissCount}");         // Output: Miss Count: 1

// Set expiration and check status
productCacheEntry.SetExpiration(DateTime.UtcNow.AddHours(24));
Console.WriteLine($"Time to expiry: {productCacheEntry.TimeToExpiry?.TotalHours:F1} hours");
Console.WriteLine($"Is expired: {productCacheEntry.IsExpired}");  // Output: Is expired: False

// Add and check tags for cache invalidation
productCacheEntry.AddTag("seasonal");
Console.WriteLine($"Has 'electronics' tag: {productCacheEntry.HasTag("electronics")}");  // Output: True
Console.WriteLine($"Has 'seasonal' tag: {productCacheEntry.HasTag("seasonal")}");      // Output: True

// Invalidate cache entry
productCacheEntry.Invalidate();
Console.WriteLine($"Status after invalidation: {productCacheEntry.Status}");  // Output: Status after invalidation: invalidated
Console.WriteLine(productCacheEntry.ToString());
// Output: "Cache [product:123] - Size: 1024B, Hit Rate: 66.7%, Status: invalidated"
```

## User

The `User` class represents a system user with authentication and profile data. It includes properties for identity, contact information, role, and activity status, and methods to manage profile updates, login tracking, and activation state.

### Usage Example

```csharp
using RedisCachePatterns.Domain;
using System;

var user = new User
{
    Id = 1,
    Username = "jdoe",
    Email = "jdoe@example.com",
    FirstName = "John",
    LastName = "Doe",
    PasswordHash = "hashed",
    Role = UserRole.User
};

user.UpdateProfile("John", "Doe", "555-1234", "123 Main St");
user.SetLastLogin();

Console.WriteLine($"Full name: {user.GetFullName()}");
Console.WriteLine($"Email valid: {user.IsValidEmail()}");
Console.WriteLine($"Orders count: {user.Orders.Count}");

user.Deactivate();
Console.WriteLine($"Is active: {user.IsActive}");
```

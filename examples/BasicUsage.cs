using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Demonstrates the most common operations: setting and retrieving data.
/// </summary>
public class BasicUsage
{
    private readonly ICacheService _cacheService;

    public BasicUsage(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task RunExampleAsync()
    {
        var key = "user:123";
        var user = new { Id = 123, Name = "John Doe" };

        // Set value with 10-minute expiration
        await _cacheService.SetAsync(key, user, TimeSpan.FromMinutes(10));

        // Get value
        var cachedUser = await _cacheService.GetAsync<object>(key);
        
        if (cachedUser != null)
        {
            // Found in cache
        }
    }
}

// existing content ...

## IEventPublisher

The `IEventPublisher` interface represents an event publisher for pub-sub pattern supporting async event handling. It provides methods for publishing events, subscribing to events, and unsubscribing from events. This allows for decoupling event producers from consumers using the observer pattern.

### Usage Example
```csharp
public class MyEvent : DomainEvent
{
    public string MyProperty { get; set; }
}

public class MyEventHandler
{
    public async Task HandleMyEvent(MyEvent @event)
    {
        Console.WriteLine($"Received event: MyProperty={@event.MyProperty}");
    }
}

var eventPublisher = new EventPublisher(new NullLogger<EventPublisher>());
var myEventHandler = new MyEventHandler();

eventPublisher.Subscribe<MyEvent>(myEventHandler.HandleMyEvent);

var myEvent = new MyEvent { MyProperty = "Hello, World!" };
await eventPublisher.PublishAsync(myEvent);
```

## CacheHitEvent

The `CacheHitEvent` represents an event triggered when a cache lookup successfully retrieves data. It contains information about the accessed cache key and the size of the retrieved data, which can be used for monitoring cache performance and analyzing hit patterns.

### Usage Example
```csharp
// Create a cache hit event when data is retrieved from cache
var cacheHitEvent = new CacheHitEvent
{
    CacheKey = "user:123:profile",
    DataSize = 1024 // Size in bytes
};

// In a cache service implementation
public class CacheService
{
    private readonly CacheEventListener _listener;
    private readonly ILogger<CacheService> _logger;
    
    public CacheService(CacheEventListener listener, ILogger<CacheService> logger)
    {
        _listener = listener;
        _logger = logger;
    }
    
    public async Task<CacheData> GetDataAsync(string key)
    {
        var cachedData = await _cache.GetAsync(key);
        
        if (cachedData != null)
        {
            // Cache hit occurred
            await _listener.OnCacheHitAsync(new CacheHitEvent
            {
                CacheKey = key,
                DataSize = cachedData.Length
            });
            
            _logger.LogInformation("Cache hit for key: {Key} | Size: {Size} bytes", 
                key, cachedData.Length);
            return cachedData;
        }
        
        // Cache miss would trigger CacheMissEvent instead
        return null;
    }
}

// Usage in application startup
var cacheEventListener = new CacheEventListener(logger);
var cacheService = new CacheService(cacheEventListener, logger);

// Monitor cache statistics
var hitRate = cacheEventListener.GetHitRate();
var totalHits = cacheEventListener.GetTotalHits();
```
#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Events;

/// <summary>
/// Event publisher for pub-sub pattern supporting async event handling
/// Decouples event producers from consumers using observer pattern
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent;
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent;
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent;
}

/// <summary>
/// Base class for domain events
/// </summary>
public abstract class DomainEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Default implementation of event publisher with in-memory subscriber management
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(ILogger<EventPublisher> logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent
    {
        var eventType = typeof(TEvent);
        _logger.LogInformation("Publishing event: {EventType} | EventId: {EventId}", eventType.Name, @event.EventId);

        if (!_subscribers.TryGetValue(eventType, out var handlers))
        {
            _logger.LogDebug("No subscribers for event type: {EventType}", eventType.Name);
            return;
        }

        var tasks = new List<Task>();
        foreach (var handler in handlers.ToList())
        {
            try
            {
                if (handler is Func<TEvent, Task> asyncHandler)
                {
                    tasks.Add(asyncHandler(@event));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing event handler for event: {EventType}", eventType.Name);
            }
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
            _logger.LogInformation("Event published successfully: {EventType} | Handlers: {Count}", eventType.Name, tasks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event: {EventType}", eventType.Name);
        }
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent
    {
        var eventType = typeof(TEvent);
        if (!_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType] = new List<Delegate>();
        }

        _subscribers[eventType].Add(handler);
        _logger.LogDebug("Subscriber added for event: {EventType}", eventType.Name);
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent
    {
        var eventType = typeof(TEvent);
        if (_subscribers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
            _logger.LogDebug("Subscriber removed for event: {EventType}", eventType.Name);
        }
    }
}

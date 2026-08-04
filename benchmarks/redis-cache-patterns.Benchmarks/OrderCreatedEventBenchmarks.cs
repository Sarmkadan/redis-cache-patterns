using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using RedisCachePatterns.Events;
using System.Text.Json;

namespace RedisCachePatterns.Benchmarks;

[MemoryDiagnoser]
public class OrderCreatedEventBenchmarks
{
    private OrderCreatedEvent[]? _events;
    private string[]? _serializedEvents;

    [Params(10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _events = new OrderCreatedEvent[N];
        _serializedEvents = new string[N];

        for (int i = 0; i < N; i++)
        {
            var orderEvent = new OrderCreatedEvent
            {
                OrderId = i + 1,
                UserId = (i + 1) * 100,
                TotalAmount = (i + 1) * 50.99m
            };

            _events[i] = orderEvent;
            _serializedEvents[i] = JsonSerializer.Serialize(orderEvent);
        }
    }

    [Benchmark]
    public void Instantiate_Events()
    {
        for (int i = 0; i < N; i++)
        {
            var orderEvent = new OrderCreatedEvent
            {
                OrderId = i + 1,
                UserId = (i + 1) * 100,
                TotalAmount = (i + 1) * 50.99m
            };
        }
    }

    [Benchmark]
    public void Serialize_Events()
    {
        for (int i = 0; i < N; i++)
        {
            JsonSerializer.Serialize(_events![i]);
        }
    }

    [Benchmark]
    public void Deserialize_Events()
    {
        for (int i = 0; i < N; i++)
        {
            JsonSerializer.Deserialize<OrderCreatedEvent>(_serializedEvents![i]);
        }
    }
}

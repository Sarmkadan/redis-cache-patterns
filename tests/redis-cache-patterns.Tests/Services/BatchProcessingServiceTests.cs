#nullable enable
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Services;
using Xunit;

/// <summary>
/// Tests for the <see cref="BatchProcessingService{T}"/> class.
/// </summary>
public class BatchProcessingServiceTests
{
    private readonly Mock<ILogger<BatchProcessingService<int>>> _mockLogger = new();

    /// <summary>
    /// Verifies that when the batch size is reached, the service processes the batch immediately.
    /// </summary>
    [Fact]
    public async Task Enqueue_WhenBatchSizeReached_ProcessesImmediately()
    {
        var processedBatches = new List<List<int>>();

        async Task ProcessBatch(List<int> batch)
        {
            processedBatches.Add(batch);
            await Task.CompletedTask;
        }

        var service = new BatchProcessingService<int>(ProcessBatch, _mockLogger.Object, batchSize: 3);

        service.Enqueue(1);
        service.Enqueue(2);
        service.Enqueue(3);

        await Task.Delay(50);

        processedBatches.Should().HaveCount(1);
        processedBatches[0].Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Verifies that when the batch size is not reached, the service does not process the batch immediately.
    /// </summary>
    [Fact]
    public async Task Enqueue_BelowBatchSize_DoesNotProcessImmediately()
    {
        var processedBatches = new List<List<int>>();

        async Task ProcessBatch(List<int> batch)
        {
            processedBatches.Add(batch);
            await Task.CompletedTask;
        }

        var service = new BatchProcessingService<int>(ProcessBatch, _mockLogger.Object, batchSize: 5);

        service.Enqueue(1);
        service.Enqueue(2);

        processedBatches.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that calling <see cref="FlushAsync"/> with pending items processes the batch.
    /// </summary>
    [Fact]
    public async Task FlushAsync_WithPendingItems_ProcessesBatch()
    {
        var processedBatches = new List<List<int>>();

        async Task ProcessBatch(List<int> batch)
        {
            processedBatches.Add(batch);
            await Task.CompletedTask;
        }

        var service = new BatchProcessingService<int>(ProcessBatch, _mockLogger.Object, batchSize: 10);

        service.Enqueue(1);
        service.Enqueue(2);
        service.Enqueue(3);
        await service.FlushAsync();

        processedBatches.Should().HaveCount(1);
        processedBatches[0].Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Verifies that calling <see cref="FlushAsync"/> with an empty queue does nothing.
    /// </summary>
    [Fact]
    public async Task FlushAsync_WithEmptyQueue_DoesNothing()
    {
        var processedBatches = new List<List<int>>();

        async Task ProcessBatch(List<int> batch)
        {
            processedBatches.Add(batch);
            await Task.CompletedTask;
        }

        var service = new BatchProcessingService<int>(ProcessBatch, _mockLogger.Object, batchSize: 5);

        await service.FlushAsync();

        processedBatches.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that starting the service with a flush interval processes periodic batches.
    /// </summary>
    [Fact]
    public async Task Start_WithFlushInterval_ProcessesPeriodicBatches()
    {
        var processedBatches = new List<List<string>>();

        async Task ProcessBatch(List<string> batch)
        {
            processedBatches.Add(batch);
            await Task.CompletedTask;
        }

        var stringLogger = new Mock<ILogger<BatchProcessingService<string>>>();
        var service = new BatchProcessingService<string>(
            ProcessBatch,
            stringLogger.Object,
            batchSize: 100,
            flushInterval: TimeSpan.FromMilliseconds(100));

        service.Start();
        service.Enqueue("item1");
        service.Enqueue("item2");

        await Task.Delay(200);

        service.Stop();

        processedBatches.Should().NotBeEmpty();
        processedBatches.First().Should().Contain("item1", "item2");
    }

    /// <summary>
    /// Verifies that <see cref="GetQueueSize"/> returns the current item count.
    /// </summary>
    [Fact]
    public void GetQueueSize_ReturnsCurrentItemCount()
    {
        async Task ProcessBatch(List<int> batch)
        {
            await Task.CompletedTask;
        }

        var service = new BatchProcessingService<int>(ProcessBatch, _mockLogger.Object, batchSize: 10);

        service.Enqueue(1);
        service.Enqueue(2);
        service.Enqueue(3);

        service.GetQueueSize().Should().Be(3);
    }

    /// <summary>
    /// Verifies that when the <see cref="ProcessBatchFn"/> throws an exception, the service logs the error but continues.
    /// </summary>
    [Fact]
    public async Task ProcessBatchFn_WithException_LogsErrorButContinues()
    {
        var processedBatches = new List<List<int>>();

        async Task ProcessBatchWithError(List<int> batch)
        {
            throw new InvalidOperationException("Processing error");
        }

        var service = new BatchProcessingService<int>(ProcessBatchWithError, _mockLogger.Object, batchSize: 3);

        service.Enqueue(1);
        service.Enqueue(2);
        service.Enqueue(3);

        await service.FlushAsync();

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that the service processes multiple batches correctly when they are partially filled.
    /// </summary>
    [Fact]
    public async Task MultipleBatches_PartiallyFilled_ProcessesCorrectly()
    {
        var processedBatches = new List<List<int>>();

        async Task ProcessBatch(List<int> batch)
        {
            processedBatches.Add(batch);
            await Task.CompletedTask;
        }

        var service = new BatchProcessingService<int>(ProcessBatch, _mockLogger.Object, batchSize: 2);

        service.Enqueue(1);
        service.Enqueue(2);
        service.Enqueue(3);
        service.Enqueue(4);
        service.Enqueue(5);

        await Task.Delay(50);

        processedBatches.Should().HaveCount(2);
        processedBatches[0].Should().Equal(1, 2);
        processedBatches[1].Should().Equal(3, 4);
    }

    /// <summary>
    /// Verifies that disposing the service stops the flush timer.
    /// </summary>
    [Fact]
    public async Task Dispose_StopsFlushTimer()
    {
        async Task ProcessBatch(List<int> batch)
        {
            await Task.CompletedTask;
        }

        var service = new BatchProcessingService<int>(
            ProcessBatch,
            _mockLogger.Object,
            batchSize: 100,
            flushInterval: TimeSpan.FromMilliseconds(100));

        service.Start();
        await Task.Delay(50);
        service.Dispose();

        var initialSize = service.GetQueueSize();
        service.Enqueue(1);
        await Task.Delay(200);

        service.GetQueueSize().Should().Be(initialSize + 1);
    }
}

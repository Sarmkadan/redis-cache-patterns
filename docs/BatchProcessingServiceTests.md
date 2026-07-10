# BatchProcessingServiceTests

Unit tests for `BatchProcessingService`, verifying batch accumulation, size-based flushing, periodic flushing, error handling, and lifecycle management.

## API

### `Enqueue_WhenBatchSizeReached_ProcessesImmediately`
Verifies that when the number of enqueued items reaches the configured batch size, the batch is processed immediately without waiting for the flush interval.

### `Enqueue_BelowBatchSize_DoesNotProcessImmediately`
Ensures that enqueuing fewer items than the configured batch size does not trigger immediate processing; processing only occurs on flush or when the batch size is reached.

### `FlushAsync_WithPendingItems_ProcessesBatch`
Confirms that calling `FlushAsync` when items are pending triggers immediate processing of the current batch.

### `FlushAsync_WithEmptyQueue_DoesNothing`
Validates that invoking `FlushAsync` when no items are queued results in no action and no exceptions.

### `Start_WithFlushInterval_ProcessesPeriodicBatches`
Checks that when `Start` is called with a flush interval, batches are processed periodically according to the interval, independent of batch size.

### `GetQueueSize_ReturnsCurrentItemCount`
Returns the current number of items in the processing queue, useful for monitoring and testing queue state.

### `ProcessBatchFn_WithException_LogsErrorButContinues`
Ensures that if the batch processing function throws an exception, the error is logged but subsequent batches continue processing without interruption.

### `MultipleBatches_PartiallyFilled_ProcessesCorrectly`
Tests that multiple batches, some partially filled, are processed correctly according to batch size and timing rules.

### `Dispose_StopsFlushTimer`
Confirms that disposing the service stops the internal flush timer, preventing further periodic processing.

## Usage

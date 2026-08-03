using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.API;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.Benchmarks
{
    [MemoryDiagnoser]
    public class ApiEndpointBaseBenchmarks
    {
        private Mock<ILogger<PerformanceMonitor>> _loggerMock = null!;
        private Mock<PerformanceMonitor> _performanceMonitorMock = null!;
        private BenchmarkApiEndpoint _endpoint = null!;
        private Mock<IDisposable> _measurementScopeMock = null!;

        [Params(10, 100, 1000)]
        public int IterationCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<PerformanceMonitor>>();
            _performanceMonitorMock = new Mock<PerformanceMonitor>(_loggerMock.Object);
            _measurementScopeMock = new Mock<IDisposable>();
            _performanceMonitorMock.Setup(p => p.MeasureOperation(It.IsAny<string>()))
                .Returns(_measurementScopeMock.Object);

            _endpoint = new BenchmarkApiEndpoint(_loggerMock.Object, _performanceMonitorMock.Object);
        }

        [Benchmark]
        public async Task ExecuteAsync_Success()
        {
            int result = 0;
            Func<Task<int>> operation = async () =>
            {
                for (int i = 0; i < IterationCount; i++)
                {
                    await Task.Yield();
                    result += i;
                }
                return result;
            };

            await _endpoint.ExecuteAsync(operation, "TestOperation");
        }

        [Benchmark]
        public async Task ExecuteAsync_Exception()
        {
            Func<Task<int>> operation = async () =>
            {
                for (int i = 0; i < IterationCount; i++)
                {
                    await Task.Yield();
                }
                throw new ArgumentException("Test exception");
            };

            await _endpoint.ExecuteAsync(operation, "TestOperation");
        }

        [Benchmark]
        public async Task ExecuteAndReshapeAsync_Success()
        {
            int inputValue = 42;
            Func<Task<int>> operation = async () =>
            {
                for (int i = 0; i < IterationCount; i++)
                {
                    await Task.Yield();
                }
                return inputValue;
            };

            Func<int, string> mapper = (input) =>
            {
                string result = input.ToString();
                for (int i = 0; i < IterationCount; i++)
                {
                    result += i.ToString();
                }
                return result;
            };

            await _endpoint.ExecuteAndReshapeAsync(operation, mapper, "TestOperation");
        }

        private class BenchmarkApiEndpoint : ApiEndpointBase
        {
            public BenchmarkApiEndpoint(ILogger<PerformanceMonitor> logger, PerformanceMonitor performanceMonitor)
                : base(logger, performanceMonitor)
            {
            }

            // Expose protected methods for benchmarking
            public new Task<ApiResponse<T>> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
                => base.ExecuteAsync(operation, operationName);

            public new Task<ApiResponse<U>> ExecuteAndReshapeAsync<T, U>(Func<Task<T>> operation, Func<T, U> mapper, string operationName)
                => base.ExecuteAndReshapeAsync(operation, mapper, operationName);
        }
    }
}
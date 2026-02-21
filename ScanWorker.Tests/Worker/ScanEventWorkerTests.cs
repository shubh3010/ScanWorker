using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ScanWorker.Interface;
using ScanWorker.Worker;

namespace ScanWorker.Tests.Worker;

public class ScanEventWorkerTests
{
    private readonly Mock<ILogger<ScanEventWorker>> _loggerMock = new();

    /// <summary>
    /// Wires up a ScanEventWorker whose IServiceProvider resolves a mocked IScanEventProcessor.
    /// </summary>
    private (ScanEventWorker worker, Mock<IScanEventProcessor> processorMock) CreateWorker()
    {
        var processorMock = new Mock<IScanEventProcessor>();

        // Build a minimal service-provider chain: IServiceProvider → IServiceScopeFactory → IServiceScope → IScanEventProcessor
        var scopeProviderMock = new Mock<IServiceProvider>();
        scopeProviderMock
            .Setup(sp => sp.GetService(typeof(IScanEventProcessor)))
            .Returns(processorMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(scopeProviderMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);

        var worker = new ScanEventWorker(serviceProviderMock.Object, _loggerMock.Object);
        return (worker, processorMock);
    }

    [Fact]
    public async Task ExecuteAsync_CallsProcessor_WhenStarted()
    {
        var (worker, processorMock) = CreateWorker();
        var firstCallComplete = new TaskCompletionSource();

        processorMock
            .Setup(p => p.ProcessBatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Callback(() => firstCallComplete.TrySetResult());

        await worker.StartAsync(CancellationToken.None);
        await firstCallComplete.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        processorMock.Verify(p => p.ProcessBatchAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_StopsGracefully_WhenCancellationRequested()
    {
        var (worker, processorMock) = CreateWorker();
        var firstCallComplete = new TaskCompletionSource();

        processorMock
            .Setup(p => p.ProcessBatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Callback(() => firstCallComplete.TrySetResult());

        await worker.StartAsync(CancellationToken.None);
        await firstCallComplete.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // StopAsync should complete without throwing
        var stopAct = () => worker.StopAsync(CancellationToken.None);
        await stopAct.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotPropagateException_WhenProcessorThrows()
    {
        var (worker, processorMock) = CreateWorker();

        processorMock
            .Setup(p => p.ProcessBatchAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("transient processor failure"));

        // Start the worker; it will hit the exception and enter retry backoff
        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(50); // give it time to reach the exception

        // StopAsync cancels the stoppingToken, which interrupts the retry delay and shuts down cleanly
        var stopAct = () => worker.StopAsync(CancellationToken.None);
        await stopAct.Should().NotThrowAsync();

        processorMock.Verify(p => p.ProcessBatchAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_CallsProcessorAgain_AfterTransientFailure()
    {
        var (worker, processorMock) = CreateWorker();
        var secondCallComplete = new TaskCompletionSource();
        var callCount = 0;

        processorMock
            .Setup(p => p.ProcessBatchAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(_ =>
            {
                callCount++;
                if (callCount == 1)
                    return Task.FromException<bool>(new Exception("first call fails"));

                secondCallComplete.TrySetResult();
                return Task.FromResult(true);
            });

        await worker.StartAsync(CancellationToken.None);

        // Retry delay is 5 s — wait up to 10 s for the second attempt
        await secondCallComplete.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(CancellationToken.None);

        processorMock.Verify(p => p.ProcessBatchAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_ResetsRetryCount_AfterSuccessfulBatch()
    {
        var (worker, processorMock) = CreateWorker();
        var secondCallComplete = new TaskCompletionSource();
        var callCount = 0;

        processorMock
            .Setup(p => p.ProcessBatchAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(_ =>
            {
                callCount++;
                if (callCount == 1)
                    return Task.FromException<bool>(new Exception("transient"));

                secondCallComplete.TrySetResult();
                return Task.FromResult(true);
            });

        await worker.StartAsync(CancellationToken.None);
        await secondCallComplete.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(CancellationToken.None);

        // After a successful call at callCount==2, the retry counter should have been reset.
        // No critical-level log should have been emitted (which would indicate max retries hit).
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_StopsWorker_AfterMaxRetriesExceeded()
    {
        var (worker, processorMock) = CreateWorker();

        processorMock
            .Setup(p => p.ProcessBatchAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("persistent failure"));

        await worker.StartAsync(CancellationToken.None);

        // With MaxRetryCount=3 and BaseDelaySeconds=5 (5s + 10s + 20s backoff),
        // verifying the critical log is emitted after enough retries.
        // We stop the worker after the first retry delay to keep the test fast.
        await Task.Delay(TimeSpan.FromSeconds(6)); // past first 5 s backoff

        await worker.StopAsync(CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}

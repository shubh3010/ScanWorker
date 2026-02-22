using ScanWorker.Interface;

namespace ScanWorker.Worker;

public class ScanEventWorker(IServiceProvider serviceProvider, ILogger<ScanEventWorker> logger)
    : BackgroundService
{
    private const int MaxRetryCount = 3;
    private const int BaseDelaySeconds = 5;

    private int _retryCount;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ScanEventWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // New scope per iteration to avoid a long-lived scoped service inside a singleton
                using var scope = serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IScanEventProcessor>();

                var hasWork = await processor.ProcessBatchAsync(stoppingToken);

                if (hasWork)
                {
                    logger.LogInformation("Batch processed successfully");
                }

                // Reset on success so transient errors don't accumulate across batches
                ResetRetryCount();

                if (!hasWork)
                {
                    // No events available — back off before polling again
                    logger.LogDebug("No new scan events found. Waiting {Delay}s before next poll", BaseDelaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(BaseDelaySeconds), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("ScanEventWorker is stopping due to cancellation");
            }
            catch (Exception ex)
            {
                // Retry with exponential backoff; stop the worker if max retries exceeded
                if (!await HandleRetryAsync(ex, stoppingToken))
                    break;
            }
        }

        logger.LogInformation("ScanEventWorker stopped");
    }

    private async Task<bool> HandleRetryAsync(Exception ex, CancellationToken ct)
    {
        _retryCount++;

        if (_retryCount > MaxRetryCount)
        {
            logger.LogCritical(ex, "Max retries ({MaxRetry}) exceeded. Worker stopping", MaxRetryCount);
            return false;
        }

        // Exponential backoff: 5s, 10s, 20s
        var delay = TimeSpan.FromSeconds(BaseDelaySeconds * Math.Pow(2, _retryCount - 1));

        logger.LogError(ex, "Retry {RetryCount}/{MaxRetry} in {Delay}s",
            _retryCount, MaxRetryCount, delay.TotalSeconds);

        await Task.Delay(delay, ct);
        return true;
    }

    private void ResetRetryCount()
    {
        _retryCount = 0;
    }
}
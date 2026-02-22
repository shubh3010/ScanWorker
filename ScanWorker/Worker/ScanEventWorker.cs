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
                using var scope = serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IScanEventProcessor>();

                var hasWork = await processor.ProcessBatchAsync(stoppingToken);

                if (hasWork)
                {
                    logger.LogInformation("Batch processed successfully");
                }

                ResetRetryCount();

                if (!hasWork)
                {
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
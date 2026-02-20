using ScanWorker.Interface;

namespace ScanWorker.Worker;

public class ScanEventWorker(IServiceProvider serviceProvider, ILogger<ScanEventWorker> logger)
    : BackgroundService
{
    private const int MaxRetryCount = 3;
    private const int BaseDelaySeconds = 5;
    private const int EmptyBatchDelaySeconds = 5;

    private int _retryCount;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IScanEventProcessor>();

                var hasWork = await processor.ProcessBatchAsync(stoppingToken);

                ResetRetryCount();

                if (!hasWork)
                {
                    logger.LogDebug("No new scan events found. Waiting {Delay}s before next poll", EmptyBatchDelaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(EmptyBatchDelaySeconds), stoppingToken);
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

    private void ResetRetryCount() => _retryCount = 0;
}
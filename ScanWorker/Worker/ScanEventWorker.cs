using ScanWorker.Interface;

namespace ScanWorker.Worker;

public class ScanEventWorker(
    IServiceProvider serviceProvider,
    ILogger<ScanEventWorker> logger)
    : BackgroundService
{
    private const int ErrorDelaySeconds = 5;
    private const int EmptyBatchDelaySeconds = 5;

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
                logger.LogError(ex, "Unexpected error in worker loop");
                await Task.Delay(TimeSpan.FromSeconds(ErrorDelaySeconds), stoppingToken);
            }
        }

        logger.LogInformation("ScanEventWorker stopped");
    }
}
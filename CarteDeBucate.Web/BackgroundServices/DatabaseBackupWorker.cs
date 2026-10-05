using Microsoft.Data.Sqlite;
using CarteDeBucate.Web.Services;

namespace CarteDeBucate.Web.BackgroundServices;

public class DatabaseBackupWorker : BackgroundService
{
    private static readonly TimeSpan BackupRetryDelay = TimeSpan.FromMinutes(15);

    private readonly ILogger<DatabaseBackupWorker> _logger;
    private readonly IDatabaseBackupService _backupService;

    public DatabaseBackupWorker(
        ILogger<DatabaseBackupWorker> logger,
        IDatabaseBackupService backupService)
    {
        _logger = logger;
        _backupService = backupService;
    }

    protected override async Task ExecuteAsync(
       CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database backup worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                TimeSpan delay = CalculateDelayUntilNextBackup();

                _logger.LogInformation("Next database backup scheduled in {Delay}.", delay);

                await Task.Delay(delay, stoppingToken);

                await CreateBackupWithRetryAsync(stoppingToken);

                _backupService.DeleteOldBackups();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database backup process failed.");
            }
        }

        _logger.LogInformation("Database backup worker stopped.");
    }

    private async Task CreateBackupWithRetryAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _backupService.CreateBackupAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Database backup failed. Retrying in {RetryDelay}.",
                    BackupRetryDelay);

                await Task.Delay(BackupRetryDelay, stoppingToken);
            }
        }
    }

    private static TimeSpan CalculateDelayUntilNextBackup()
    {
        DateTime now = DateTime.UtcNow;
        DateTime nextRun = now.Date.AddDays(1).AddHours(2);

        return nextRun - now;
    }
}

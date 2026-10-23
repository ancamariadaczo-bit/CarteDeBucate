using Microsoft.Data.Sqlite;

namespace CarteDeBucate.Web.Services;

public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly string _databasePath;
    private readonly IWebHostEnvironment _environment;

    private const int RetentionDays = 7;

    public DatabaseBackupService(
        ILogger<DatabaseBackupService> logger,
        string databasePath,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _databasePath = databasePath;
        _environment = environment;
    }

    public async Task CreateBackupAsync(CancellationToken cancellationToken)
    {
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        string backupDirectory = Path.Combine(_environment.ContentRootPath, "Backups");

        Directory.CreateDirectory(backupDirectory);

        string fileName = $"recipes-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.db";

        string backupPath = Path.Combine(backupDirectory, fileName);
        string temporaryBackupPath = $"{backupPath}.tmp";

        _logger.LogInformation("Starting database backup. Destination: {BackupPath}", backupPath);

        try
        {
            await using (var sourceConnection = new SqliteConnection(connectionString))
            await using (var backupConnection = new SqliteConnection($"Data Source={temporaryBackupPath}"))
            {
                await sourceConnection.OpenAsync(cancellationToken);
                await backupConnection.OpenAsync(cancellationToken);

                sourceConnection.BackupDatabase(backupConnection);
            }

            File.Move(temporaryBackupPath, backupPath);
        }
        catch
        {
            TryDeleteTemporaryBackup(temporaryBackupPath);
            throw;
        }

        var backupFile = new FileInfo(backupPath);

        _logger.LogInformation(
            "Database backup completed successfully. File: {BackupFile}, Size: {BackupSize} bytes.",
            backupFile.Name,
            backupFile.Length);
    }

    private void TryDeleteTemporaryBackup(string temporaryBackupPath)
    {
        try
        {
            if (File.Exists(temporaryBackupPath))
            {
                File.Delete(temporaryBackupPath);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Could not delete incomplete database backup: {BackupPath}",
                temporaryBackupPath);
        }
    }

    public void DeleteOldBackups()
    {
        string backupDirectory = Path.Combine(_environment.ContentRootPath, "Backups");

        if (!Directory.Exists(backupDirectory))
        {
            _logger.LogInformation("Backup directory does not exist. No old backups to delete.");

            return;
        }

        DateTime cutoffDate = DateTime.UtcNow.AddDays(-RetentionDays);

        FileInfo[] oldBackups =
            new DirectoryInfo(backupDirectory)
                .GetFiles("*.db")
                .Where(file => file.LastWriteTimeUtc < cutoffDate)
                .ToArray();
        int deletedBackupCount = 0;

        foreach (FileInfo backup in oldBackups)
        {
            try
            {
                backup.Delete();
                deletedBackupCount++;

                _logger.LogInformation("Deleted old database backup: {BackupFile}", backup.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete old database backup: {BackupFile}", backup.Name);
            }
        }

        _logger.LogInformation(
            "Backup cleanup completed. Deleted {DeletedBackupCount} old backup(s).",
            deletedBackupCount);
    }
}

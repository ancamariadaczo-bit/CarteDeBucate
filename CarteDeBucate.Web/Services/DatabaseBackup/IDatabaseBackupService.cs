namespace CarteDeBucate.Web.Services;

public interface IDatabaseBackupService
{
    Task CreateBackupAsync(CancellationToken cancellationToken);

    void DeleteOldBackups();
}

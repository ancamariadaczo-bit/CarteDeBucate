using Microsoft.Extensions.Logging;

internal sealed class RecipePhotoReconciliationService : IRecipePhotoReconciliationService
{
    private static readonly StringComparer StorageFileNameComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private readonly IRecipePhotoStorage _storage;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IRecipePhotoRepository _photoRepository;
    private readonly TimeSpan _reconciliationMinimumAge;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RecipePhotoReconciliationService> _logger;

    public RecipePhotoReconciliationService(
        IRecipePhotoStorage storage,
        IRecipeRepository recipeRepository,
        IRecipePhotoRepository photoRepository,
        RecipePhotoOptions options,
        TimeProvider timeProvider,
        ILogger<RecipePhotoReconciliationService> logger
    )
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(recipeRepository);
        ArgumentNullException.ThrowIfNull(photoRepository);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        if (options.ReconciliationMinimumAge < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.ReconciliationMinimumAge),
                options.ReconciliationMinimumAge,
                "The reconciliation minimum age cannot be negative."
            );
        }

        _storage = storage;
        _recipeRepository = recipeRepository;
        _photoRepository = photoRepository;
        _reconciliationMinimumAge = options.ReconciliationMinimumAge;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public RecipePhotoReconciliationReport Reconcile()
    {
        RecipePhotoReconciliationReport report = new();
        HashSet<string> knownStorageFileNames;

        try
        {
            knownStorageFileNames = BuildKnownStorageFileNames();
        }
        catch (Exception exception)
        {
            report.FailedCount++;
            _logger.LogError(
                "Recipe photo reconciliation stopped because metadata "
                    + "could not be read. Error type: {ExceptionType}.",
                exception.GetType().Name
            );

            return report;
        }

        DateTime reconciliationCutoffUtc = GetReconciliationCutoffUtc();

        CleanupStagedUploads(report, reconciliationCutoffUtc);
        ReconcileQuarantinedDeletions(knownStorageFileNames, report, reconciliationCutoffUtc);
        ReportOrphanedFinalFiles(knownStorageFileNames, report, reconciliationCutoffUtc);

        return report;
    }

    private DateTime GetReconciliationCutoffUtc()
    {
        DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        TimeSpan maximumRepresentableAge = utcNow - DateTime.MinValue;

        return _reconciliationMinimumAge > maximumRepresentableAge
            ? DateTime.MinValue
            : utcNow - _reconciliationMinimumAge;
    }

    private void CleanupStagedUploads(
        RecipePhotoReconciliationReport report,
        DateTime reconciliationCutoffUtc
    )
    {
        RecipePhotoStorageInventory<StagedRecipePhotoUploadInventoryItem> inventory;

        try
        {
            inventory = _storage.GetStagedUploads();
        }
        catch (Exception exception)
        {
            report.FailedCount++;
            _logger.LogError(
                "Staged recipe photo uploads could not be inventoried. "
                    + "Error type: {ExceptionType}.",
                exception.GetType().Name
            );

            return;
        }

        ReportInvalidInventoryEntries(inventory.InvalidEntryCount, report, "staged recipe photo upload");

        foreach (StagedRecipePhotoUploadInventoryItem item in inventory.Items)
        {
            if (item.LastModifiedAtUtc >= reconciliationCutoffUtc)
            {
                report.SkippedTooNewCount++;
                continue;
            }

            try
            {
                _storage.AbandonUpload(item.Upload);
                report.DeletedCount++;
            }
            catch (Exception exception)
            {
                report.FailedCount++;
                _logger.LogError(
                    "A staged recipe photo upload could not be deleted. "
                        + "Error type: {ExceptionType}.",
                    exception.GetType().Name
                );
            }
        }
    }

    private void ReconcileQuarantinedDeletions(
        HashSet<string> knownStorageFileNames,
        RecipePhotoReconciliationReport report,
        DateTime reconciliationCutoffUtc
    )
    {
        RecipePhotoStorageInventory<StagedRecipePhotoDeletionInventoryItem> inventory;

        try
        {
            inventory = _storage.GetQuarantinedDeletions();
        }
        catch (Exception exception)
        {
            report.FailedCount++;
            _logger.LogError(
                "Quarantined recipe photo deletions could not be "
                    + "inventoried. Error type: {ExceptionType}.",
                exception.GetType().Name
            );

            return;
        }

        ReportInvalidInventoryEntries(
            inventory.InvalidEntryCount,
            report,
            "quarantined recipe photo deletion"
        );

        foreach (StagedRecipePhotoDeletionInventoryItem item in inventory.Items)
        {
            if (item.QuarantinedAtUtc >= reconciliationCutoffUtc)
            {
                report.SkippedTooNewCount++;
                continue;
            }

            if (knownStorageFileNames.Contains(item.StorageFileName))
            {
                RestoreQuarantinedDeletion(item, report);
            }
            else
            {
                DeleteQuarantinedDeletion(item, report);
            }
        }
    }

    private void RestoreQuarantinedDeletion(
        StagedRecipePhotoDeletionInventoryItem item,
        RecipePhotoReconciliationReport report
    )
    {
        try
        {
            _storage.RestoreFromQuarantine(item.Deletion);
            report.RestoredCount++;
        }
        catch (Exception exception)
        {
            report.FailedCount++;
            _logger.LogError(
                "A quarantined recipe photo could not be restored. Error type: {ExceptionType}.",
                exception.GetType().Name
            );
        }
    }

    private void DeleteQuarantinedDeletion(
        StagedRecipePhotoDeletionInventoryItem item,
        RecipePhotoReconciliationReport report
    )
    {
        try
        {
            _storage.DeleteFromQuarantine(item.Deletion);
            report.DeletedCount++;
        }
        catch (Exception exception)
        {
            report.FailedCount++;
            _logger.LogError(
                "A quarantined recipe photo could not be deleted. Error type: {ExceptionType}.",
                exception.GetType().Name
            );
        }
    }

    private void ReportOrphanedFinalFiles(
        HashSet<string> knownStorageFileNames,
        RecipePhotoReconciliationReport report,
        DateTime reconciliationCutoffUtc
    )
    {
        RecipePhotoStorageInventory<RecipePhotoStorageFileInventoryItem> inventory;

        try
        {
            inventory = _storage.GetFinalFiles();
        }
        catch (Exception exception)
        {
            report.FailedCount++;
            _logger.LogError(
                "Final recipe photo files could not be inventoried. Error type: {ExceptionType}.",
                exception.GetType().Name
            );

            return;
        }

        ReportInvalidInventoryEntries(inventory.InvalidEntryCount, report, "final recipe photo file");

        foreach (RecipePhotoStorageFileInventoryItem item in inventory.Items)
        {
            if (item.LastModifiedAtUtc >= reconciliationCutoffUtc)
            {
                report.SkippedTooNewCount++;
                continue;
            }

            if (knownStorageFileNames.Contains(item.StorageFileName))
            {
                continue;
            }

            report.OrphanedCount++;
            _logger.LogWarning(
                "The final recipe photo file {StorageFileName} is not referenced by metadata.",
                item.StorageFileName
            );
        }
    }

    private void ReportInvalidInventoryEntries(
        int invalidEntryCount,
        RecipePhotoReconciliationReport report,
        string inventoryEntryDescription
    )
    {
        if (invalidEntryCount == 0)
        {
            return;
        }

        report.FailedCount += invalidEntryCount;
        _logger.LogWarning(
            "The recipe photo storage inventory contains "
                + "{InvalidEntryCount} invalid "
                + "{InventoryEntryDescription} entries.",
            invalidEntryCount,
            inventoryEntryDescription
        );
    }

    internal HashSet<string> BuildKnownStorageFileNames()
    {
        HashSet<string> storageFileNames = new(StorageFileNameComparer);
        List<Recipe> recipes = _recipeRepository.GetAllRecipes();

        foreach (Recipe recipe in recipes)
        {
            List<RecipePhoto> photos = _photoRepository.GetByRecipeId(recipe.Id);

            foreach (RecipePhoto photo in photos)
            {
                storageFileNames.Add(photo.StorageFileName);
            }
        }

        return storageFileNames;
    }
}

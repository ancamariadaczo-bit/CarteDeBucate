public sealed class RecipePhotoReconciliationReport
{
    public int DeletedCount { get; internal set; }

    public int RestoredCount { get; internal set; }

    public int OrphanedCount { get; internal set; }

    public int SkippedTooNewCount { get; internal set; }

    public int FailedCount { get; internal set; }
}

internal sealed class PreparedRecipePhotoDeletionBatch
{
    internal PreparedRecipePhotoDeletionBatch(
        int recipeId,
        IEnumerable<StagedRecipePhotoDeletion> stagedDeletions)
    {
        ArgumentNullException.ThrowIfNull(stagedDeletions);

        RecipeId = recipeId;
        StagedDeletions = stagedDeletions.ToArray();
    }

    internal int RecipeId { get; }

    internal IReadOnlyList<StagedRecipePhotoDeletion> StagedDeletions
    {
        get;
    }
}

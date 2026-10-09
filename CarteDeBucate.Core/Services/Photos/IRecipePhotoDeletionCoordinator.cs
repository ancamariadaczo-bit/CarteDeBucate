internal interface IRecipePhotoDeletionCoordinator
{
    PreparedRecipePhotoDeletionBatch? PrepareRecipeDeletion(int recipeId);

    void ConfirmRecipeDeletion(PreparedRecipePhotoDeletionBatch batch);

    void RestoreRecipeDeletion(PreparedRecipePhotoDeletionBatch batch);
}

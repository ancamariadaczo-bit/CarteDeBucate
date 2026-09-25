public static class RecipeBackupMapper
{
    public static RecipeBackupItem ToBackupItem(Recipe recipe)
    {
        return new RecipeBackupItem
        {
            Name = recipe.Name,
            SourceUrl = recipe.SourceUrl,
            SavedAt = recipe.SavedAt,
            Ingredients = recipe.Ingredients is null
                ? null!
                : new List<string>(recipe.Ingredients),
            Steps = recipe.Steps is null
                ? null!
                : new List<string>(recipe.Steps),
            Notes = recipe.Notes,
            Status = recipe.Status
        };
    }

    public static Recipe ToRecipe(RecipeBackupItem backupItem)
    {
        return new Recipe
        {
            Id = 0,
            Name = backupItem.Name,
            SourceUrl = backupItem.SourceUrl,
            SavedAt = backupItem.SavedAt,
            Ingredients = backupItem.Ingredients is null
                ? null!
                : new List<string>(backupItem.Ingredients),
            Steps = backupItem.Steps is null
                ? null!
                : new List<string>(backupItem.Steps),
            Notes = backupItem.Notes,
            Status = backupItem.Status
        };
    }
}

public class RecipeBackupMapperTests
{
    [Fact]
    public void ToBackupItem_ShouldCopyPortableRecipeData()
    {
        Recipe recipe = new Recipe
        {
            Id = 42,
            UserId = 7,
            Name = "Supa crema",
            SourceUrl = "https://example.com/supa-crema",
            SavedAt = new DateTime(2026, 9, 2, 10, 30, 0),
            Ingredients = new List<string> { "Dovleac", "Sare" },
            Steps = new List<string> { "Fierbe", "Mixeaza" },
            Notes = "Se serveste calda.",
            Status = RecipeStatus.Favorite
        };

        RecipeBackupItem result = RecipeBackupMapper.ToBackupItem(recipe);

        Assert.Equal(recipe.Name, result.Name);
        Assert.Equal(recipe.SourceUrl, result.SourceUrl);
        Assert.Equal(recipe.SavedAt, result.SavedAt);
        Assert.Equal(recipe.Ingredients, result.Ingredients);
        Assert.NotSame(recipe.Ingredients, result.Ingredients);
        Assert.Equal(recipe.Steps, result.Steps);
        Assert.NotSame(recipe.Steps, result.Steps);
        Assert.Equal(recipe.Notes, result.Notes);
        Assert.Equal(recipe.Status, result.Status);
    }

    [Fact]
    public void ToRecipe_ShouldCopyBackupDataWithoutLocalIdentity()
    {
        RecipeBackupItem backupItem = new RecipeBackupItem
        {
            Name = "Supa crema",
            SourceUrl = "https://example.com/supa-crema",
            SavedAt = new DateTime(2026, 9, 2, 10, 30, 0),
            Ingredients = new List<string> { "Dovleac", "Sare" },
            Steps = new List<string> { "Fierbe", "Mixeaza" },
            Notes = "Se serveste calda.",
            Status = RecipeStatus.Favorite
        };

        Recipe result = RecipeBackupMapper.ToRecipe(backupItem);

        Assert.Equal(0, result.Id);
        Assert.Null(result.UserId);
        Assert.Equal(backupItem.Name, result.Name);
        Assert.Equal(backupItem.SourceUrl, result.SourceUrl);
        Assert.Equal(backupItem.SavedAt, result.SavedAt);
        Assert.Equal(backupItem.Ingredients, result.Ingredients);
        Assert.NotSame(backupItem.Ingredients, result.Ingredients);
        Assert.Equal(backupItem.Steps, result.Steps);
        Assert.NotSame(backupItem.Steps, result.Steps);
        Assert.Equal(backupItem.Notes, result.Notes);
        Assert.Equal(backupItem.Status, result.Status);
    }
}

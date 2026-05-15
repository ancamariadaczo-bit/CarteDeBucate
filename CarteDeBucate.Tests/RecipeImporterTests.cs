public class RecipeImporterTests
{
    [Fact]
    public async Task ImportFromUrlAsync_WithEmptyUrl_ShouldFail()
    {
        RecipeImporter importer = new RecipeImporter();

        RecipeImportResult result = await importer.ImportFromUrlAsync("");

        Assert.False(result.Success);
        Assert.Null(result.Recipe);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Theory]
    [InlineData("https://tastebazaar.ro/2018/08/crumble-cu-prune-si-migdale.html")]
    [InlineData("https://awfully-tasty.com/2021/04/01/salata-cu-linte-verde-rucola-rosii-cherry/")]
    //[InlineData("https://jamilacuisine.ro/rulouri-cu-scortisoara-si-dovleac-cu-aroma-de-toamna-reteta-video/")]
    [InlineData("https://www.wholesomeyum.com/recipes/low-carb-banana-bread-paleo-gluten-free-sugar-free/")]
    [InlineData("https://www.dietdoctor.com/recipes/the-keto-bread")]
    [InlineData("https://www.bbcgoodfood.com/recipes/keto-pancakes")]
    //[InlineData("https://lchf.ro/paine-crocanta-lchf/")]
    [InlineData("https://dolcefarverde.ro/paine-keto-fara-gluten-fara-framantare-reteta-video/")]
    [InlineData("https://www.lowcarbspark.com/almond-flour-coffee-loaf-cake/#wprm-recipe-container-77203")]
    public async Task ImportFromUrlAsync_WithRecipeUrl_ShouldDetectRecipe(string url)
    {
        RecipeImporter importer = new RecipeImporter();

        RecipeImportResult result = await importer.ImportFromUrlAsync(url);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Recipe);
        Assert.False(string.IsNullOrWhiteSpace(result.Recipe.Name));
    }

    [Fact]
    public async Task ImportFromUrlAsync_AwfullyTasty_ShouldExtractIngredients()
    {
        RecipeImporter importer = new RecipeImporter();

        RecipeImportResult result = await importer.ImportFromUrlAsync(
            "https://awfully-tasty.com/2021/04/01/salata-cu-linte-verde-rucola-rosii-cherry/");

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Recipe);

        Assert.True(result.Recipe.Ingredients.Count > 0);

        Assert.Contains(result.Recipe.Ingredients, ingredient =>
            ingredient.Contains("linte", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportFromUrlAsync_NonRecipeUrl_ShouldFail()
    {
        RecipeImporter importer = new RecipeImporter();

        RecipeImportResult result = await importer.ImportFromUrlAsync(
            "https://www.google.com");

        Assert.False(result.Success);
        Assert.Null(result.Recipe);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }
}
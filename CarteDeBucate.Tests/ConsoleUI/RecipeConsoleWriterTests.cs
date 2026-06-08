[Collection(ConsoleTestCollection.Name)]
public class RecipeConsoleWriterTests
{
    private static readonly object ConsoleLock = new();

    [Fact]
    public void ShowMenu_ShouldWriteMenuOptions()
    {
        string output = CaptureOutput(writer =>
            writer.ShowMenu(
            [
                new MenuOption { Key = "1", Text = "Add" },
                new MenuOption { Key = "2", Text = "Exit" }
            ]));

        Assert.Contains(AppTexts.AppTitle, output);
        Assert.Contains("1. Add", output);
        Assert.Contains("2. Exit", output);
        Assert.Contains(AppTexts.ChooseOption, output);
    }

    [Fact]
    public void DisplayRecipeDetails_ShouldWriteRecipeSections()
    {
        Recipe recipe = CreateRecipe();

        string output = CaptureOutput(writer => writer.DisplayRecipeDetails(recipe));

        Assert.Contains(AppTexts.RecipeDetailsTitle, output);
        Assert.Contains(recipe.Name, output);
        Assert.Contains(recipe.Ingredients[0], output);
        Assert.Contains(recipe.Steps[0], output);
        Assert.Contains(recipe.Notes, output);
    }

    [Fact]
    public void DisplaySearchResults_WhenNoRecipes_ShouldWriteEmptyMessage()
    {
        string output = CaptureOutput(writer => writer.DisplaySearchResults([]));

        Assert.Contains(AppTexts.NoSearchResults, output);
    }

    private static string CaptureOutput(Action<RecipeConsoleWriter> action)
    {
        lock (ConsoleLock)
        {
            TextWriter originalOutput = Console.Out;

            try
            {
                StringWriter output = new StringWriter();
                Console.SetOut(output);

                action(new RecipeConsoleWriter());

                return output.ToString();
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }
    }

    private static Recipe CreateRecipe()
    {
        return new Recipe
        {
            Id = 1,
            Name = "Cake",
            SourceUrl = "https://example.com/cake",
            SavedAt = new DateTime(2026, 6, 8, 10, 0, 0),
            Ingredients = ["Flour"],
            Steps = ["Bake"],
            Notes = "Good"
        };
    }
}

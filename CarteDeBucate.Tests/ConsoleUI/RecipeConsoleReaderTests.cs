[Collection(ConsoleTestCollection.Name)]
public class RecipeConsoleReaderTests
{
    private static readonly object ConsoleLock = new();

    [Fact]
    public void ReadRecipeFromConsole_WhenInputIsBlank_ShouldReturnNull()
    {
        lock (ConsoleLock)
        {
            TextReader originalInput = Console.In;
            TextWriter originalOutput = Console.Out;

            try
            {
                Console.SetIn(new StringReader(Environment.NewLine));
                Console.SetOut(new StringWriter());

                RecipeConsoleReader reader = new RecipeConsoleReader();

                Recipe? recipe = reader.ReadRecipeFromConsole();

                Assert.Null(recipe);
            }
            finally
            {
                Console.SetIn(originalInput);
                Console.SetOut(originalOutput);
            }
        }
    }

    [Fact]
    public void ReadRecipeFromConsole_ShouldReadPastedIngredientsAndSteps()
    {
        lock (ConsoleLock)
        {
            TextReader originalInput = Console.In;
            TextWriter originalOutput = Console.Out;

            try
            {
                string input = string.Join(
                    Environment.NewLine,
                    [
                        "Cake",
                        "https://example.com/cake",
                        "Flour",
                        "",
                        "Eggs",
                        "gata",
                        "Mix",
                        "",
                        "Bake",
                        "gata",
                        "Notes"
                    ]);

                Console.SetIn(new StringReader(input));
                Console.SetOut(new StringWriter());

                RecipeConsoleReader reader = new RecipeConsoleReader();

                Recipe? recipe = reader.ReadRecipeFromConsole();

                Assert.NotNull(recipe);
                Assert.Equal("Cake", recipe.Name);
                Assert.Equal("https://example.com/cake", recipe.SourceUrl);
                Assert.Equal(["Flour", "Eggs"], recipe.Ingredients);
                Assert.Equal(["Mix", "Bake"], recipe.Steps);
                Assert.Equal("Notes", recipe.Notes);
            }
            finally
            {
                Console.SetIn(originalInput);
                Console.SetOut(originalOutput);
            }
        }
    }

    [Fact]
    public void ReadRecipeIdToView_WhenInputIsZero_ShouldReturnNull()
    {
        lock (ConsoleLock)
        {
            TextReader originalInput = Console.In;
            TextWriter originalOutput = Console.Out;

            try
            {
                Console.SetIn(new StringReader("0"));
                Console.SetOut(new StringWriter());

                RecipeConsoleReader reader = new RecipeConsoleReader();

                int? recipeId = reader.ReadRecipeIdToView();

                Assert.Null(recipeId);
            }
            finally
            {
                Console.SetIn(originalInput);
                Console.SetOut(originalOutput);
            }
        }
    }

    [Theory]
    [InlineData("y", true)]
    [InlineData("nu", false)]
    public void AskForSaveConfirmation_ShouldReturnExpectedValue(string input, bool expectedResult)
    {
        lock (ConsoleLock)
        {
            TextReader originalInput = Console.In;
            TextWriter originalOutput = Console.Out;

            try
            {
                Console.SetIn(new StringReader(input));
                Console.SetOut(new StringWriter());

                RecipeConsoleReader reader = new RecipeConsoleReader();

                bool result = reader.AskForSaveConfirmation();

                Assert.Equal(expectedResult, result);
            }
            finally
            {
                Console.SetIn(originalInput);
                Console.SetOut(originalOutput);
            }
        }
    }
}

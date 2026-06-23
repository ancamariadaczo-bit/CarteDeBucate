

public class RecipeConsoleWriter : IRecipeConsoleWriter
{
    public void DisplayEmptyLine()
    {
        Console.WriteLine();
    }

    public void ShowMenu(List<MenuOption> options)
    {
        Console.WriteLine();
        Console.WriteLine(AppTexts.AppTitle);

        foreach (MenuOption option in options)
        {
            Console.WriteLine($"{option.Key}. {option.Text}");
        }

        Console.Write(AppTexts.ChooseOption);
    }

    public void DisplayMessage(string message)
    {
        Console.WriteLine(message);
    }

    public void DisplayRecipes(List<RecipeSummary> recipesToDisplay)
    {
        if (recipesToDisplay.Count == 0)
        {
            Console.WriteLine(AppTexts.NoRecipes);
            return;
        }

        Console.WriteLine(AppTexts.YourRecipes);
        Console.WriteLine(AppTexts.SeparatorLine);

        for (int i = 0; i < recipesToDisplay.Count; i++)
        {
            DisplayRecipe(recipesToDisplay[i], i + 1);
        }
    }

    public void DisplaySearchResults(List<RecipeSummary> foundRecipes)
    {
        if (foundRecipes.Count == 0)
        {
            Console.WriteLine(AppTexts.NoSearchResults);
            return;
        }

        Console.WriteLine();
        Console.WriteLine(AppTexts.SearchResults);
        Console.WriteLine(AppTexts.SeparatorLine);

        for (int i = 0; i < foundRecipes.Count; i++)
        {
            DisplayRecipeListItem(foundRecipes[i], i + 1);
        }
    }

    public void DisplayRecipe(RecipeSummary recipe, int index)
    {
        Console.WriteLine($"{index}. {recipe.Name} (ID: {recipe.Id})");
        WriteRecipeSummary(recipe);
        Console.WriteLine();
    }

    public void DisplayImportedRecipe(Recipe recipe)
    {
        Console.WriteLine();
        Console.WriteLine(AppTexts.ImportedRecipeTitle);
        Console.WriteLine();

        WriteRecipeDetailsBody(recipe, includeId: false, dateLabel: AppTexts.ImportedAtLabel);
        Console.WriteLine(AppTexts.ImportedRecipeEndLine);
    }

    public void DisplayRecipeList(List<RecipeSummary> recipesToDisplay)
    {
        if (recipesToDisplay.Count == 0)
        {
            Console.WriteLine(AppTexts.NoRecipes);
            return;
        }

        Console.WriteLine(AppTexts.YourRecipes);
        Console.WriteLine(AppTexts.SeparatorLine);

        for (int i = 0; i < recipesToDisplay.Count; i++)
        {
            RecipeSummary recipe = recipesToDisplay[i];

            DisplayRecipeListItem(recipe, i + 1);
        }
    }

    public void DisplayRecipeDetails(Recipe recipe)
    {
        Console.WriteLine();
        Console.WriteLine(AppTexts.RecipeDetailsTitle);
        Console.WriteLine();

        WriteRecipeDetailsBody(recipe, includeId: true, dateLabel: AppTexts.SavedAtLabel);
        Console.WriteLine(AppTexts.RecipeDetailsEndLine);
    }

    private static void WriteRecipeSummary(RecipeSummary recipe)
    {
        Console.WriteLine($"{AppTexts.SourceLabel}{recipe.SourceUrl}");
        Console.WriteLine($"{AppTexts.SavedAtLabel}{recipe.SavedAt}");
    }

    private static void DisplayRecipeListItem(RecipeSummary recipe, int index)
    {
        Console.WriteLine($"{index}. {recipe.Name} ({AppTexts.IdLabel}: {recipe.Id})");
        WriteRecipeSummary(recipe);
        Console.WriteLine();
    }

    private static void WriteRecipeDetailsBody(Recipe recipe, bool includeId, string dateLabel)
    {
        Console.WriteLine($"{AppTexts.NameLabel}{recipe.Name}");

        if (includeId)
        {
            Console.WriteLine($"{AppTexts.IdLabel}: {recipe.Id}");
        }

        Console.WriteLine($"{AppTexts.SourceLabel}{recipe.SourceUrl}");
        Console.WriteLine($"{dateLabel}{recipe.SavedAt}");

        Console.WriteLine();
        Console.WriteLine(AppTexts.IngredientsTitle);
        WriteBulletedListOrEmpty(recipe.Ingredients, AppTexts.NoIngredientsFound);

        Console.WriteLine();
        Console.WriteLine(AppTexts.StepsTitle);
        WriteNumberedListOrEmpty(recipe.Steps, AppTexts.NoStepsFound);

        if (!string.IsNullOrWhiteSpace(recipe.Notes))
        {
            Console.WriteLine();
            Console.WriteLine($"{AppTexts.NotesTitle}{recipe.Notes}");
        }

        Console.WriteLine();
    }

    private static void WriteBulletedListOrEmpty(List<string> values, string emptyMessage)
    {
        if (values.Count == 0)
        {
            Console.WriteLine(emptyMessage);
            return;
        }

        WriteBulletedList(values);
    }

    private static void WriteNumberedListOrEmpty(List<string> values, string emptyMessage)
    {
        if (values.Count == 0)
        {
            Console.WriteLine(emptyMessage);
            return;
        }

        WriteNumberedList(values);
    }

    private static void WriteBulletedList(List<string> values)
    {
        foreach (string value in values)
        {
            Console.WriteLine($"{AppTexts.ListItemPrefix}{value}");
        }
    }

    private static void WriteNumberedList(List<string> values, string prefix = "")
    {
        for (int i = 0; i < values.Count; i++)
        {
            Console.WriteLine($"{prefix}{i + 1}. {values[i]}");
        }
    }
}

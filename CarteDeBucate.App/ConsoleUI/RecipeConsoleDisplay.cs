public class RecipeConsoleDisplay
{
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

    public void DisplayRecipes(List<Recipe> recipesToDisplay)
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

    public void DisplaySearchResults(List<Recipe> foundRecipes)
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
            DisplayRecipe(foundRecipes[i], i + 1);
        }
    }

    public void DisplayRecipe(Recipe recipe, int index)
    {
        Console.WriteLine($"{index}. {recipe.Name} (ID: {recipe.Id})");
        Console.WriteLine($"{AppTexts.SourceLabel}{recipe.SourceUrl}");
        Console.WriteLine($"{AppTexts.SavedAtLabel}{recipe.SavedAt}");

        Console.WriteLine(AppTexts.IngredientsLabel);

        foreach (string ingredient in recipe.Ingredients)
        {
            Console.WriteLine($"{AppTexts.ListItemPrefix}{ingredient}");
        }

        Console.WriteLine(AppTexts.StepsLabel);

        for (int stepIndex = 0; stepIndex < recipe.Steps.Count; stepIndex++)
        {
            Console.WriteLine($"   {stepIndex + 1}. {recipe.Steps[stepIndex]}");
        }

        if (!string.IsNullOrWhiteSpace(recipe.Notes))
        {
            Console.WriteLine($"{AppTexts.NotesLabel}{recipe.Notes}");
        }

        Console.WriteLine();
    }

    public void DisplayImportedRecipe(Recipe recipe)
    {
        Console.WriteLine();
        Console.WriteLine(AppTexts.ImportedRecipeTitle);
        Console.WriteLine();

        Console.WriteLine($"{AppTexts.NameLabel}{recipe.Name}");
        Console.WriteLine($"{AppTexts.SourceLabel}{recipe.SourceUrl}");
        Console.WriteLine($"{AppTexts.ImportedAtLabel}{recipe.SavedAt}");

        Console.WriteLine();
        Console.WriteLine(AppTexts.IngredientsTitle);

        if (recipe.Ingredients.Count == 0)
        {
            Console.WriteLine(AppTexts.NoIngredientsFound);
        }
        else
        {
            foreach (string ingredient in recipe.Ingredients)
            {
                Console.WriteLine($"{AppTexts.ListItemPrefix}{ingredient}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(AppTexts.StepsTitle);

        if (recipe.Steps.Count == 0)
        {
            Console.WriteLine(AppTexts.NoStepsFound);
        }
        else
        {
            for (int i = 0; i < recipe.Steps.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {recipe.Steps[i]}");
            }
        }

        if (!string.IsNullOrWhiteSpace(recipe.Notes))
        {
            Console.WriteLine();
            Console.WriteLine($"{AppTexts.NotesTitle}{recipe.Notes}");
        }

        Console.WriteLine();
        Console.WriteLine(AppTexts.ImportedRecipeEndLine);
    }

    public void DisplayRecipeList(List<Recipe> recipesToDisplay)
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
            Recipe recipe = recipesToDisplay[i];

            Console.WriteLine($"{i + 1}. {recipe.Name} ({AppTexts.IdLabel}: {recipe.Id})");
            Console.WriteLine($"{AppTexts.SourceLabel}{recipe.SourceUrl}");
            Console.WriteLine($"{AppTexts.SavedAtLabel}{recipe.SavedAt}");
            Console.WriteLine();
        }
    }

    public void DisplayRecipeDetails(Recipe recipe)
    {
        Console.WriteLine();
        Console.WriteLine(AppTexts.RecipeDetailsTitle);
        Console.WriteLine();

        Console.WriteLine($"{AppTexts.NameLabel}{recipe.Name}");
        Console.WriteLine($"{AppTexts.IdLabel}: {recipe.Id}");
        Console.WriteLine($"{AppTexts.SourceLabel}{recipe.SourceUrl}");
        Console.WriteLine($"{AppTexts.SavedAtLabel}{recipe.SavedAt}");

        Console.WriteLine();
        Console.WriteLine(AppTexts.IngredientsTitle);

        if (recipe.Ingredients.Count == 0)
        {
            Console.WriteLine(AppTexts.NoIngredientsFound);
        }
        else
        {
            foreach (string ingredient in recipe.Ingredients)
            {
                Console.WriteLine($"{AppTexts.ListItemPrefix}{ingredient}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(AppTexts.StepsTitle);

        if (recipe.Steps.Count == 0)
        {
            Console.WriteLine(AppTexts.NoStepsFound);
        }
        else
        {
            for (int i = 0; i < recipe.Steps.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {recipe.Steps[i]}");
            }
        }

        if (!string.IsNullOrWhiteSpace(recipe.Notes))
        {
            Console.WriteLine();
            Console.WriteLine($"{AppTexts.NotesTitle}{recipe.Notes}");
        }

        Console.WriteLine();
        Console.WriteLine(AppTexts.RecipeDetailsEndLine);
    }
}
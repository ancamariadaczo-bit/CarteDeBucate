public class RecipeConsoleReader : IRecipeConsoleReader
{
    public string ReadMenuOption()
    {
        return Console.ReadLine() ?? string.Empty;
    }

    public Recipe? ReadRecipeFromConsole()
    {
        Recipe recipe = new Recipe();

        Console.WriteLine(AppTexts.BackToMainMenuHint);
        Console.Write(AppTexts.EnterRecipeName);
        recipe.Name = Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(recipe.Name))
        {
            return null;
        }

        Console.Write(AppTexts.EnterSourceUrl);
        recipe.SourceUrl = Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(recipe.SourceUrl))
        {
            return null;
        }

        Console.WriteLine();
        Console.WriteLine(AppTexts.EnterIngredientsIntro);
        Console.WriteLine(AppTexts.EmptyLineToFinish);

        recipe.Ingredients = ReadMultipleLines(AppTexts.IngredientPrompt);

        Console.WriteLine();
        Console.WriteLine(AppTexts.EnterStepsIntro);
        Console.WriteLine(AppTexts.EmptyLineToFinish);

        recipe.Steps = ReadMultipleLines(AppTexts.StepPrompt);

        Console.WriteLine();
        Console.Write(AppTexts.EnterNotes);
        recipe.Notes = Console.ReadLine() ?? "";

        recipe.SavedAt = DateTime.Now;

        return recipe;
    }

    public string ReadRecipeUrlToImport()
    {
        Console.WriteLine(AppTexts.BackToMainMenuHint);
        Console.Write(AppTexts.EnterRecipeUrlToImport);

        return Console.ReadLine() ?? "";
    }

    public string ReadSearchText()
    {
        Console.WriteLine(AppTexts.BackToMainMenuHint);
        Console.Write(AppTexts.SearchPrompt);

        return Console.ReadLine() ?? "";
    }

    public void CompleteImportedRecipeFromConsole(Recipe recipe)
    {
        Console.WriteLine();
        Console.WriteLine(AppTexts.KeepImportedIngredientsPrompt);
        Console.WriteLine(AppTexts.EditImportedIngredientsPrompt);
        Console.Write(AppTexts.ConsolePrompt);

        string shouldEditIngredients = Console.ReadLine() ?? "";

        if (!string.IsNullOrWhiteSpace(shouldEditIngredients))
        {
            Console.WriteLine();
            Console.WriteLine(AppTexts.EnterIngredientsIntro);
            Console.WriteLine(AppTexts.EmptyLineToFinish);

            recipe.Ingredients = ReadMultipleLines(AppTexts.IngredientPrompt);
        }

        Console.WriteLine();
        Console.WriteLine(AppTexts.KeepImportedStepsPrompt);
        Console.WriteLine(AppTexts.EditImportedStepsPrompt);
        Console.Write(AppTexts.ConsolePrompt);

        string shouldEditSteps = Console.ReadLine() ?? "";

        if (!string.IsNullOrWhiteSpace(shouldEditSteps))
        {
            Console.WriteLine();
            Console.WriteLine(AppTexts.EnterStepsIntro);
            Console.WriteLine(AppTexts.EmptyLineToFinish);

            recipe.Steps = ReadMultipleLines(AppTexts.StepPrompt);
        }

        Console.WriteLine();
        Console.Write(AppTexts.EnterNotes);
        recipe.Notes = Console.ReadLine() ?? "";

        recipe.SavedAt = DateTime.Now;
    }

    public int? ReadRecipeIdToEdit()
    {
        return ReadRecipeId(AppTexts.EnterRecipeIdToEdit);
    }

    public Recipe? ReadRecipeEditsFromConsole(Recipe recipe)
    {
        Console.WriteLine();
        Console.WriteLine(AppTexts.EditRecipeTitle);
        Console.WriteLine(AppTexts.KeepCurrentValuePrompt);
        Console.WriteLine(AppTexts.BackToMainMenuEditHint);

        Console.WriteLine();
        Console.WriteLine($"{AppTexts.CurrentValueLabel}{recipe.Name}");
        Console.Write(AppTexts.EnterRecipeName);
        string name = Console.ReadLine() ?? "";

        if (IsBackToMainMenuCommand(name))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            recipe.Name = name;
        }

        Console.WriteLine();
        Console.WriteLine($"{AppTexts.CurrentValueLabel}{recipe.SourceUrl}");
        Console.Write(AppTexts.EnterSourceUrl);
        string sourceUrl = Console.ReadLine() ?? "";

        if (!string.IsNullOrWhiteSpace(sourceUrl))
        {
            recipe.SourceUrl = sourceUrl;
        }

        Console.WriteLine();
        Console.WriteLine($"{AppTexts.CurrentValueLabel}{recipe.Notes}");
        Console.Write(AppTexts.EnterNotes);
        string notes = Console.ReadLine() ?? "";

        if (!string.IsNullOrWhiteSpace(notes))
        {
            recipe.Notes = notes;
        }

        Console.WriteLine();
        Console.WriteLine(AppTexts.EditIngredientsPrompt);
        Console.Write(AppTexts.ConsolePrompt);
        string shouldEditIngredients = Console.ReadLine() ?? "";

        if (shouldEditIngredients.Equals("da", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine();
            Console.WriteLine(AppTexts.EnterIngredientsIntro);
            Console.WriteLine(AppTexts.EmptyLineToFinish);

            recipe.Ingredients = ReadMultipleLines(AppTexts.IngredientPrompt);
        }

        Console.WriteLine();
        Console.WriteLine(AppTexts.EditStepsPrompt);
        Console.Write(AppTexts.ConsolePrompt);
        string shouldEditSteps = Console.ReadLine() ?? "";

        if (shouldEditSteps.Equals("da", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine();
            Console.WriteLine(AppTexts.EnterStepsIntro);
            Console.WriteLine(AppTexts.EmptyLineToFinish);

            recipe.Steps = ReadMultipleLines(AppTexts.StepPrompt);
        }

        recipe.SavedAt = DateTime.Now;

        return recipe;
    }

    private List<string> ReadMultipleLines(string prompt)
    {
        List<string> values = new List<string>();

        Console.WriteLine(prompt);

        while (true)
        {
            string value = Console.ReadLine() ?? "";

            if (value.Trim().Equals(
                AppTexts.FinishMultilineInputCommand,
                StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            values.AddRange(ParseInputLines(value));
        }

        return values;
    }

    private static List<string> ParseInputLines(string input)
    {
        return input
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    public int? ReadRecipeIdToDelete()
    {
        return ReadRecipeId(AppTexts.EnterRecipeIdToDelete);
    }

    public int? ReadRecipeIdToView()
    {
        return ReadRecipeId(AppTexts.EnterRecipeIdToView);
    }

    public PaginationAction ReadPaginationAction()
    {
        Console.WriteLine(AppTexts.PaginationNextOption);
        Console.WriteLine(AppTexts.PaginationPreviousOption);
        Console.WriteLine(AppTexts.PaginationBackOption);
        Console.Write(AppTexts.PaginationPrompt);

        string input = Console.ReadLine() ?? "";

        return input.Trim().ToLowerInvariant() switch
        {
            "n" => PaginationAction.NextPage,
            "p" => PaginationAction.PreviousPage,
            _ => PaginationAction.BackToMenu
        };
    }

    public bool AskForSaveConfirmation()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine(AppTexts.SaveRecipeQuestion);
            Console.Write(AppTexts.ConsolePrompt);

            string? input = Console.ReadLine()?.Trim().ToLower();

            if (IsAffirmativeAnswer(input))
            {
                return true;
            }

            if (IsNegativeAnswer(input))
            {
                return false;
            }

            Console.WriteLine();
            Console.WriteLine(AppTexts.InvalidSaveOption);
        }
    }

    public string ReadBackupFilePath()
    {
        Console.WriteLine(AppTexts.BackToMainMenuHint);
        Console.Write(AppTexts.BackupPrompt);

        return Console.ReadLine() ?? "";
    }

    private static int? ReadRecipeId(string prompt)
    {
        Console.WriteLine(AppTexts.BackToMainMenuIdHint);
        Console.Write(prompt);

        string input = Console.ReadLine() ?? "";

        if (IsBackToMainMenuId(input))
        {
            return null;
        }

        if (!int.TryParse(input, out int recipeId))
        {
            return -1;
        }

        return recipeId;
    }

    private static bool IsBackToMainMenuId(string input)
    {
        return string.IsNullOrWhiteSpace(input) || input.Trim() == "0";
    }

    private static bool IsBackToMainMenuCommand(string input)
    {
        return input.Trim().Equals(AppTexts.BackToMainMenuInput, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAffirmativeAnswer(string? input)
    {
        return input == "y" || input == "yes" || input == "da" || input == "d";
    }

    private static bool IsNegativeAnswer(string? input)
    {
        return input == "n" || input == "no" || input == "nu";
    }
}

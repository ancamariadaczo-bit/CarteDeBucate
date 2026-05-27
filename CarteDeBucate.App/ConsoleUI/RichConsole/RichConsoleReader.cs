using Spectre.Console;

public class RichConsoleReader : IRichConsoleReader
{
    public Recipe ReadRecipe()
    {
        return new Recipe
        {
            Name = ReadRequiredText(AppTexts.EnterRecipeName),
            SourceUrl = ReadRequiredText(AppTexts.EnterSourceUrl),
            Ingredients = ReadIngredients(),
            Steps = ReadSteps(),
            Notes = ReadNotes(),
            SavedAt = DateTime.Now
        };
    }

    public string ReadRecipeUrlToImport()
    {
        return ReadOptionalText(AppTexts.EnterRecipeUrlToImport);
    }

    public string ReadSearchText()
    {
        return ReadOptionalText(AppTexts.SearchPrompt);
    }

    public bool ConfirmKeepImportedIngredients()
    {
        return Confirm("Păstrezi ingredientele importate?", true);
    }

    public bool ConfirmKeepImportedSteps()
    {
        return Confirm("Păstrezi pașii importați?", true);
    }

    public List<string> ReadIngredients()
    {
        return ReadMultipleLines(AppTexts.EnterIngredientsIntro, AppTexts.IngredientPrompt);
    }

    public List<string> ReadSteps()
    {
        return ReadMultipleLines(AppTexts.EnterStepsIntro, AppTexts.StepPrompt);
    }

    public string ReadNotes()
    {
        return ReadOptionalText(AppTexts.EnterNotes);
    }

    public bool ConfirmSaveRecipe()
    {
        return Confirm("Dorești să salvezi această rețetă?", true);
    }

    public Recipe SelectRecipe(List<Recipe> recipes, string title)
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<Recipe>()
                .Title(Markup.Escape(title))
                .PageSize(10)
                .UseConverter(CreateRecipeSelectionText)
                .AddChoices(recipes));
    }

    public Recipe ReadRecipeEdits(Recipe recipe)
    {
        string name = ReadOptionalText($"{AppTexts.EnterRecipeName}({AppTexts.KeepCurrentValuePrompt})");
        if (!string.IsNullOrWhiteSpace(name))
        {
            recipe.Name = name;
        }

        string sourceUrl = ReadOptionalText($"{AppTexts.EnterSourceUrl}({AppTexts.KeepCurrentValuePrompt})");
        if (!string.IsNullOrWhiteSpace(sourceUrl))
        {
            recipe.SourceUrl = sourceUrl;
        }

        string notes = ReadOptionalText($"{AppTexts.EnterNotes}({AppTexts.KeepCurrentValuePrompt})");
        if (!string.IsNullOrWhiteSpace(notes))
        {
            recipe.Notes = notes;
        }

        if (Confirm("Vrei să editezi ingredientele?", false))
        {
            recipe.Ingredients = ReadIngredients();
        }

        if (Confirm("Vrei să editezi pașii?", false))
        {
            recipe.Steps = ReadSteps();
        }

        recipe.SavedAt = DateTime.Now;

        return recipe;
    }

    public bool ConfirmDeleteRecipe(Recipe recipe)
    {
        return Confirm($"Sigur ștergi rețeta \"{recipe.Name}\"?", false);
    }

    public string ReadBackupFilePath()
    {
        return ReadOptionalText(AppTexts.BackupPrompt);
    }

    public void WaitForContinue()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(CreateMarkupMessage(RichConsoleTexts.UserOptionMarkupStart, "Apasă Enter pentru a continua..."));
        Console.ReadLine();
    }

    private static string ReadRequiredText(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>(Markup.Escape(prompt))
                .Validate(value => string.IsNullOrWhiteSpace(value)
                    ? ValidationResult.Error(Markup.Escape(AppTexts.RecipeNameRequired))
                    : ValidationResult.Success()));
    }

    private static string ReadOptionalText(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>(Markup.Escape(prompt))
                .AllowEmpty());
    }

    private static List<string> ReadMultipleLines(string intro, string prompt)
    {
        List<string> values = new();

        AnsiConsole.MarkupLine(CreateMarkupMessage(RichConsoleTexts.InfoMarkupStart, intro));
        AnsiConsole.MarkupLine(CreateMarkupMessage(RichConsoleTexts.UserOptionMarkupStart, AppTexts.EmptyLineToFinish));

        while (true)
        {
            string value = ReadOptionalText(prompt);

            if (string.IsNullOrWhiteSpace(value))
            {
                break;
            }

            values.Add(value);
        }

        return values;
    }

    private static bool Confirm(string prompt, bool defaultValue)
    {
        return AnsiConsole.Prompt(
            new ConfirmationPrompt(Markup.Escape(prompt))
            {
                DefaultValue = defaultValue
            });
    }

    private static string CreateRecipeSelectionText(Recipe recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe.SourceUrl))
        {
            return recipe.Name;
        }

        return string.Concat(recipe.Name, RichConsoleTexts.SelectionDetailsSeparator, recipe.SourceUrl);
    }

    private static string CreateMarkupMessage(string markupStart, string message)
    {
        return string.Concat(markupStart, Markup.Escape(message), RichConsoleTexts.MarkupEnd);
    }
}

using Spectre.Console;

public class RichConsoleReader : IRichConsoleReader
{
    public Recipe? ReadRecipe()
    {
        ShowBackToMainMenuHint();

        string name = ReadOptionalText(AppTexts.EnterRecipeName);
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        string sourceUrl = ReadOptionalText(AppTexts.EnterSourceUrl);
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            return null;
        }

        return new Recipe
        {
            Name = name,
            SourceUrl = sourceUrl,
            Ingredients = ReadIngredients(),
            Steps = ReadSteps(),
            Notes = ReadNotes(),
            SavedAt = DateTime.Now
        };
    }

    public string? ReadRecipeUrlToImport()
    {
        ShowBackToMainMenuHint();

        return ReadOptionalText(AppTexts.EnterRecipeUrlToImport);
    }

    public string? ReadSearchText()
    {
        ShowBackToMainMenuHint();

        return ReadOptionalText(AppTexts.SearchPrompt);
    }

    public bool ConfirmKeepImportedIngredients()
    {
        return Confirm(RichConsoleTexts.KeepImportedIngredientsQuestion, true);
    }

    public bool ConfirmKeepImportedSteps()
    {
        return Confirm(RichConsoleTexts.KeepImportedStepsQuestion, true);
    }

    public List<string> ReadIngredients()
    {
        return ReadMultipleLines(RichConsoleTexts.EnterIngredientsIntro, AppTexts.IngredientPrompt);
    }

    public List<string> ReadSteps()
    {
        return ReadMultipleLines(RichConsoleTexts.EnterStepsIntro, AppTexts.StepPrompt);
    }

    public string ReadNotes()
    {
        return ReadOptionalText(AppTexts.EnterNotes);
    }

    public bool ConfirmSaveRecipe()
    {
        return Confirm(RichConsoleTexts.SaveRecipeQuestion, true);
    }

    public bool ConfirmImportAnotherRecipe()
    {
        return SelectFollowUpAction(RichConsoleTexts.ImportAnotherRecipeOption);
    }

    public bool ConfirmSearchAnotherRecipe()
    {
        return SelectFollowUpAction(RichConsoleTexts.SearchAnotherRecipeOption);
    }

    public Recipe? SelectRecipe(List<Recipe> recipes, string title)
    {
        List<RecipeSelection> choices = recipes
            .Select(recipe => new RecipeSelection(recipe))
            .ToList();

        choices.Add(RecipeSelection.BackToMainMenu);

        RecipeSelection selection = AnsiConsole.Prompt(
            new SelectionPrompt<RecipeSelection>()
                .Title(Markup.Escape(title))
                .PageSize(10)
                .UseConverter(selection => selection.Text)
                .AddChoices(choices));

        return selection.Recipe;
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

        if (Confirm(RichConsoleTexts.EditIngredientsQuestion, false))
        {
            recipe.Ingredients = ReadIngredients();
        }

        if (Confirm(RichConsoleTexts.EditStepsQuestion, false))
        {
            recipe.Steps = ReadSteps();
        }

        recipe.SavedAt = DateTime.Now;

        return recipe;
    }

    public bool ConfirmDeleteRecipe(Recipe recipe)
    {
        return Confirm(string.Format(RichConsoleTexts.DeleteRecipeConfirmation, recipe.Name), false);
    }

    public string? ReadBackupFilePath()
    {
        ShowBackToMainMenuHint();

        return ReadOptionalText(AppTexts.BackupPrompt);
    }

    public void WaitForContinue()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(CreateMarkupMessage(
            RichConsoleTexts.UserOptionMarkupStart,
            RichConsoleTexts.ReturnToMainMenuPrompt));
        Console.ReadLine();
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
        AnsiConsole.MarkupLine(CreateMarkupMessage(
            RichConsoleTexts.UserOptionMarkupStart,
            RichConsoleTexts.FinishMultilineInputHint));
        AnsiConsole.MarkupLine(Markup.Escape(prompt));

        while (true)
        {
            string value = Console.ReadLine() ?? string.Empty;

            if (value.Trim().Equals(
                RichConsoleTexts.FinishMultilineInputCommand,
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
            return Markup.Escape(recipe.Name);
        }

        return string.Concat(
            Markup.Escape(recipe.Name),
            RichConsoleTexts.SelectionDetailsSeparator,
            Markup.Escape(recipe.SourceUrl));
    }

    private static string CreateMarkupMessage(string markupStart, string message)
    {
        return string.Concat(markupStart, Markup.Escape(message), RichConsoleTexts.MarkupEnd);
    }

    private static void ShowBackToMainMenuHint()
    {
        AnsiConsole.MarkupLine(CreateMarkupMessage(
            RichConsoleTexts.UserOptionMarkupStart,
            RichConsoleTexts.EmptyInputReturnsToMainMenu));
    }

    private static bool SelectFollowUpAction(string continueOption)
    {
        string selectedOption = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(CreateUserOptionTitle(RichConsoleTexts.NextActionTitle))
                .PageSize(3)
                .AddChoices(continueOption, CreateBackToMainMenuOption()));

        return selectedOption == continueOption;
    }

    private sealed class RecipeSelection
    {
        public static readonly RecipeSelection BackToMainMenu = new RecipeSelection(
            null,
            CreateBackToMainMenuOption());

        public RecipeSelection(Recipe recipe)
            : this(recipe, CreateRecipeSelectionText(recipe))
        {
        }

        private RecipeSelection(Recipe? recipe, string text)
        {
            Recipe = recipe;
            Text = text;
        }

        public Recipe? Recipe { get; }

        public string Text { get; }
    }

    private static string CreateUserOptionTitle(string text)
    {
        return string.Concat(RichConsoleTexts.UserOptionTitleMarkupStart, Markup.Escape(text), RichConsoleTexts.MarkupEnd);
    }

    private static string CreateBackToMainMenuOption()
    {
        return string.Concat(
            RichConsoleTexts.BackToMainMenuMarkupStart,
            Markup.Escape(RichConsoleTexts.BackToMainMenuOption),
            RichConsoleTexts.MarkupEnd);
    }
}

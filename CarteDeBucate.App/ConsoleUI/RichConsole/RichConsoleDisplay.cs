using Spectre.Console;

public class RichConsoleDisplay : IRichConsoleDisplay
{
    public void Clear()
    {
        AnsiConsole.Clear();
        Console.Write("\u001b[2J\u001b[3J\u001b[H");
        Console.Out.Flush();
    }

    public void ShowTitle()
    {
        AnsiConsole.Clear();

        AnsiConsole.Write(
            new FigletText(RichConsoleTexts.AppTitle)
                .Centered()
                .Color(Color.Green));

        AnsiConsole.WriteLine();
    }

    public void ShowRecipes(List<RecipeSummary> recipes, string? title = null, string? emptyMessage = null)
    {
        if (recipes.Count == 0)
        {
            ShowInfo(emptyMessage ?? RichConsoleTexts.NoRecipes);
            return;
        }

        Table table = new Table()
            .Title(Markup.Escape(title ?? RichConsoleTexts.YourRecipesTitle))
            .Border(TableBorder.Rounded);

        table.AddColumn(RichConsoleTexts.NameColumn);
        table.AddColumn(RichConsoleTexts.SourceColumn);
        table.AddColumn(RichConsoleTexts.SavedAtColumn);

        for (int index = 0; index < recipes.Count; index++)
        {
            RecipeSummary recipe = recipes[index];

            table.AddRow(
                Markup.Escape(recipe.Name ?? RichConsoleTexts.EmptyValue),
                Markup.Escape(recipe.SourceUrl ?? RichConsoleTexts.EmptyValue),
                Markup.Escape(recipe.SavedAt.ToString(RichConsoleTexts.SavedAtDateFormat)));

            if (index < recipes.Count - 1)
            {
                table.AddEmptyRow();
            }
        }

        AnsiConsole.Write(table);
    }

    public void ShowRecipeDetails(Recipe recipe, string? title = null)
    {
        Grid metadata = new Grid();
        metadata.AddColumn();
        metadata.AddColumn();
        metadata.AddRow(RichConsoleTexts.NameColumn, Markup.Escape(recipe.Name));
        metadata.AddRow(RichConsoleTexts.SourceColumn, Markup.Escape(recipe.SourceUrl));
        metadata.AddRow(
            RichConsoleTexts.SavedAtColumn,
            Markup.Escape(recipe.SavedAt.ToString(RichConsoleTexts.SavedAtDateFormat)));

        if (!string.IsNullOrWhiteSpace(recipe.Notes))
        {
            metadata.AddRow(RichConsoleTexts.NotesRow, Markup.Escape(recipe.Notes));
        }

        AnsiConsole.Write(new Panel(metadata)
            .Header(Markup.Escape(title ?? RichConsoleTexts.RecipeDetailsTitle))
            .Border(BoxBorder.Rounded));

        WriteListSection(RichConsoleTexts.IngredientsTitle, recipe.Ingredients, false);
        WriteListSection(RichConsoleTexts.StepsTitle, recipe.Steps, true);
    }

    public void ShowImportedRecipe(Recipe recipe)
    {
        ShowRecipeDetails(recipe, RichConsoleTexts.ImportedRecipeTitle);
    }

    public void ShowSuccess(string message)
    {
        AnsiConsole.MarkupLine(CreateMarkupMessage(RichConsoleTexts.SuccessMarkupStart, message));
    }

    public void ShowSpacedSuccess(string message)
    {
        AnsiConsole.WriteLine();
        ShowSuccess(message);
        AnsiConsole.WriteLine();
    }

    public void ShowError(string message)
    {
        AnsiConsole.MarkupLine(CreateMarkupMessage(RichConsoleTexts.ErrorMarkupStart, message));
    }

    public void ShowInfo(string message)
    {
        AnsiConsole.MarkupLine(CreateMarkupMessage(RichConsoleTexts.InfoMarkupStart, message));
    }

    private static void WriteListSection(string title, List<string> values, bool isNumbered)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(CreateMarkupMessage(RichConsoleTexts.SectionTitleMarkupStart, title));
        AnsiConsole.Write(CreateListTable(values, isNumbered));
    }

    private static Table CreateListTable(List<string> values, bool isNumbered)
    {
        Table table = new Table()
            .Border(TableBorder.Simple)
            .HideHeaders();

        table.AddColumn(isNumbered ? RichConsoleTexts.NumberColumn : RichConsoleTexts.EmptyColumn);
        table.AddColumn(RichConsoleTexts.TextColumn);

        if (values.Count == 0)
        {
            table.AddRow(
                RichConsoleTexts.EmptyColumn,
                Markup.Escape(isNumbered ? RichConsoleTexts.NoStepsFound : RichConsoleTexts.NoIngredientsFound));
            return table;
        }

        for (int index = 0; index < values.Count; index++)
        {
            table.AddRow(
                isNumbered ? (index + 1).ToString() : RichConsoleTexts.BulletPrefix,
                Markup.Escape(values[index]));
        }

        return table;
    }

    private static string CreateMarkupMessage(string markupStart, string message)
    {
        return string.Concat(markupStart, Markup.Escape(message), RichConsoleTexts.MarkupEnd);
    }
}

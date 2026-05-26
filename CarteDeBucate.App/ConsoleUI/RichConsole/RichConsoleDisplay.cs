using Spectre.Console;

public class RichConsoleDisplay
{
    public void ShowTitle()
    {
        AnsiConsole.Clear();

        AnsiConsole.Write(
            new FigletText("Carte de bucate")
                .Centered()
                .Color(Color.Green));

        AnsiConsole.WriteLine();
    }

    public void ShowRecipes(List<Recipe> recipes)
    {
        if (recipes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Nu există rețete salvate.[/]");
            return;
        }

        Table table = new Table();

        table.AddColumn("Id");
        table.AddColumn("Nume");
        table.AddColumn("Sursă");
        table.AddColumn("Salvată la");

        foreach (Recipe recipe in recipes)
        {
            table.AddRow(
                recipe.Id.ToString(),
                recipe.Name ?? "",
                recipe.SourceUrl ?? "",
                recipe.SavedAt.ToString("dd.MM.yyyy"));
        }

        AnsiConsole.Write(table);
    }

    public void ShowSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]{message}[/]");
    }

    public void ShowError(string message)
    {
        AnsiConsole.MarkupLine($"[red]{message}[/]");
    }

    public void ShowInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue]{message}[/]");
    }
}
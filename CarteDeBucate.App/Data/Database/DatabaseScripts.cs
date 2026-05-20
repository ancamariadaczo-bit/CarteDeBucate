public static class DatabaseScripts
{
    public static readonly string[] CreateTables =
    [
        CreateRecipesTable,
        CreateRecipeIngredientsTable,
        CreateRecipeStepsTable
    ];

    public const string CreateRecipesTable = """
    CREATE TABLE IF NOT EXISTS Recipes (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL,
        SourceUrl TEXT NOT NULL,
        SavedAt TEXT NOT NULL,
        Notes TEXT NULL
    );
    """;

    public const string CreateRecipeIngredientsTable = """
    CREATE TABLE IF NOT EXISTS RecipeIngredients (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        RecipeId INTEGER NOT NULL,
        IngredientOrder INTEGER NOT NULL,
        IngredientText TEXT NOT NULL,
        FOREIGN KEY (RecipeId) REFERENCES Recipes(Id)
    );
    """;

    public const string CreateRecipeStepsTable = """
    CREATE TABLE IF NOT EXISTS RecipeSteps (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        RecipeId INTEGER NOT NULL,
        StepOrder INTEGER NOT NULL,
        StepText TEXT NOT NULL,
        FOREIGN KEY (RecipeId) REFERENCES Recipes(Id)
    );
    """;
}
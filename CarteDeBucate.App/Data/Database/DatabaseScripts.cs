public static class DatabaseScripts
{
    public static readonly string[] CreateTables =
    [
        CreateRecipesTable,
        CreateRecipeIngredientsTable,
        CreateRecipeStepsTable,
        CreateUsersTable
    ];

    public const string CreateRecipesTable = """
    CREATE TABLE IF NOT EXISTS Recipes (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL,
        SourceUrl TEXT NOT NULL,
        SavedAt TEXT NOT NULL,
        Notes TEXT NULL,
        Status INTEGER NOT NULL DEFAULT 0
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

    public const string CreateSchemaMigrationsTable = """
    CREATE TABLE IF NOT EXISTS SchemaMigrations (
        Version INTEGER PRIMARY KEY,
        Name TEXT NOT NULL,
        AppliedAt TEXT NOT NULL
    );
    """;

    public const string AddRecipeStatusColumn = """
    ALTER TABLE Recipes
    ADD COLUMN Status INTEGER NOT NULL DEFAULT 0;
    """;

    public const string CreateUsersTable = """
    CREATE TABLE IF NOT EXISTS Users (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Username TEXT NOT NULL UNIQUE,
        PasswordHash TEXT NOT NULL,
        PasswordSalt TEXT NOT NULL,
        CreatedAt TEXT NOT NULL
    );
    """;
}
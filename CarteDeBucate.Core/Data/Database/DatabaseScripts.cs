public static class DatabaseScripts
{
    public static readonly string[] CreateTables =
    [
        CreateUsersTable,
        CreateUsersEmailNoCaseUniqueIndex,
        CreateUserEmailTokensSchema,
        CreateRecipesTable,
        CreateRecipePhotosTable,
        CreateRecipePhotosIndex,
        CreateRecipeIngredientsTable,
        CreateRecipeStepsTable
    ];

    public const string CreateRecipesTable = """
    CREATE TABLE IF NOT EXISTS Recipes (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL,
        SourceUrl TEXT NULL,
        SavedAt TEXT NOT NULL,
        Notes TEXT NULL,
        Status INTEGER NOT NULL DEFAULT 0,
        UserId INTEGER NULL,
        FOREIGN KEY (UserId) REFERENCES Users(Id)
    );
    """;

    public const string CreateRecipePhotosTable = """
    CREATE TABLE IF NOT EXISTS RecipePhotos (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        RecipeId INTEGER NOT NULL,
        StorageFileName TEXT NOT NULL UNIQUE
            CHECK (length(trim(StorageFileName)) BETWEEN 1 AND 128),
        OriginalFileName TEXT NOT NULL
            CHECK (length(trim(OriginalFileName)) BETWEEN 1 AND 255),
        ContentType TEXT NOT NULL
            CHECK (length(trim(ContentType)) BETWEEN 1 AND 100),
        FileSize INTEGER NOT NULL
            CHECK (FileSize > 0),
        CreatedAtUtc TEXT NOT NULL,
        DisplayOrder INTEGER NOT NULL DEFAULT 0
            CHECK (DisplayOrder >= 0),
        Origin INTEGER NOT NULL DEFAULT 0,
        FOREIGN KEY (RecipeId)
            REFERENCES Recipes(Id)
            ON DELETE CASCADE
    );
    """;

    public const string CreateRecipePhotosIndex = """
    CREATE INDEX IF NOT EXISTS IX_RecipePhotos_RecipeId_DisplayOrder_Id
    ON RecipePhotos (RecipeId, DisplayOrder, Id);
    """;

    public const string CreateRecipePhotosSchema =
        CreateRecipePhotosTable + "\n" + CreateRecipePhotosIndex;

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
        Email TEXT NOT NULL COLLATE NOCASE,
        PasswordHash TEXT NOT NULL,
        PasswordSalt TEXT NOT NULL,
        CreatedAt TEXT NOT NULL,
        EmailVerifiedAtUtc TEXT NULL,
        PreferredCulture TEXT NULL
    );
    """;

    public const string CreateUsersEmailNoCaseUniqueIndex = """
    CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email_NoCase
        ON Users (Email COLLATE NOCASE);
    """;

    public const string CreateUserEmailTokensTable = """
    CREATE TABLE IF NOT EXISTS UserEmailTokens (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        UserId INTEGER NOT NULL,
        Purpose INTEGER NOT NULL,
        TokenHash BLOB NOT NULL,
        TargetEmail TEXT NOT NULL COLLATE NOCASE,
        Culture TEXT NOT NULL,
        CreatedAtUtc TEXT NOT NULL,
        ExpiresAtUtc TEXT NOT NULL,
        UsedAtUtc TEXT NULL,
        RevokedAtUtc TEXT NULL,
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
        CHECK (Purpose IN (1, 2)),
        CHECK (length(TokenHash) = 32)
    );
    """;

    public const string CreateUserEmailTokensTokenHashIndex = """
    CREATE UNIQUE INDEX IF NOT EXISTS IX_UserEmailTokens_TokenHash
        ON UserEmailTokens (TokenHash);
    """;

    public const string CreateUserEmailTokensActivePurposeIndex = """
    CREATE UNIQUE INDEX IF NOT EXISTS IX_UserEmailTokens_ActivePurpose
        ON UserEmailTokens (UserId, Purpose)
        WHERE UsedAtUtc IS NULL AND RevokedAtUtc IS NULL;
    """;

    public const string CreateUserEmailTokensUserPurposeExpiryIndex = """
    CREATE INDEX IF NOT EXISTS IX_UserEmailTokens_UserPurposeExpiry
        ON UserEmailTokens (UserId, Purpose, ExpiresAtUtc);
    """;

    public const string CreateUserEmailTokensSchema =
        CreateUserEmailTokensTable + "\n" +
        CreateUserEmailTokensTokenHashIndex + "\n" +
        CreateUserEmailTokensActivePurposeIndex + "\n" +
        CreateUserEmailTokensUserPurposeExpiryIndex;

    public const string AddUserIdToRecipes = """
    ALTER TABLE Recipes
        ADD COLUMN UserId INTEGER NULL REFERENCES Users(Id);
    """;

    public const string MakeRecipeSourceUrlOptional = """
    ALTER TABLE Recipes RENAME COLUMN SourceUrl TO SourceUrlRequired;
    ALTER TABLE Recipes ADD COLUMN SourceUrl TEXT NULL;
    UPDATE Recipes SET SourceUrl = SourceUrlRequired;
    ALTER TABLE Recipes DROP COLUMN SourceUrlRequired;
    """;
}

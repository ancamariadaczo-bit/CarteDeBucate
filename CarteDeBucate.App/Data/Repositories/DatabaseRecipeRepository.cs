using Microsoft.Data.Sqlite;

public class DatabaseRecipeRepository : IRecipeRepository
{
    private readonly string _connectionString;

    public DatabaseRecipeRepository(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }

    public List<Recipe> GetAllRecipes()
    {
        List<Recipe> recipes = new List<Recipe>();

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, SourceUrl, SavedAt, Notes
            FROM Recipes
            ORDER BY SavedAt DESC;
            """;

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            Recipe recipe = ReadRecipeFromReader(reader);

            recipes.Add(recipe);
        }

        foreach (Recipe recipe in recipes)
        {
            recipe.Ingredients = GetIngredientsForRecipe(connection, recipe.Id);
            recipe.Steps = GetStepsForRecipe(connection, recipe.Id);
        }

        return recipes;
    }

    public Recipe? GetRecipeById(int recipeId)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        Recipe? recipe = GetRecipeMainRecordById(connection, recipeId);

        if (recipe == null)
        {
            return null;
        }

        recipe.Ingredients = GetIngredientsForRecipe(connection, recipe.Id);
        recipe.Steps = GetStepsForRecipe(connection, recipe.Id);

        return recipe;
    }

    public void AddRecipe(Recipe recipe)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            int recipeId = InsertRecipe(connection, transaction, recipe);

            InsertIngredients(connection, transaction, recipeId, recipe.Ingredients);
            InsertSteps(connection, transaction, recipeId, recipe.Steps);

            transaction.Commit();

            recipe.Id = recipeId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void UpdateRecipe(Recipe recipe)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            UpdateRecipeMainRecord(connection, transaction, recipe);

            DeleteRecipeIngredients(connection, transaction, recipe.Id);
            InsertIngredients(connection, transaction, recipe.Id, recipe.Ingredients);

            DeleteRecipeSteps(connection, transaction, recipe.Id);
            InsertSteps(connection, transaction, recipe.Id, recipe.Steps);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
    public bool RecipeExistsBySourceUrl(string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            return false;
        }

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM Recipes
            WHERE LOWER(TRIM(SourceUrl)) = LOWER(TRIM(@SourceUrl));
            """;

        command.Parameters.AddWithValue("@SourceUrl", sourceUrl);

        long count = (long)(command.ExecuteScalar() ?? 0);

        return count > 0;
    }

    private int InsertRecipe(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Recipe recipe)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = """
            INSERT INTO Recipes (Name, SourceUrl, SavedAt, Notes)
            VALUES (@Name, @SourceUrl, @SavedAt, @Notes);

            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("@Name", recipe.Name);
        command.Parameters.AddWithValue("@SourceUrl", recipe.SourceUrl);
        command.Parameters.AddWithValue("@SavedAt", recipe.SavedAt.ToString("O"));
        command.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(recipe.Notes) ? DBNull.Value : recipe.Notes);

        long recipeId = (long)(command.ExecuteScalar() ?? 0);

        return (int)recipeId;
    }

    private void InsertIngredients(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int recipeId,
        List<string> ingredients)
    {
        for (int i = 0; i < ingredients.Count; i++)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = """
                INSERT INTO RecipeIngredients (RecipeId, IngredientOrder, IngredientText)
                VALUES (@RecipeId, @IngredientOrder, @IngredientText);
                """;

            command.Parameters.AddWithValue("@RecipeId", recipeId);
            command.Parameters.AddWithValue("@IngredientOrder", i + 1);
            command.Parameters.AddWithValue("@IngredientText", ingredients[i]);

            command.ExecuteNonQuery();
        }
    }

    private void InsertSteps(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int recipeId,
        List<string> steps)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = """
                INSERT INTO RecipeSteps (RecipeId, StepOrder, StepText)
                VALUES (@RecipeId, @StepOrder, @StepText);
                """;

            command.Parameters.AddWithValue("@RecipeId", recipeId);
            command.Parameters.AddWithValue("@StepOrder", i + 1);
            command.Parameters.AddWithValue("@StepText", steps[i]);

            command.ExecuteNonQuery();
        }
    }

    private List<string> GetIngredientsForRecipe(SqliteConnection connection, int recipeId)
    {
        List<string> ingredients = new List<string>();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT IngredientText
            FROM RecipeIngredients
            WHERE RecipeId = @RecipeId
            ORDER BY IngredientOrder;
            """;

        command.Parameters.AddWithValue("@RecipeId", recipeId);

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            ingredients.Add(reader.GetString(0));
        }

        return ingredients;
    }

    private List<string> GetStepsForRecipe(SqliteConnection connection, int recipeId)
    {
        List<string> steps = new List<string>();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT StepText
            FROM RecipeSteps
            WHERE RecipeId = @RecipeId
            ORDER BY StepOrder;
            """;

        command.Parameters.AddWithValue("@RecipeId", recipeId);

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            steps.Add(reader.GetString(0));
        }

        return steps;
    }

    public void DeleteRecipe(int recipeId)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            DeleteRecipeIngredients(connection, transaction, recipeId);
            DeleteRecipeSteps(connection, transaction, recipeId);
            DeleteRecipeMainRecord(connection, transaction, recipeId);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private void DeleteRecipeIngredients(
    SqliteConnection connection,
    SqliteTransaction transaction,
    int recipeId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = """
            DELETE FROM RecipeIngredients
            WHERE RecipeId = @RecipeId;
            """;

        command.Parameters.AddWithValue("@RecipeId", recipeId);

        command.ExecuteNonQuery();
    }

    private void DeleteRecipeSteps(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int recipeId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = """
            DELETE FROM RecipeSteps
            WHERE RecipeId = @RecipeId;
            """;

        command.Parameters.AddWithValue("@RecipeId", recipeId);

        command.ExecuteNonQuery();
    }

    private void DeleteRecipeMainRecord(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int recipeId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = """
            DELETE FROM Recipes
            WHERE Id = @RecipeId;
            """;

        command.Parameters.AddWithValue("@RecipeId", recipeId);

        command.ExecuteNonQuery();
    }

    private Recipe? GetRecipeMainRecordById(SqliteConnection connection, int recipeId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT Id, Name, SourceUrl, SavedAt, Notes
            FROM Recipes
            WHERE Id = @RecipeId;
            """;

        command.Parameters.AddWithValue("@RecipeId", recipeId);

        using SqliteDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return ReadRecipeFromReader(reader);
    }

    private Recipe ReadRecipeFromReader(SqliteDataReader reader)
    {
        return new Recipe
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            SourceUrl = reader.GetString(2),
            SavedAt = DateTime.Parse(reader.GetString(3)),
            Notes = reader.IsDBNull(4) ? "" : reader.GetString(4),
            Ingredients = new List<string>(),
            Steps = new List<string>()
        };
    }

    private void UpdateRecipeMainRecord(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Recipe recipe)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = """
            UPDATE Recipes
            SET Name = @Name,
                SourceUrl = @SourceUrl,
                SavedAt = @SavedAt,
                Notes = @Notes
            WHERE Id = @RecipeId;
            """;

        command.Parameters.AddWithValue("@RecipeId", recipe.Id);
        command.Parameters.AddWithValue("@Name", recipe.Name);
        command.Parameters.AddWithValue("@SourceUrl", recipe.SourceUrl);
        command.Parameters.AddWithValue("@SavedAt", recipe.SavedAt.ToString("O"));
        command.Parameters.AddWithValue(
            "@Notes",
            string.IsNullOrWhiteSpace(recipe.Notes) ? DBNull.Value : recipe.Notes);

        command.ExecuteNonQuery();
    }
}
using Microsoft.Data.Sqlite;

public class DatabaseRecipeRepository : IRecipeRepository
{
    private readonly string _connectionString;

    public DatabaseRecipeRepository(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }

    public List<RecipeSummary> GetAllRecipeSummaries()
    {
        List<RecipeSummary> recipes = new List<RecipeSummary>();

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, SourceUrl, SavedAt, Status, UserId
            FROM Recipes
            ORDER BY SavedAt DESC;
            """;

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            RecipeSummary recipe = ReadRecipeSummaryFromReader(reader);

            recipes.Add(recipe);
        }

        return recipes;
    }

    public List<RecipeSummary> GetRecipeSummariesByUserId(int userId)
    {
        List<RecipeSummary> recipes = new List<RecipeSummary>();

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, SourceUrl, SavedAt, Status, UserId
            FROM Recipes
            WHERE UserId = @UserId
            ORDER BY SavedAt DESC;
            """;

        command.Parameters.AddWithValue("@UserId", userId);

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            RecipeSummary recipe = ReadRecipeSummaryFromReader(reader);

            recipes.Add(recipe);
        }

        return recipes;
    }

    public PagedResult<RecipeSummary> GetRecipeSummariesPage(int pageNumber, int pageSize)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        int totalItems = CountRecipes(connection);
        List<RecipeSummary> recipes = GetRecipeSummariesPage(
            connection,
            pageNumber,
            pageSize);

        return new PagedResult<RecipeSummary>(
            recipes,
            pageNumber,
            pageSize,
            totalItems);
    }

    public PagedResult<RecipeSummary> GetRecipeSummariesPageByUserId(
        int userId,
        int pageNumber,
        int pageSize)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        int totalItems = CountRecipesByUserId(connection, userId);
        List<RecipeSummary> recipes = GetRecipeSummariesPageByUserId(
            connection,
            userId,
            pageNumber,
            pageSize);

        return new PagedResult<RecipeSummary>(
            recipes,
            pageNumber,
            pageSize,
            totalItems);
    }

    public List<Recipe> GetAllRecipes()
    {
        List<Recipe> recipes = new List<Recipe>();

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, SourceUrl, SavedAt, Notes, Status, UserId
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

    public List<Recipe> GetRecipesByUserId(int userId)
    {
        List<Recipe> recipes = new List<Recipe>();

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, SourceUrl, SavedAt, Notes, Status, UserId
            FROM Recipes
            WHERE UserId = @UserId
            ORDER BY SavedAt DESC;
            """;

        command.Parameters.AddWithValue("@UserId", userId);

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

    public List<RecipeSummary> SearchRecipes(string searchText, int? userId)
    {
        List<RecipeSummary> recipes = new List<RecipeSummary>();

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT DISTINCT r.Id, r.Name, r.SourceUrl, r.SavedAt, r.Status, r.UserId
            FROM Recipes r
            LEFT JOIN RecipeIngredients i ON i.RecipeId = r.Id
            LEFT JOIN RecipeSteps s ON s.RecipeId = r.Id
            WHERE (
                LOWER(r.Name) LIKE @SearchText
                OR LOWER(r.SourceUrl) LIKE @SearchText
                OR LOWER(COALESCE(r.Notes, '')) LIKE @SearchText
                OR LOWER(COALESCE(i.IngredientText, '')) LIKE @SearchText
                OR LOWER(COALESCE(s.StepText, '')) LIKE @SearchText
            )
            AND (@UserId IS NULL OR r.UserId = @UserId)
            ORDER BY r.SavedAt DESC;
            """;

        command.Parameters.AddWithValue("@SearchText", $"%{searchText.Trim().ToLowerInvariant()}%");
        command.Parameters.AddWithValue("@UserId", userId.HasValue ? userId.Value : (object)DBNull.Value);

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            RecipeSummary recipe = ReadRecipeSummaryFromReader(reader);

            recipes.Add(recipe);
        }

        return recipes;
    }

    public bool HasRecipes()
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM Recipes
            );
            """;

        long exists = (long)(command.ExecuteScalar() ?? 0);

        return exists == 1;
    }

    public bool HasRecipesForUser(int userId)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM Recipes
                WHERE UserId = @UserId
            );
            """;

        command.Parameters.AddWithValue("@UserId", userId);

        long exists = (long)(command.ExecuteScalar() ?? 0);

        return exists == 1;
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

    public Recipe? GetRecipeByIdAndUserId(int recipeId, int userId)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        Recipe? recipe = GetRecipeMainRecordByIdAndUserId(connection, recipeId, userId);

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

    public void UpdateRecipeForUser(Recipe recipe, int userId)
    {
        if (GetRecipeByIdAndUserId(recipe.Id, userId) == null)
        {
            return;
        }

        recipe.UserId = userId;

        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            UpdateRecipeMainRecordForUser(connection, transaction, recipe, userId);

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

    public bool RecipeExistsBySourceUrlForUser(string sourceUrl, int userId)
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
            WHERE LOWER(TRIM(SourceUrl)) = LOWER(TRIM(@SourceUrl))
            AND UserId = @UserId;
            """;

        command.Parameters.AddWithValue("@SourceUrl", sourceUrl);
        command.Parameters.AddWithValue("@UserId", userId);

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
            INSERT INTO Recipes (Name, SourceUrl, SavedAt, Notes, Status, UserId)
            VALUES (@Name, @SourceUrl, @SavedAt, @Notes, @Status, @UserId);

            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("@Name", recipe.Name);
        command.Parameters.AddWithValue("@SourceUrl", recipe.SourceUrl);
        command.Parameters.AddWithValue("@SavedAt", recipe.SavedAt.ToString("O"));
        command.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(recipe.Notes) ? DBNull.Value : recipe.Notes);
        command.Parameters.AddWithValue("@Status", (int)recipe.Status);
        command.Parameters.AddWithValue("@UserId", recipe.UserId.HasValue ? recipe.UserId.Value : (object)DBNull.Value);

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

    public void DeleteRecipeForUser(int recipeId, int userId)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        if (!RecipeBelongsToUser(connection, recipeId, userId))
        {
            return;
        }

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

    private bool RecipeBelongsToUser(
        SqliteConnection connection,
        int recipeId,
        int userId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM Recipes
                WHERE Id = @RecipeId
                    AND UserId = @UserId
            );
            """;

        command.Parameters.AddWithValue("@RecipeId", recipeId);
        command.Parameters.AddWithValue("@UserId", userId);

        long exists = (long)(command.ExecuteScalar() ?? 0);

        return exists == 1;
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
            SELECT Id, Name, SourceUrl, SavedAt, Notes, Status, UserId
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

    private Recipe? GetRecipeMainRecordByIdAndUserId(SqliteConnection connection, int recipeId, int userId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT Id, Name, SourceUrl, SavedAt, Notes, Status, UserId
            FROM Recipes
            WHERE Id = @RecipeId
            AND UserId = @UserId;
            """;

        command.Parameters.AddWithValue("@RecipeId", recipeId);
        command.Parameters.AddWithValue("@UserId", userId);

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
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            SourceUrl = reader.GetString(reader.GetOrdinal("SourceUrl")),
            SavedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("SavedAt"))),
            Notes = reader.IsDBNull(reader.GetOrdinal("Notes"))
                ? ""
                : reader.GetString(reader.GetOrdinal("Notes")),
            Status = (RecipeStatus)reader.GetInt32(reader.GetOrdinal("Status")),
            Ingredients = new List<string>(),
            Steps = new List<string>(),
            UserId = reader.IsDBNull(reader.GetOrdinal("UserId"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("UserId"))
        };
    }

    private RecipeSummary ReadRecipeSummaryFromReader(SqliteDataReader reader)
    {
        return new RecipeSummary
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            SourceUrl = reader.GetString(reader.GetOrdinal("SourceUrl")),
            SavedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("SavedAt"))),
            Status = (RecipeStatus)reader.GetInt32(reader.GetOrdinal("Status")),
            UserId = reader.IsDBNull(reader.GetOrdinal("UserId"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("UserId"))
        };
    }

    private int CountRecipes(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM Recipes;
            """;

        long count = (long)(command.ExecuteScalar() ?? 0);

        return (int)count;
    }

    private int CountRecipesByUserId(SqliteConnection connection, int userId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM Recipes
            WHERE UserId = @UserId;
            """;

        command.Parameters.AddWithValue("@UserId", userId);

        long count = (long)(command.ExecuteScalar() ?? 0);

        return (int)count;
    }

    private List<RecipeSummary> GetRecipeSummariesPage(
        SqliteConnection connection,
        int pageNumber,
        int pageSize)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, SourceUrl, SavedAt, Status, UserId
            FROM Recipes
            ORDER BY SavedAt DESC
            LIMIT @PageSize OFFSET @Offset;
            """;

        AddPaginationParameters(command, pageNumber, pageSize);

        return ReadRecipeSummaries(command);
    }

    private List<RecipeSummary> GetRecipeSummariesPageByUserId(
        SqliteConnection connection,
        int userId,
        int pageNumber,
        int pageSize)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, SourceUrl, SavedAt, Status, UserId
            FROM Recipes
            WHERE UserId = @UserId
            ORDER BY SavedAt DESC
            LIMIT @PageSize OFFSET @Offset;
            """;

        command.Parameters.AddWithValue("@UserId", userId);
        AddPaginationParameters(command, pageNumber, pageSize);

        return ReadRecipeSummaries(command);
    }

    private void AddPaginationParameters(
        SqliteCommand command,
        int pageNumber,
        int pageSize)
    {
        int normalizedPageNumber = Math.Max(1, pageNumber);
        int normalizedPageSize = Math.Max(1, pageSize);
        int offset = (normalizedPageNumber - 1) * normalizedPageSize;

        command.Parameters.AddWithValue("@PageSize", normalizedPageSize);
        command.Parameters.AddWithValue("@Offset", offset);
    }

    private List<RecipeSummary> ReadRecipeSummaries(SqliteCommand command)
    {
        List<RecipeSummary> recipes = new List<RecipeSummary>();

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            RecipeSummary recipe = ReadRecipeSummaryFromReader(reader);

            recipes.Add(recipe);
        }

        return recipes;
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
                Notes = @Notes,
                Status = @Status,
                UserId = @UserId
            WHERE Id = @RecipeId;
            """;

        command.Parameters.AddWithValue("@RecipeId", recipe.Id);
        command.Parameters.AddWithValue("@Name", recipe.Name);
        command.Parameters.AddWithValue("@SourceUrl", recipe.SourceUrl);
        command.Parameters.AddWithValue("@SavedAt", recipe.SavedAt.ToString("O"));
        command.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(recipe.Notes) ? DBNull.Value : recipe.Notes);
        command.Parameters.AddWithValue("@Status", (int)recipe.Status);
        command.Parameters.AddWithValue("@UserId", recipe.UserId.HasValue ? recipe.UserId.Value : (object)DBNull.Value);

        command.ExecuteNonQuery();
    }

    private void UpdateRecipeMainRecordForUser(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Recipe recipe,
        int userId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = """
            UPDATE Recipes
            SET Name = @Name,
                SourceUrl = @SourceUrl,
                SavedAt = @SavedAt,
                Notes = @Notes,
                Status = @Status,
                UserId = @UserId
            WHERE Id = @RecipeId
            AND UserId = @UserId;
            """;

        command.Parameters.AddWithValue("@RecipeId", recipe.Id);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Name", recipe.Name);
        command.Parameters.AddWithValue("@SourceUrl", recipe.SourceUrl);
        command.Parameters.AddWithValue("@SavedAt", recipe.SavedAt.ToString("O"));
        command.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(recipe.Notes) ? DBNull.Value : recipe.Notes);
        command.Parameters.AddWithValue("@Status", (int)recipe.Status);

        command.ExecuteNonQuery();
    }
}

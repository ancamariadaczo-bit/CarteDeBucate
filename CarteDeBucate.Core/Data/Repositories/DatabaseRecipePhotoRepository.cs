using System.Globalization;
using Microsoft.Data.Sqlite;

public class DatabaseRecipePhotoRepository : IRecipePhotoRepository
{
    private readonly string _connectionString;

    public DatabaseRecipePhotoRepository(string databasePath)
    {
        _connectionString = SqliteConnectionStringFactory.Create(databasePath);
    }

    public void Add(RecipePhoto photo)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = CreateAddCommand(connection, photo);

        long photoId = (long)(command.ExecuteScalar() ?? 0);
        photo.Id = (int)photoId;
    }

    public bool AddForUser(RecipePhoto photo, int userId)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = CreateAddForUserCommand(connection, photo, userId);

        object? result = command.ExecuteScalar();

        if (result == null)
        {
            return false;
        }

        photo.Id = Convert.ToInt32(result);

        return true;
    }

    public RecipePhoto? GetById(int photoId)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                Id,
                RecipeId,
                StorageFileName,
                OriginalFileName,
                ContentType,
                FileSize,
                CreatedAtUtc,
                DisplayOrder,
                Origin
            FROM RecipePhotos
            WHERE Id = @PhotoId;
            """;
        command.Parameters.AddWithValue("@PhotoId", photoId);

        using SqliteDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return ReadPhoto(reader);
    }

    public RecipePhoto? GetByIdAndUserId(int photoId, int userId)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                photo.Id,
                photo.RecipeId,
                photo.StorageFileName,
                photo.OriginalFileName,
                photo.ContentType,
                photo.FileSize,
                photo.CreatedAtUtc,
                photo.DisplayOrder,
                photo.Origin
            FROM RecipePhotos AS photo
            INNER JOIN Recipes AS recipe
                ON recipe.Id = photo.RecipeId
            WHERE photo.Id = @PhotoId
                AND recipe.UserId = @UserId;
            """;
        command.Parameters.AddWithValue("@PhotoId", photoId);
        command.Parameters.AddWithValue("@UserId", userId);

        using SqliteDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return ReadPhoto(reader);
    }

    public List<RecipePhoto> GetByRecipeId(int recipeId)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                Id,
                RecipeId,
                StorageFileName,
                OriginalFileName,
                ContentType,
                FileSize,
                CreatedAtUtc,
                DisplayOrder,
                Origin
            FROM RecipePhotos
            WHERE RecipeId = @RecipeId
            ORDER BY DisplayOrder, Id;
            """;
        command.Parameters.AddWithValue("@RecipeId", recipeId);

        return ReadPhotos(command);
    }

    public List<RecipePhoto> GetByRecipeIdAndUserId(int recipeId, int userId)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                photo.Id,
                photo.RecipeId,
                photo.StorageFileName,
                photo.OriginalFileName,
                photo.ContentType,
                photo.FileSize,
                photo.CreatedAtUtc,
                photo.DisplayOrder,
                photo.Origin
            FROM RecipePhotos AS photo
            INNER JOIN Recipes AS recipe
                ON recipe.Id = photo.RecipeId
            WHERE photo.RecipeId = @RecipeId
                AND recipe.UserId = @UserId
            ORDER BY photo.DisplayOrder, photo.Id;
            """;
        command.Parameters.AddWithValue("@RecipeId", recipeId);
        command.Parameters.AddWithValue("@UserId", userId);

        return ReadPhotos(command);
    }

    public bool Delete(int photoId)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM RecipePhotos
            WHERE Id = @PhotoId;
            """;
        command.Parameters.AddWithValue("@PhotoId", photoId);

        return command.ExecuteNonQuery() == 1;
    }

    public bool DeleteForUser(int photoId, int userId)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM RecipePhotos
            WHERE Id = @PhotoId
                AND EXISTS (
                    SELECT 1
                    FROM Recipes AS recipe
                    WHERE recipe.Id = RecipePhotos.RecipeId
                        AND recipe.UserId = @UserId
                );
            """;
        command.Parameters.AddWithValue("@PhotoId", photoId);
        command.Parameters.AddWithValue("@UserId", userId);

        return command.ExecuteNonQuery() == 1;
    }

    private static SqliteCommand CreateAddCommand(
        SqliteConnection connection,
        RecipePhoto photo)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO RecipePhotos (
                RecipeId,
                StorageFileName,
                OriginalFileName,
                ContentType,
                FileSize,
                CreatedAtUtc,
                DisplayOrder,
                Origin)
            VALUES (
                @RecipeId,
                @StorageFileName,
                @OriginalFileName,
                @ContentType,
                @FileSize,
                @CreatedAtUtc,
                @DisplayOrder,
                @Origin);

            SELECT last_insert_rowid();
            """;

        AddPhotoParameters(command, photo);

        return command;
    }

    private static SqliteCommand CreateAddForUserCommand(
        SqliteConnection connection,
        RecipePhoto photo,
        int userId)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO RecipePhotos (
                RecipeId,
                StorageFileName,
                OriginalFileName,
                ContentType,
                FileSize,
                CreatedAtUtc,
                DisplayOrder,
                Origin)
            SELECT
                @RecipeId,
                @StorageFileName,
                @OriginalFileName,
                @ContentType,
                @FileSize,
                @CreatedAtUtc,
                @DisplayOrder,
                @Origin
            FROM Recipes
            WHERE Id = @RecipeId
                AND UserId = @UserId
            RETURNING Id;
            """;

        AddPhotoParameters(command, photo);
        command.Parameters.AddWithValue("@UserId", userId);

        return command;
    }

    private static void AddPhotoParameters(SqliteCommand command, RecipePhoto photo)
    {
        command.Parameters.AddWithValue("@RecipeId", photo.RecipeId);
        command.Parameters.AddWithValue("@StorageFileName", photo.StorageFileName);
        command.Parameters.AddWithValue("@OriginalFileName", photo.OriginalFileName);
        command.Parameters.AddWithValue("@ContentType", photo.ContentType);
        command.Parameters.AddWithValue("@FileSize", photo.FileSize);
        command.Parameters.AddWithValue("@CreatedAtUtc", photo.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@DisplayOrder", photo.DisplayOrder);
        command.Parameters.AddWithValue("@Origin", (int)photo.Origin);
    }

    private static RecipePhoto ReadPhoto(SqliteDataReader reader)
    {
        return new RecipePhoto
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            RecipeId = reader.GetInt32(reader.GetOrdinal("RecipeId")),
            StorageFileName = reader.GetString(reader.GetOrdinal("StorageFileName")),
            OriginalFileName = reader.GetString(reader.GetOrdinal("OriginalFileName")),
            ContentType = reader.GetString(reader.GetOrdinal("ContentType")),
            FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
            CreatedAtUtc = DateTime.Parse(
                reader.GetString(reader.GetOrdinal("CreatedAtUtc")),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind),
            DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
            Origin = (RecipePhotoOrigin)reader.GetInt32(reader.GetOrdinal("Origin"))
        };
    }

    private static List<RecipePhoto> ReadPhotos(SqliteCommand command)
    {
        using SqliteDataReader reader = command.ExecuteReader();
        List<RecipePhoto> photos = new List<RecipePhoto>();

        while (reader.Read())
        {
            photos.Add(ReadPhoto(reader));
        }

        return photos;
    }
}

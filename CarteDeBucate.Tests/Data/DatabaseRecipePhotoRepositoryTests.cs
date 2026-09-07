using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

public class DatabaseRecipePhotoRepositoryTests
{
    [Fact]
    public void Add_WhenPhotoIsValid_ShouldInsertAllFieldsAndSetGeneratedId()
    {
        WithInitializedDatabase(databasePath =>
        {
            int recipeId = InsertRecipe(databasePath);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto photo = CreatePhoto(recipeId, "stored-photo.jpg");

            repository.Add(photo);

            Assert.True(photo.Id > 0);

            StoredRecipePhoto storedPhoto = ReadStoredPhoto(databasePath, photo.Id);

            Assert.Equal(photo.Id, storedPhoto.Id);
            Assert.Equal(photo.RecipeId, storedPhoto.RecipeId);
            Assert.Equal(photo.StorageFileName, storedPhoto.StorageFileName);
            Assert.Equal(photo.OriginalFileName, storedPhoto.OriginalFileName);
            Assert.Equal(photo.ContentType, storedPhoto.ContentType);
            Assert.Equal(photo.FileSize, storedPhoto.FileSize);
            Assert.Equal(photo.CreatedAtUtc.ToString("O"), storedPhoto.CreatedAtUtc);
            Assert.Equal(photo.DisplayOrder, storedPhoto.DisplayOrder);
            Assert.Equal((int)photo.Origin, storedPhoto.Origin);

            DateTime parsedCreatedAtUtc = DateTime.Parse(
                storedPhoto.CreatedAtUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);

            Assert.Equal(DateTimeKind.Utc, parsedCreatedAtUtc.Kind);
            Assert.Equal(photo.CreatedAtUtc, parsedCreatedAtUtc);
        });
    }

    [Fact]
    public void Add_WhenRecipeDoesNotExist_ShouldPropagateSqliteError()
    {
        WithInitializedDatabase(databasePath =>
        {
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto photo = CreatePhoto(999, "missing-recipe.jpg");

            SqliteException exception = Assert.Throws<SqliteException>(() =>
                repository.Add(photo));

            Assert.Equal(19, exception.SqliteErrorCode);
            Assert.Equal(0, photo.Id);
        });
    }

    [Fact]
    public void Add_WhenStorageFileNameAlreadyExists_ShouldPropagateSqliteError()
    {
        WithInitializedDatabase(databasePath =>
        {
            int recipeId = InsertRecipe(databasePath);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto firstPhoto = CreatePhoto(recipeId, "duplicate-photo.jpg");
            RecipePhoto secondPhoto = CreatePhoto(recipeId, "duplicate-photo.jpg");

            repository.Add(firstPhoto);

            SqliteException exception = Assert.Throws<SqliteException>(() =>
                repository.Add(secondPhoto));

            Assert.Equal(19, exception.SqliteErrorCode);
            Assert.Equal(0, secondPhoto.Id);
        });
    }

    [Fact]
    public void AddForUser_WhenRecipeBelongsToUser_ShouldInsertPhotoAndSetGeneratedId()
    {
        WithInitializedDatabase(databasePath =>
        {
            int userId = InsertUser(databasePath, "photo-owner");
            int recipeId = InsertRecipe(databasePath, userId);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto photo = CreatePhoto(recipeId, "owner-photo.jpg");

            bool wasAdded = repository.AddForUser(photo, userId);

            Assert.True(wasAdded);
            Assert.True(photo.Id > 0);

            StoredRecipePhoto storedPhoto = ReadStoredPhoto(databasePath, photo.Id);
            Assert.Equal(photo.Id, storedPhoto.Id);
            Assert.Equal(recipeId, storedPhoto.RecipeId);
            Assert.Equal("owner-photo.jpg", storedPhoto.StorageFileName);
        });
    }

    [Fact]
    public void AddForUser_WhenRecipeBelongsToAnotherUser_ShouldReturnFalseAndNotInsertPhoto()
    {
        WithInitializedDatabase(databasePath =>
        {
            int ownerId = InsertUser(databasePath, "photo-owner");
            int otherUserId = InsertUser(databasePath, "other-user");
            int recipeId = InsertRecipe(databasePath, ownerId);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto photo = CreatePhoto(recipeId, "other-user-photo.jpg");

            bool wasAdded = repository.AddForUser(photo, otherUserId);

            Assert.False(wasAdded);
            Assert.Equal(0, photo.Id);
            Assert.Equal(0L, CountRecipePhotos(databasePath));
        });
    }

    [Fact]
    public void AddForUser_WhenRecipeDoesNotExist_ShouldReturnFalseAndNotInsertPhoto()
    {
        WithInitializedDatabase(databasePath =>
        {
            int userId = InsertUser(databasePath, "photo-owner");
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto photo = CreatePhoto(999, "missing-recipe-for-user.jpg");

            bool wasAdded = repository.AddForUser(photo, userId);

            Assert.False(wasAdded);
            Assert.Equal(0, photo.Id);
            Assert.Equal(0L, CountRecipePhotos(databasePath));
        });
    }

    [Fact]
    public void GetById_WhenPhotoExists_ShouldReturnPhoto()
    {
        WithInitializedDatabase(databasePath =>
        {
            int recipeId = InsertRecipe(databasePath);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto expectedPhoto = CreatePhoto(recipeId, "get-by-id.jpg");
            repository.Add(expectedPhoto);

            RecipePhoto? photo = repository.GetById(expectedPhoto.Id);

            Assert.NotNull(photo);
            Assert.Equal(expectedPhoto.Id, photo.Id);
            Assert.Equal(expectedPhoto.RecipeId, photo.RecipeId);
            Assert.Equal(expectedPhoto.StorageFileName, photo.StorageFileName);
            Assert.Equal(expectedPhoto.OriginalFileName, photo.OriginalFileName);
            Assert.Equal(expectedPhoto.ContentType, photo.ContentType);
            Assert.Equal(expectedPhoto.FileSize, photo.FileSize);
            Assert.Equal(expectedPhoto.CreatedAtUtc, photo.CreatedAtUtc);
            Assert.Equal(DateTimeKind.Utc, photo.CreatedAtUtc.Kind);
            Assert.Equal(expectedPhoto.DisplayOrder, photo.DisplayOrder);
            Assert.Equal(RecipePhotoOrigin.UserUpload, photo.Origin);
        });
    }

    [Fact]
    public void GetById_WhenPhotoDoesNotExist_ShouldReturnNull()
    {
        WithInitializedDatabase(databasePath =>
        {
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);

            RecipePhoto? photo = repository.GetById(999);

            Assert.Null(photo);
        });
    }

    [Fact]
    public void GetByIdAndUserId_WhenPhotoBelongsToUser_ShouldReturnPhoto()
    {
        WithInitializedDatabase(databasePath =>
        {
            int userId = InsertUser(databasePath, "photo-owner");
            int recipeId = InsertRecipe(databasePath, userId);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto expectedPhoto = CreatePhoto(recipeId, "owned-photo.jpg");
            repository.Add(expectedPhoto);

            RecipePhoto? photo = repository.GetByIdAndUserId(expectedPhoto.Id, userId);

            Assert.NotNull(photo);
            Assert.Equal(expectedPhoto.Id, photo.Id);
            Assert.Equal(recipeId, photo.RecipeId);
        });
    }

    [Fact]
    public void GetByIdAndUserId_WhenPhotoBelongsToAnotherUser_ShouldReturnNull()
    {
        WithInitializedDatabase(databasePath =>
        {
            int ownerId = InsertUser(databasePath, "photo-owner");
            int otherUserId = InsertUser(databasePath, "other-user");
            int recipeId = InsertRecipe(databasePath, ownerId);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto photo = CreatePhoto(recipeId, "other-owner-photo.jpg");
            repository.Add(photo);

            RecipePhoto? result = repository.GetByIdAndUserId(photo.Id, otherUserId);

            Assert.Null(result);
        });
    }

    [Fact]
    public void GetByIdAndUserId_WhenRecipeHasNoUser_ShouldReturnNull()
    {
        WithInitializedDatabase(databasePath =>
        {
            int userId = InsertUser(databasePath, "authenticated-user");
            int recipeId = InsertRecipe(databasePath);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto photo = CreatePhoto(recipeId, "unowned-recipe-photo.jpg");
            repository.Add(photo);

            RecipePhoto? result = repository.GetByIdAndUserId(photo.Id, userId);

            Assert.Null(result);
        });
    }

    [Fact]
    public void GetByRecipeId_WhenRecipeHasNoPhotos_ShouldReturnEmptyList()
    {
        WithInitializedDatabase(databasePath =>
        {
            int recipeId = InsertRecipe(databasePath);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);

            List<RecipePhoto> photos = repository.GetByRecipeId(recipeId);

            Assert.Empty(photos);
        });
    }

    [Fact]
    public void GetByRecipeId_ShouldReturnOnlyPhotosForRequestedRecipe()
    {
        WithInitializedDatabase(databasePath =>
        {
            int requestedRecipeId = InsertRecipe(databasePath);
            int otherRecipeId = InsertRecipe(databasePath);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto requestedPhoto = CreatePhoto(requestedRecipeId, "requested-recipe.jpg");
            RecipePhoto otherPhoto = CreatePhoto(otherRecipeId, "other-recipe.jpg");
            repository.Add(requestedPhoto);
            repository.Add(otherPhoto);

            List<RecipePhoto> photos = repository.GetByRecipeId(requestedRecipeId);

            RecipePhoto photo = Assert.Single(photos);
            Assert.Equal(requestedPhoto.Id, photo.Id);
            Assert.Equal(requestedRecipeId, photo.RecipeId);
        });
    }

    [Fact]
    public void GetByRecipeId_ShouldOrderByDisplayOrderThenId()
    {
        WithInitializedDatabase(databasePath =>
        {
            int recipeId = InsertRecipe(databasePath);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);

            RecipePhoto firstPhoto = CreatePhoto(recipeId, "first.jpg");
            firstPhoto.DisplayOrder = 1;
            repository.Add(firstPhoto);

            RecipePhoto thirdPhoto = CreatePhoto(recipeId, "third.jpg");
            thirdPhoto.DisplayOrder = 2;
            repository.Add(thirdPhoto);

            RecipePhoto secondPhoto = CreatePhoto(recipeId, "second.jpg");
            secondPhoto.DisplayOrder = 1;
            repository.Add(secondPhoto);

            List<RecipePhoto> photos = repository.GetByRecipeId(recipeId);

            Assert.Equal(
                new[] { firstPhoto.Id, secondPhoto.Id, thirdPhoto.Id },
                photos.Select(photo => photo.Id).ToArray());
        });
    }

    [Fact]
    public void GetByRecipeIdAndUserId_ShouldOrderPhotosAndIsolateUsers()
    {
        WithInitializedDatabase(databasePath =>
        {
            int ownerId = InsertUser(databasePath, "photo-owner");
            int otherUserId = InsertUser(databasePath, "other-user");
            int ownerRecipeId = InsertRecipe(databasePath, ownerId);
            int otherRecipeId = InsertRecipe(databasePath, otherUserId);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);

            RecipePhoto secondOwnerPhoto = CreatePhoto(ownerRecipeId, "owner-second.jpg");
            secondOwnerPhoto.DisplayOrder = 2;
            repository.Add(secondOwnerPhoto);

            RecipePhoto firstOwnerPhoto = CreatePhoto(ownerRecipeId, "owner-first.jpg");
            firstOwnerPhoto.DisplayOrder = 1;
            repository.Add(firstOwnerPhoto);

            RecipePhoto otherPhoto = CreatePhoto(otherRecipeId, "other-user.jpg");
            otherPhoto.DisplayOrder = 0;
            repository.Add(otherPhoto);

            List<RecipePhoto> ownerPhotos =
                repository.GetByRecipeIdAndUserId(ownerRecipeId, ownerId);
            List<RecipePhoto> otherUserResult =
                repository.GetByRecipeIdAndUserId(ownerRecipeId, otherUserId);

            Assert.Equal(
                new[] { firstOwnerPhoto.Id, secondOwnerPhoto.Id },
                ownerPhotos.Select(photo => photo.Id).ToArray());
            Assert.Empty(otherUserResult);
        });
    }

    [Fact]
    public void Delete_WhenPhotoExists_ShouldDeletePhotoAndReturnTrue()
    {
        WithInitializedDatabase(databasePath =>
        {
            int recipeId = InsertRecipe(databasePath);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto photo = CreatePhoto(recipeId, "delete-photo.jpg");
            repository.Add(photo);

            bool wasDeleted = repository.Delete(photo.Id);

            Assert.True(wasDeleted);
            Assert.Null(repository.GetById(photo.Id));
        });
    }

    [Fact]
    public void Delete_WhenPhotoDoesNotExist_ShouldReturnFalse()
    {
        WithInitializedDatabase(databasePath =>
        {
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);

            bool wasDeleted = repository.Delete(999);

            Assert.False(wasDeleted);
        });
    }

    [Fact]
    public void DeleteForUser_WhenPhotoBelongsToUser_ShouldDeletePhotoAndReturnTrue()
    {
        WithInitializedDatabase(databasePath =>
        {
            int userId = InsertUser(databasePath, "photo-owner");
            int recipeId = InsertRecipe(databasePath, userId);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto photo = CreatePhoto(recipeId, "delete-owned-photo.jpg");
            repository.Add(photo);

            bool wasDeleted = repository.DeleteForUser(photo.Id, userId);

            Assert.True(wasDeleted);
            Assert.Null(repository.GetById(photo.Id));
        });
    }

    [Fact]
    public void DeleteForUser_WhenPhotoBelongsToAnotherUser_ShouldReturnFalseAndKeepPhoto()
    {
        WithInitializedDatabase(databasePath =>
        {
            int ownerId = InsertUser(databasePath, "photo-owner");
            int otherUserId = InsertUser(databasePath, "other-user");
            int recipeId = InsertRecipe(databasePath, ownerId);
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto photo = CreatePhoto(recipeId, "keep-other-owner-photo.jpg");
            repository.Add(photo);

            bool wasDeleted = repository.DeleteForUser(photo.Id, otherUserId);

            Assert.False(wasDeleted);
            Assert.NotNull(repository.GetById(photo.Id));
        });
    }

    [Fact]
    public void ForeignKeys_WhenRecipeDoesNotExist_ShouldRejectPhotoMetadata()
    {
        WithInitializedDatabase(databasePath =>
        {
            DatabaseRecipePhotoRepository repository =
                new DatabaseRecipePhotoRepository(databasePath);
            RecipePhoto photo = CreatePhoto(999, "orphan-photo.jpg");

            SqliteException exception = Assert.Throws<SqliteException>(() =>
                repository.Add(photo));

            Assert.Equal(787, exception.SqliteExtendedErrorCode);
            Assert.Equal(0L, CountRecipePhotos(databasePath));
        });
    }

    [Fact]
    public void DeleteRecipe_ShouldCascadePhotoMetadataAndKeepOtherRecipePhotos()
    {
        WithInitializedDatabase(databasePath =>
        {
            DatabaseRecipeRepository recipeRepository =
                new DatabaseRecipeRepository(
                    databasePath,
                    NullLogger<DatabaseRecipeRepository>.Instance);
            DatabaseRecipePhotoRepository photoRepository =
                new DatabaseRecipePhotoRepository(databasePath);

            Recipe deletedRecipe = CreateRecipe(
                "Deleted recipe",
                "https://example.com/deleted-recipe");
            Recipe remainingRecipe = CreateRecipe(
                "Remaining recipe",
                "https://example.com/remaining-recipe");
            recipeRepository.AddRecipe(deletedRecipe);
            recipeRepository.AddRecipe(remainingRecipe);

            RecipePhoto firstDeletedPhoto =
                CreatePhoto(deletedRecipe.Id, "first-deleted-photo.jpg");
            RecipePhoto secondDeletedPhoto =
                CreatePhoto(deletedRecipe.Id, "second-deleted-photo.jpg");
            RecipePhoto remainingPhoto =
                CreatePhoto(remainingRecipe.Id, "remaining-photo.jpg");
            photoRepository.Add(firstDeletedPhoto);
            photoRepository.Add(secondDeletedPhoto);
            photoRepository.Add(remainingPhoto);

            recipeRepository.DeleteRecipe(deletedRecipe.Id);

            Assert.Null(recipeRepository.GetRecipeById(deletedRecipe.Id));
            Assert.Empty(photoRepository.GetByRecipeId(deletedRecipe.Id));

            RecipePhoto persistedRemainingPhoto =
                Assert.Single(photoRepository.GetByRecipeId(remainingRecipe.Id));
            Assert.Equal(remainingPhoto.Id, persistedRemainingPhoto.Id);
        });
    }

    private static RecipePhoto CreatePhoto(int recipeId, string storageFileName)
    {
        return new RecipePhoto
        {
            RecipeId = recipeId,
            StorageFileName = storageFileName,
            OriginalFileName = "Sunday cake.jpg",
            ContentType = "image/jpeg",
            FileSize = 12_345,
            CreatedAtUtc = new DateTime(
                2026,
                9,
                7,
                10,
                11,
                12,
                345,
                DateTimeKind.Utc).AddTicks(6_789),
            DisplayOrder = 3,
            Origin = RecipePhotoOrigin.UserUpload
        };
    }

    private static Recipe CreateRecipe(string name, string sourceUrl)
    {
        return new Recipe
        {
            Name = name,
            SourceUrl = sourceUrl,
            SavedAt = DateTime.UtcNow,
            Notes = "Integration test recipe",
            Ingredients = new List<string>(),
            Steps = new List<string>()
        };
    }

    private static int InsertUser(string databasePath, string username)
    {
        using SqliteConnection connection = OpenConnection(databasePath);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users (Username, PasswordHash, PasswordSalt, CreatedAt)
            VALUES (@Username, @PasswordHash, @PasswordSalt, @CreatedAt);

            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("@Username", username);
        command.Parameters.AddWithValue("@PasswordHash", "hash");
        command.Parameters.AddWithValue("@PasswordSalt", "salt");
        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("O"));

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static int InsertRecipe(string databasePath, int? userId = null)
    {
        using SqliteConnection connection = OpenConnection(databasePath);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Recipes (Name, SourceUrl, SavedAt, Notes, Status, UserId)
            VALUES (@Name, @SourceUrl, @SavedAt, NULL, 0, @UserId);

            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("@Name", "Test recipe");
        command.Parameters.AddWithValue("@SourceUrl", "https://example.com/test-recipe");
        command.Parameters.AddWithValue("@SavedAt", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue(
            "@UserId",
            userId.HasValue ? userId.Value : (object)DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static long CountRecipePhotos(string databasePath)
    {
        using SqliteConnection connection = OpenConnection(databasePath);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM RecipePhotos;";

        return (long)(command.ExecuteScalar() ?? 0);
    }

    private static StoredRecipePhoto ReadStoredPhoto(string databasePath, int photoId)
    {
        using SqliteConnection connection = OpenConnection(databasePath);
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
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", photoId);

        using SqliteDataReader reader = command.ExecuteReader();

        Assert.True(reader.Read());

        return new StoredRecipePhoto(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetInt64(5),
            reader.GetString(6),
            reader.GetInt32(7),
            reader.GetInt32(8));
    }

    private static SqliteConnection OpenConnection(string databasePath)
    {
        SqliteConnection connection =
            new SqliteConnection(SqliteConnectionStringFactory.Create(databasePath));
        connection.Open();

        return connection;
    }

    private static void WithInitializedDatabase(Action<string> test)
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            $"recipe-photos-{Guid.NewGuid()}.db");

        try
        {
            DatabaseInitializer initializer = new DatabaseInitializer(databasePath);
            initializer.Initialize();

            test(databasePath);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private sealed record StoredRecipePhoto(
        int Id,
        int RecipeId,
        string StorageFileName,
        string OriginalFileName,
        string ContentType,
        long FileSize,
        string CreatedAtUtc,
        int DisplayOrder,
        int Origin);
}

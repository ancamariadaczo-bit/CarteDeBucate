using Microsoft.Extensions.Logging.Abstractions;

public sealed class RecipePhotoServiceGetPhotosTests
{
    [Fact]
    public void GetPhotosForRecipe_ShouldPreserveFilteredRepositoryOrder()
    {
        const int userId = 7;
        FakeRecipeRepository recipeRepository =
            CreateRecipeRepository(userId);
        GetPhotosRepository photoRepository = new()
        {
            OwnerUserId = userId,
            Photos =
            [
                CreatePhoto(id: 30, displayOrder: 2),
                CreatePhoto(id: 10, displayOrder: 0),
                CreatePhoto(id: 20, displayOrder: 1)
            ]
        };
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            CreateCurrentUser(userId));

        RecipePhotosResult result = service.GetPhotosForRecipe(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.Success, result.Code);
        Assert.Equal(
            new[] { 30, 10, 20 },
            result.Photos.Select(photo => photo.Id));
        Assert.True(photoRepository.GetByRecipeIdAndUserIdWasCalled);
        Assert.False(photoRepository.GetByRecipeIdWasCalled);
    }

    [Fact]
    public void GetPhotosForRecipe_WhenRecipeHasNoPhotos_ShouldReturnEmptySuccess()
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        GetPhotosRepository photoRepository = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository);

        RecipePhotosResult result = service.GetPhotosForRecipe(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.Success, result.Code);
        Assert.Empty(result.Photos);
        Assert.True(photoRepository.GetByRecipeIdWasCalled);
        Assert.False(photoRepository.GetByRecipeIdAndUserIdWasCalled);
    }

    [Fact]
    public void GetPhotosForRecipe_WhenRecipeDoesNotExist_ShouldReturnNotFound()
    {
        FakeRecipeRepository recipeRepository = new();
        GetPhotosRepository photoRepository = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository);

        RecipePhotosResult result = service.GetPhotosForRecipe(10);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.RecipeNotFound, result.Code);
        Assert.Empty(result.Photos);
        Assert.False(photoRepository.GetByRecipeIdWasCalled);
        Assert.False(photoRepository.GetByRecipeIdAndUserIdWasCalled);
    }

    [Fact]
    public void GetPhotosForRecipe_WithInvalidId_ShouldNotCallRepositories()
    {
        FakeRecipeRepository recipeRepository = CreateRecipeRepository();
        GetPhotosRepository photoRepository = new();
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository);

        RecipePhotosResult result = service.GetPhotosForRecipe(0);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.InvalidRecipeId, result.Code);
        Assert.Empty(result.Photos);
        Assert.False(recipeRepository.GetRecipeByIdWasCalled);
        Assert.False(recipeRepository.GetRecipeByIdAndUserIdWasCalled);
        Assert.False(photoRepository.GetByRecipeIdWasCalled);
        Assert.False(photoRepository.GetByRecipeIdAndUserIdWasCalled);
    }

    [Fact]
    public void GetPhotosForRecipe_WithAnotherUsersRecipe_ShouldReturnNotFound()
    {
        FakeRecipeRepository recipeRepository =
            CreateRecipeRepository(userId: 9);
        GetPhotosRepository photoRepository = new()
        {
            OwnerUserId = 9,
            Photos = [CreatePhoto(id: 30, displayOrder: 0)]
        };
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            CreateCurrentUser(userId: 7));

        RecipePhotosResult result = service.GetPhotosForRecipe(10);

        Assert.False(result.IsSuccess);
        Assert.Equal(RecipePhotoResultCode.RecipeNotFound, result.Code);
        Assert.Empty(result.Photos);
        Assert.True(recipeRepository.GetRecipeByIdAndUserIdWasCalled);
        Assert.False(photoRepository.GetByRecipeIdAndUserIdWasCalled);
    }

    private static RecipePhotoService CreateService(
        FakeRecipeRepository recipeRepository,
        GetPhotosRepository photoRepository,
        ICurrentUserContext? currentUserContext = null)
    {
        return new RecipePhotoService(
            new UnusedPhotoStorage(),
            photoRepository,
            recipeRepository,
            currentUserContext,
            new RecipePhotoValidator(),
            new RecipePhotoFileNameNormalizer(),
            new RecipePhotoOptions(),
            TimeProvider.System,
            NullLogger<RecipePhotoService>.Instance);
    }

    private static FakeRecipeRepository CreateRecipeRepository(
        int? userId = null)
    {
        return new FakeRecipeRepository
        {
            Recipes =
            [
                new Recipe
                {
                    Id = 10,
                    Name = "Recipe 10",
                    UserId = userId
                }
            ]
        };
    }

    private static CurrentUserContext CreateCurrentUser(int userId)
    {
        CurrentUserContext context = new();
        context.SetCurrentUser(new User
        {
            Id = userId,
            Username = $"user-{userId}"
        });
        return context;
    }

    private static RecipePhoto CreatePhoto(int id, int displayOrder)
    {
        return new RecipePhoto
        {
            Id = id,
            RecipeId = 10,
            StorageFileName = $"{id:D32}.jpg",
            OriginalFileName = $"photo-{id}.jpg",
            ContentType = "image/jpeg",
            FileSize = id,
            CreatedAtUtc = new DateTime(
                2026,
                10,
                8,
                10,
                0,
                0,
                DateTimeKind.Utc),
            DisplayOrder = displayOrder,
            Origin = RecipePhotoOrigin.UserUpload
        };
    }

    private sealed class GetPhotosRepository : IRecipePhotoRepository
    {
        public List<RecipePhoto> Photos { get; set; } = [];

        public int? OwnerUserId { get; set; }

        public bool GetByRecipeIdWasCalled { get; private set; }

        public bool GetByRecipeIdAndUserIdWasCalled { get; private set; }

        public void Add(RecipePhoto photo)
        {
            throw new NotImplementedException();
        }

        public bool AddForUser(RecipePhoto photo, int userId)
        {
            throw new NotImplementedException();
        }

        public RecipePhoto? GetById(int photoId)
        {
            throw new NotImplementedException();
        }

        public RecipePhoto? GetByIdAndUserId(int photoId, int userId)
        {
            throw new NotImplementedException();
        }

        public List<RecipePhoto> GetByRecipeId(int recipeId)
        {
            GetByRecipeIdWasCalled = true;
            return Photos
                .Where(photo => photo.RecipeId == recipeId)
                .ToList();
        }

        public List<RecipePhoto> GetByRecipeIdAndUserId(
            int recipeId,
            int userId)
        {
            GetByRecipeIdAndUserIdWasCalled = true;

            return OwnerUserId == userId
                ? GetPhotosWithoutRecordingGlobalCall(recipeId)
                : [];
        }

        public bool Delete(int photoId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteForUser(int photoId, int userId)
        {
            throw new NotImplementedException();
        }

        private List<RecipePhoto> GetPhotosWithoutRecordingGlobalCall(
            int recipeId)
        {
            return Photos
                .Where(photo => photo.RecipeId == recipeId)
                .ToList();
        }
    }

    private sealed class UnusedPhotoStorage : IRecipePhotoStorage
    {
        public Task<StagedRecipePhotoUpload> StageUploadAsync(
            Stream content,
            long maximumBytes,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Stream OpenStagedUpload(StagedRecipePhotoUpload upload)
        {
            throw new NotImplementedException();
        }

        public string FinalizeUpload(
            StagedRecipePhotoUpload upload,
            RecipePhotoFormat format)
        {
            throw new NotImplementedException();
        }

        public void AbandonUpload(StagedRecipePhotoUpload upload)
        {
            throw new NotImplementedException();
        }

        public Stream? OpenFinalFile(string storageFileName)
        {
            throw new NotImplementedException();
        }

        public bool FinalFileExists(string storageFileName)
        {
            throw new NotImplementedException();
        }

        public StagedRecipePhotoDeletion? MoveToQuarantine(
            string storageFileName)
        {
            throw new NotImplementedException();
        }

        public void RestoreFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            throw new NotImplementedException();
        }

        public void DeleteFromQuarantine(
            StagedRecipePhotoDeletion deletion)
        {
            throw new NotImplementedException();
        }

        public RecipePhotoStorageInventory<StagedRecipePhotoUploadInventoryItem>
            GetStagedUploads()
        {
            throw new NotImplementedException();
        }

        public RecipePhotoStorageInventory<StagedRecipePhotoDeletionInventoryItem>
            GetQuarantinedDeletions()
        {
            throw new NotImplementedException();
        }

        public RecipePhotoStorageInventory<RecipePhotoStorageFileInventoryItem>
            GetFinalFiles()
        {
            throw new NotImplementedException();
        }
    }
}

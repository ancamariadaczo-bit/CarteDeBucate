using Microsoft.Extensions.Logging.Abstractions;

public sealed class RecipePhotoServiceSkeletonTests
{
    [Fact]
    public void Helpers_InGlobalContext_ShouldUseUnfilteredRepositories()
    {
        Recipe recipe = CreateRecipe(id: 10, userId: null);
        RecipePhoto photo = CreatePhoto(id: 20, recipeId: recipe.Id);
        FakeRecipeRepository recipeRepository = new()
        {
            Recipes = [recipe]
        };
        SkeletonPhotoRepository photoRepository = new(photo, userId: null);
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository);

        Recipe? resultRecipe = service.GetRecipeInCurrentContext(recipe.Id);
        RecipePhoto? resultPhoto = service.GetPhotoInCurrentContext(photo.Id);

        Assert.Null(service.CurrentUserId);
        Assert.Same(recipe, resultRecipe);
        Assert.Same(photo, resultPhoto);
        Assert.True(recipeRepository.GetRecipeByIdWasCalled);
        Assert.False(recipeRepository.GetRecipeByIdAndUserIdWasCalled);
        Assert.True(photoRepository.GetByIdWasCalled);
        Assert.False(photoRepository.GetByIdAndUserIdWasCalled);
    }

    [Fact]
    public void Helpers_WithAuthenticatedOwner_ShouldUseFilteredRepositories()
    {
        const int userId = 7;
        Recipe recipe = CreateRecipe(id: 10, userId);
        RecipePhoto photo = CreatePhoto(id: 20, recipeId: recipe.Id);
        FakeRecipeRepository recipeRepository = new()
        {
            Recipes = [recipe]
        };
        SkeletonPhotoRepository photoRepository = new(photo, userId);
        CurrentUserContext currentUserContext = CreateCurrentUser(userId);
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            currentUserContext);

        Recipe? resultRecipe = service.GetRecipeInCurrentContext(recipe.Id);
        RecipePhoto? resultPhoto = service.GetPhotoInCurrentContext(photo.Id);

        Assert.Equal(userId, service.CurrentUserId);
        Assert.Same(recipe, resultRecipe);
        Assert.Same(photo, resultPhoto);
        Assert.False(recipeRepository.GetRecipeByIdWasCalled);
        Assert.True(recipeRepository.GetRecipeByIdAndUserIdWasCalled);
        Assert.False(photoRepository.GetByIdWasCalled);
        Assert.True(photoRepository.GetByIdAndUserIdWasCalled);
    }

    [Fact]
    public void Helpers_WithResourcesFromAnotherUser_ShouldReturnNull()
    {
        const int currentUserId = 7;
        const int otherUserId = 9;
        Recipe recipe = CreateRecipe(id: 10, userId: otherUserId);
        RecipePhoto photo = CreatePhoto(id: 20, recipeId: recipe.Id);
        FakeRecipeRepository recipeRepository = new()
        {
            Recipes = [recipe]
        };
        SkeletonPhotoRepository photoRepository = new(photo, otherUserId);
        CurrentUserContext currentUserContext =
            CreateCurrentUser(currentUserId);
        RecipePhotoService service = CreateService(
            recipeRepository,
            photoRepository,
            currentUserContext);

        Recipe? resultRecipe = service.GetRecipeInCurrentContext(recipe.Id);
        RecipePhoto? resultPhoto = service.GetPhotoInCurrentContext(photo.Id);

        Assert.Null(resultRecipe);
        Assert.Null(resultPhoto);
        Assert.True(recipeRepository.GetRecipeByIdAndUserIdWasCalled);
        Assert.True(photoRepository.GetByIdAndUserIdWasCalled);
    }

    [Fact]
    public void ToPhotoInfo_ShouldMapPublicMetadataWithoutStorageFileName()
    {
        RecipePhoto photo = CreatePhoto(id: 20, recipeId: 10);
        photo.StorageFileName =
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg";
        photo.OriginalFileName = "prajitura.jpg";
        photo.ContentType = "image/jpeg";
        photo.FileSize = 1234;
        photo.CreatedAtUtc = new DateTime(
            2026,
            10,
            8,
            9,
            30,
            0,
            DateTimeKind.Utc);
        photo.DisplayOrder = 3;
        photo.Origin = RecipePhotoOrigin.UserUpload;

        RecipePhotoInfo result = RecipePhotoService.ToPhotoInfo(photo);

        Assert.Equal(photo.Id, result.Id);
        Assert.Equal(photo.RecipeId, result.RecipeId);
        Assert.Equal(photo.OriginalFileName, result.OriginalFileName);
        Assert.Equal(photo.ContentType, result.ContentType);
        Assert.Equal(photo.FileSize, result.FileSize);
        Assert.Equal(photo.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(photo.DisplayOrder, result.DisplayOrder);
        Assert.Equal(photo.Origin, result.Origin);
        Assert.Null(typeof(RecipePhotoInfo).GetProperty("StorageFileName"));
    }

    private static RecipePhotoService CreateService(
        FakeRecipeRepository recipeRepository,
        SkeletonPhotoRepository photoRepository,
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

    private static CurrentUserContext CreateCurrentUser(int userId)
    {
        CurrentUserContext currentUserContext = new();
        currentUserContext.SetCurrentUser(new User
        {
            Id = userId,
            Username = $"user-{userId}"
        });

        return currentUserContext;
    }

    private static Recipe CreateRecipe(int id, int? userId)
    {
        return new Recipe
        {
            Id = id,
            Name = $"Recipe {id}",
            UserId = userId
        };
    }

    private static RecipePhoto CreatePhoto(int id, int recipeId)
    {
        return new RecipePhoto
        {
            Id = id,
            RecipeId = recipeId
        };
    }

    private sealed class SkeletonPhotoRepository : IRecipePhotoRepository
    {
        private readonly RecipePhoto _photo;
        private readonly int? _userId;

        public SkeletonPhotoRepository(RecipePhoto photo, int? userId)
        {
            _photo = photo;
            _userId = userId;
        }

        public bool GetByIdWasCalled { get; private set; }

        public bool GetByIdAndUserIdWasCalled { get; private set; }

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
            GetByIdWasCalled = true;
            return _photo.Id == photoId ? _photo : null;
        }

        public RecipePhoto? GetByIdAndUserId(int photoId, int userId)
        {
            GetByIdAndUserIdWasCalled = true;

            return _photo.Id == photoId && _userId == userId
                ? _photo
                : null;
        }

        public List<RecipePhoto> GetByRecipeId(int recipeId)
        {
            throw new NotImplementedException();
        }

        public List<RecipePhoto> GetByRecipeIdAndUserId(
            int recipeId,
            int userId)
        {
            throw new NotImplementedException();
        }

        public bool Delete(int photoId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteForUser(int photoId, int userId)
        {
            throw new NotImplementedException();
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

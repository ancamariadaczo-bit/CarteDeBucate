using System.Reflection;

public sealed class RecipePhotoBackupReadinessContractTests
{
    [Fact]
    public void PhotoService_ShouldExposeMetadataEnumerationAndContentReading()
    {
        MethodInfo? getPhotosMethod = typeof(IRecipePhotoService).GetMethod(
            nameof(IRecipePhotoService.GetPhotosForRecipe));
        MethodInfo? getContentMethod = typeof(IRecipePhotoService).GetMethod(
            nameof(IRecipePhotoService.GetPhotoContent));

        Assert.NotNull(getPhotosMethod);
        Assert.Equal(typeof(RecipePhotosResult), getPhotosMethod.ReturnType);
        Assert.Equal(
            new[] { typeof(int) },
            getPhotosMethod
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray());

        Assert.NotNull(getContentMethod);
        Assert.Equal(
            typeof(RecipePhotoContentResult),
            getContentMethod.ReturnType);
        Assert.Equal(
            new[] { typeof(int) },
            getContentMethod
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray());
        Assert.Equal(
            typeof(Stream),
            typeof(RecipePhotoContentResult)
                .GetProperty(nameof(RecipePhotoContentResult.Content))!
                .PropertyType);
    }

    [Fact]
    public void PhotoInfo_ShouldKeepPortableMetadataSeparateFromRegeneratedMetadata()
    {
        HashSet<string> propertyNames = typeof(RecipePhotoInfo)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        string[] portableMetadata =
        [
            nameof(RecipePhotoInfo.OriginalFileName),
            nameof(RecipePhotoInfo.CreatedAtUtc),
            nameof(RecipePhotoInfo.DisplayOrder),
            nameof(RecipePhotoInfo.Origin)
        ];
        string[] regeneratedMetadata =
        [
            nameof(RecipePhotoInfo.Id),
            nameof(RecipePhotoInfo.RecipeId),
            nameof(RecipePhotoInfo.ContentType),
            nameof(RecipePhotoInfo.FileSize)
        ];

        Assert.All(
            portableMetadata,
            propertyName => Assert.Contains(propertyName, propertyNames));
        Assert.All(
            regeneratedMetadata,
            propertyName => Assert.Contains(propertyName, propertyNames));
        Assert.Empty(portableMetadata.Intersect(regeneratedMetadata));
        Assert.DoesNotContain("StorageFileName", propertyNames);
    }

    [Fact]
    public void PublicPhotoContracts_ShouldNotExposeStorageNamesPathsOrRestoreOperations()
    {
        Type[] publicPhotoContractTypes =
        [
            typeof(IRecipePhotoService),
            typeof(RecipePhotoInfo),
            typeof(RecipePhotosResult),
            typeof(RecipePhotoContentResult)
        ];

        foreach (Type contractType in publicPhotoContractTypes)
        {
            Assert.DoesNotContain(
                contractType.GetProperties(),
                property => ContainsInternalStorageTerm(property.Name));
            Assert.DoesNotContain(
                contractType.GetMethods(),
                method => ContainsInternalStorageTerm(method.Name));
            Assert.DoesNotContain(
                contractType.GetMethods()
                    .SelectMany(method => method.GetParameters()),
                parameter => ContainsInternalStorageTerm(
                    parameter.Name ?? string.Empty));
        }

        Assert.DoesNotContain(
            typeof(IRecipePhotoService).GetMethods(),
            method => method.Name.Contains(
                "Restore",
                StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsInternalStorageTerm(string value)
    {
        return value.Contains(
                "StorageFileName",
                StringComparison.OrdinalIgnoreCase)
            || value.Contains(
                "Path",
                StringComparison.OrdinalIgnoreCase)
            || value.Contains(
                "UploadToken",
                StringComparison.OrdinalIgnoreCase)
            || value.Contains(
                "DeletionToken",
                StringComparison.OrdinalIgnoreCase);
    }
}

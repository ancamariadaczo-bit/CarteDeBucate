
AppSettings appSettings = AppSettings.Load();

IRecipeConsoleReader recipeReader = new RecipeConsoleReader();
IRecipeConsoleWriter recipeWriter = new RecipeConsoleWriter();

IRecipeRepository recipeRepository = CreateRecipeRepository(appSettings);
RecipeImporter recipeImporter = new RecipeImporter();

IRecipeImporterService importerService = new RecipeImporterService(recipeRepository, recipeImporter);

IRecipeBackupService backupService = new RecipeBackupService(recipeRepository);

IRecipeApp app = RecipeAppFactory.Create(appSettings.CurrentInterfaceMode,
    recipeReader, recipeWriter, importerService, backupService);

await app.RunAsync();

IRecipeRepository CreateRecipeRepository(AppSettings settings)
{
    if (settings.CurrentStorageMode == StorageMode.Json)
    {
        return new JsonRecipeRepository(settings.JsonFilePath);
    }

    if (settings.CurrentStorageMode == StorageMode.Database)
    {
        DatabaseInitializer databaseInitializer = new DatabaseInitializer(settings.DatabasePath);
        databaseInitializer.Initialize();

        DatabaseMigrator migrator = new DatabaseMigrator(settings.DatabasePath);
        migrator.ApplyMigrations();

        return new DatabaseRecipeRepository(settings.DatabasePath);
    }

    throw new InvalidOperationException(AppTexts.UnknownStorageModeError);
}

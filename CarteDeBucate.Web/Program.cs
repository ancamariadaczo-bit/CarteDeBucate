var builder = WebApplication.CreateBuilder(args);

string databasePath = builder.Configuration["DatabasePath"]
    ?? throw new InvalidOperationException("DatabasePath is not configured.");

AppServiceFactory.EnsureDatabaseIsUpToDate(databasePath);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<IRecipeRepository>(_ =>
    AppServiceFactory.CreateDatabaseRecipeRepository(databasePath));
builder.Services.AddScoped<IUserRepository>(_ =>
    AppServiceFactory.CreateDatabaseUserRepository(databasePath));
builder.Services.AddScoped<IRecipeImporterService>(serviceProvider =>
{
    IRecipeRepository recipeRepository =
        serviceProvider.GetRequiredService<IRecipeRepository>();
    ICurrentUserContext currentUserContext =
        serviceProvider.GetRequiredService<ICurrentUserContext>();

    return AppServiceFactory.CreateRecipeImporterService(
        recipeRepository,
        currentUserContext);
});
builder.Services.AddScoped<IRecipeBackupService>(serviceProvider =>
{
    IRecipeRepository recipeRepository =
        serviceProvider.GetRequiredService<IRecipeRepository>();
    ICurrentUserContext currentUserContext =
        serviceProvider.GetRequiredService<ICurrentUserContext>();

    return AppServiceFactory.CreateRecipeBackupService(
        recipeRepository,
        currentUserContext);
});
builder.Services.AddScoped<IAuthenticationService>(serviceProvider =>
{
    IUserRepository userRepository =
        serviceProvider.GetRequiredService<IUserRepository>();
    ICurrentUserContext currentUserContext =
        serviceProvider.GetRequiredService<ICurrentUserContext>();

    return AppServiceFactory.CreateAuthenticationService(
        userRepository,
        currentUserContext);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

using CarteDeBucate.Web.Configuration;
using CarteDeBucate.Web.Filters;
using CarteDeBucate.Web.Services.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

string databasePath = builder.Configuration["DatabasePath"]
    ?? throw new InvalidOperationException("DatabasePath is not configured.");
WebAppSettings webAppSettings = builder.Configuration.Get<WebAppSettings>()
    ?? new WebAppSettings();

// Add services to the container.
builder.Services.AddControllersWithViews();


string[] allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("ChromeExtension", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

string jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");

string jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    })
    .AddJwtBearer(
        JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey))
            };
        });
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IExtensionAuthenticationCodeService, ExtensionAuthenticationCodeService>();
builder.Services.AddSingleton(webAppSettings);
builder.Services.AddScoped<OptionalAuthenticationFilter>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
builder.Services.AddScoped<IRecipeRepository>(serviceProvider =>
    AppServiceFactory.CreateDatabaseRecipeRepository(
        databasePath,
        serviceProvider.GetRequiredService<ILogger<DatabaseRecipeRepository>>()));
builder.Services.AddScoped<IUserRepository>(_ =>
    AppServiceFactory.CreateDatabaseUserRepository(databasePath));
builder.Services.AddScoped<IRecipeLibraryService>(serviceProvider =>
{
    IRecipeRepository recipeRepository =
        serviceProvider.GetRequiredService<IRecipeRepository>();
    ICurrentUserContext currentUserContext =
        serviceProvider.GetRequiredService<ICurrentUserContext>();

    return AppServiceFactory.CreateRecipeLibraryService(
        recipeRepository,
        currentUserContext);
});
builder.Services.AddSingleton<IHostAddressResolver, DnsHostAddressResolver>();
builder.Services.AddSingleton<RecipeImportDestinationPolicy>();
builder.Services
    .AddHttpClient<IRecipeImporter, RecipeImporter>(
        RecipeImporterHttpClientConfiguration.Configure)
    .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        new RecipeImportHttpMessageHandler(
            serviceProvider.GetRequiredService<RecipeImportDestinationPolicy>()));
builder.Services.AddScoped<IRecipeImporterService, RecipeImporterService>();
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

app.Logger.LogInformation(
    "Application starting in {EnvironmentName} environment. Authentication enabled: {AuthenticationEnabled}",
    app.Environment.EnvironmentName,
    webAppSettings.AuthenticationEnabled);

try
{
    app.Logger.LogInformation("Database initialization and migrations started");

    AppServiceFactory.EnsureDatabaseIsUpToDate(databasePath);

    app.Logger.LogInformation("Database initialization and migrations completed successfully");
}
catch (Exception exception)
{
    app.Logger.LogCritical(
        exception,
        "Application startup failed while initializing or migrating the database");

    throw;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(new ExceptionHandlerOptions
    {
        ExceptionHandlingPath = "/Home/Error",
        SuppressDiagnosticsCallback = _ => true
    });
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("ChromeExtension");

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

public partial class Program
{
}

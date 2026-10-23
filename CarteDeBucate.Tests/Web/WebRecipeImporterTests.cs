using Microsoft.Extensions.DependencyInjection;

public class WebRecipeImporterTests
{
    [Fact]
    public void Services_ShouldResolveImporterAndImporterService()
    {
        using AuthenticationApiWebApplicationFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();

        IRecipeImporter importer =
            scope.ServiceProvider.GetRequiredService<IRecipeImporter>();
        IRecipeImporterService importerService =
            scope.ServiceProvider.GetRequiredService<IRecipeImporterService>();

        Assert.NotNull(importer);
        Assert.NotNull(importerService);
    }
}

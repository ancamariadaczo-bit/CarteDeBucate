using System.Net;

public class RecipeImporterTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("www.google.com")]
    [InlineData("google.com")]
    [InlineData("ftp://example.com/recipe")]
    [InlineData("mailto:test@example.com")]
    public async Task ImportFromUrlAsync_WithInvalidUrl_ShouldFail(string url)
    {
        RecipeImporter importer = CreateImporterReturningHtml("");

        RecipeImportResult result = await importer.ImportFromUrlAsync(url);

        Assert.False(result.Success);
        Assert.Null(result.Recipe);
        Assert.Equal(AppTexts.ImportFailedInvalidUrl, result.Message);
    }

    [Fact]
    public async Task ImportFromUrlAsync_WithEmptyUrl_ShouldFail()
    {
        RecipeImporter importer = CreateImporterReturningHtml("");

        RecipeImportResult result = await importer.ImportFromUrlAsync("");

        Assert.False(result.Success);
        Assert.Null(result.Recipe);
        Assert.Equal(AppTexts.ImportFailedEmptyUrl, result.Message);
    }

    [Fact]
    public async Task ImportFromUrlAsync_WhenPageCannotBeDownloaded_ShouldFail()
    {
        HttpMessageHandler handler = new ThrowingHttpMessageHandler();
        HttpClient httpClient = new HttpClient(handler);
        RecipeImporter importer = new RecipeImporter(httpClient);

        RecipeImportResult result = await importer.ImportFromUrlAsync(
            "https://example.com/recipe");

        Assert.False(result.Success);
        Assert.Null(result.Recipe);
        Assert.Equal(AppTexts.ImportFailedCouldNotDownloadPage, result.Message);
    }

    [Fact]
    public async Task ImportFromUrlAsync_WithBlockedPage_ShouldFailWithBlockedMessage()
    {
        string html = """
        <html>
            <body>
                Please wait while your request is being verified...
            </body>
        </html>
        """;

        RecipeImporter importer = CreateImporterReturningHtml(html);

        RecipeImportResult result = await importer.ImportFromUrlAsync(
            "https://example.com/recipe");

        Assert.False(result.Success);
        Assert.Null(result.Recipe);
        Assert.Equal(AppTexts.ImportFailedBlockedPage, result.Message);
    }

    [Fact]
    public async Task ImportFromUrlAsync_WithNonRecipePage_ShouldFail()
    {
        string html = """
        <html>
            <head>
                <title>About us</title>
            </head>
            <body>
                <h1>About our company</h1>
                <p>We provide software services and consulting.</p>
            </body>
        </html>
        """;

        RecipeImporter importer = CreateImporterReturningHtml(html);

        RecipeImportResult result = await importer.ImportFromUrlAsync(
            "https://example.com/about");

        Assert.False(result.Success);
        Assert.Null(result.Recipe);
        Assert.Equal(AppTexts.ImportFailedNotRecipePage, result.Message);
    }

    [Fact]
    public async Task ImportFromUrlAsync_WithJsonLdRecipe_ShouldExtractNameIngredientsAndSteps()
    {
        string html = """
        <html>
            <head>
                <title>Fallback title</title>
                <script type="application/ld+json">
                {
                    "@context": "https://schema.org",
                    "@type": "Recipe",
                    "name": "Banana bread keto",
                    "recipeIngredient": [
                        "2 bananas",
                        "100 g almond flour",
                        "2 eggs"
                    ],
                    "recipeInstructions": [
                        {
                            "@type": "HowToStep",
                            "text": "Mix all ingredients."
                        },
                        {
                            "@type": "HowToStep",
                            "text": "Bake for 40 minutes."
                        }
                    ]
                }
                </script>
            </head>
            <body></body>
        </html>
        """;

        RecipeImporter importer = CreateImporterReturningHtml(html);

        RecipeImportResult result = await importer.ImportFromUrlAsync(
            "https://example.com/banana-bread-recipe");

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Recipe);

        Assert.Equal("Banana bread keto", result.Recipe.Name);
        Assert.Contains(result.Recipe.Ingredients, ingredient =>
            ingredient.Contains("almond flour", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recipe.Steps, step =>
            step.Contains("Bake", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportFromUrlAsync_WithJsonLdGraphRecipe_ShouldExtractRecipe()
    {
        string html = """
        <html>
            <head>
                <script type="application/ld+json">
                {
                    "@context": "https://schema.org",
                    "@graph": [
                        {
                            "@type": "WebPage",
                            "name": "Some page"
                        },
                        {
                            "@type": "Recipe",
                            "name": "Tomato soup",
                            "recipeIngredient": [
                                "500 g tomatoes",
                                "1 onion"
                            ],
                            "recipeInstructions": [
                                {
                                    "@type": "HowToStep",
                                    "text": "Cook the vegetables."
                                },
                                {
                                    "@type": "HowToStep",
                                    "text": "Blend the soup."
                                }
                            ]
                        }
                    ]
                }
                </script>
            </head>
            <body></body>
        </html>
        """;

        RecipeImporter importer = CreateImporterReturningHtml(html);

        RecipeImportResult result = await importer.ImportFromUrlAsync(
            "https://example.com/tomato-soup-recipe");

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Recipe);

        Assert.Equal("Tomato soup", result.Recipe.Name);
        Assert.Contains(result.Recipe.Ingredients, ingredient =>
            ingredient.Contains("tomatoes", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recipe.Steps, step =>
            step.Contains("Blend", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportFromUrlAsync_WithVisibleHtmlRecipe_ShouldExtractIngredientsAndSteps()
    {
        string html = """
        <html>
            <head>
                <title>Salată cu linte verde</title>
            </head>
            <body>
                <article>
                    <h1>Salată cu linte verde, rucola și roșii cherry</h1>

                    <h2>Ingrediente</h2>
                    <ul>
                        <li>200 g linte verde</li>
                        <li>100 g rucola</li>
                        <li>150 g roșii cherry</li>
                    </ul>

                    <h2>Pregătire</h2>
                    <ol>
                        <li>Fierbe lintea.</li>
                        <li>Amestecă lintea cu rucola și roșiile.</li>
                    </ol>
                </article>
            </body>
        </html>
        """;

        RecipeImporter importer = CreateImporterReturningHtml(html);

        RecipeImportResult result = await importer.ImportFromUrlAsync(
            "https://example.com/salata-cu-linte-reteta");

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Recipe);

        Assert.Contains("Salată cu linte", result.Recipe.Name);
        Assert.Contains(result.Recipe.Ingredients, ingredient =>
            ingredient.Contains("linte", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Recipe.Steps, step =>
            step.Contains("Fierbe", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportFromUrlAsync_WithRecipeThatHasIngredientsButNoSteps_ShouldStillDetectRecipe()
    {
        string html = """
        <html>
            <head>
                <title>Dip de ricotta</title>
            </head>
            <body>
                <h1>Dip de ricotta cu roșii uscate</h1>

                <h2>Ingrediente</h2>
                <ul>
                    <li>250 g ricotta</li>
                    <li>50 g roșii uscate</li>
                    <li>ulei de măsline</li>
                </ul>
            </body>
        </html>
        """;

        RecipeImporter importer = CreateImporterReturningHtml(html);

        RecipeImportResult result = await importer.ImportFromUrlAsync(
            "https://example.com/dip-de-ricotta-reteta");

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Recipe);

        Assert.True(result.Recipe.Ingredients.Count >= 2);
        Assert.Empty(result.Recipe.Steps);
    }

    [Fact]
    public async Task ImportFromUrlAsync_WithRecipeThatHasStepsButNoIngredients_ShouldStillDetectRecipe()
    {
        string html = """
        <html>
            <head>
                <title>Simple method page</title>
            </head>
            <body>
                <h1>How to make roasted pumpkin</h1>

                <h2>Instructions</h2>
                <ol>
                    <li>Cut the pumpkin.</li>
                    <li>Bake until soft.</li>
                </ol>
            </body>
        </html>
        """;

        RecipeImporter importer = CreateImporterReturningHtml(html);

        RecipeImportResult result = await importer.ImportFromUrlAsync(
            "https://example.com/roasted-pumpkin-recipe");

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Recipe);

        Assert.Empty(result.Recipe.Ingredients);
        Assert.True(result.Recipe.Steps.Count >= 2);
    }

    [Fact]
    public async Task ImportFromUrlAsync_PageContainsLoaderWordButIsRecipe_ShouldNotBeConsideredBlocked()
    {
        string html = """
        <html>
            <head>
                <title>Pancakes recipe</title>
            </head>
            <body>
                <div class="image-loader"></div>

                <h1>Pancakes recipe</h1>

                <h2>Ingredients</h2>
                <ul>
                    <li>2 eggs</li>
                    <li>200 ml milk</li>
                    <li>100 g flour</li>
                </ul>

                <h2>Directions</h2>
                <ol>
                    <li>Mix the ingredients.</li>
                    <li>Cook in a pan.</li>
                </ol>
            </body>
        </html>
        """;

        RecipeImporter importer = CreateImporterReturningHtml(html);

        RecipeImportResult result = await importer.ImportFromUrlAsync(
            "https://example.com/pancakes-recipe");

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Recipe);
    }

    private static RecipeImporter CreateImporterReturningHtml(string html)
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html)
            });

        HttpClient httpClient = new HttpClient(handler);

        return new RecipeImporter(httpClient);
    }

    private class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Simulated download error.");
        }
    }
}
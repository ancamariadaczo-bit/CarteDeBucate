using RecipeExtraction;
using Xunit;

public class RecipeJsonLdExtractorTests
{
    private const string SourceUrl = "https://example.com/recipes/test-recipe";

    [Fact]
    public void TryExtract_WhenRecipeIsAtDocumentRoot_ReturnsRecipe()
    {
        string html = """
            <html>
            <head>
                <script type="application/ld+json">
                {
                    "@context": "https://schema.org",
                    "@type": "Recipe",
                    "name": "Root Recipe"
                }
                </script>
            </head>
            </html>
            """;
        RecipeJsonLdExtractor extractor = new RecipeJsonLdExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal("Root Recipe", recipe.Name);
        Assert.Equal(SourceUrl, recipe.SourceUrl);
    }

    [Fact]
    public void TryExtract_WhenRecipeIsInsideRootArray_ReturnsRecipe()
    {
        string html = """
            <html>
            <head>
                <script type="application/ld+json">
                [
                    {
                        "@type": "WebPage",
                        "name": "Not a Recipe"
                    },
                    {
                        "@type": "Recipe",
                        "name": "Array Recipe"
                    }
                ]
                </script>
            </head>
            </html>
            """;
        RecipeJsonLdExtractor extractor = new RecipeJsonLdExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal("Array Recipe", recipe.Name);
    }

    [Fact]
    public void TryExtract_WhenRecipeIsInsideGraph_ReturnsRecipe()
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
                            "name": "Not a Recipe"
                        },
                        {
                            "@type": "Recipe",
                            "name": "Graph Recipe"
                        }
                    ]
                }
                </script>
            </head>
            </html>
            """;
        RecipeJsonLdExtractor extractor = new RecipeJsonLdExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal("Graph Recipe", recipe.Name);
    }

    [Fact]
    public void TryExtract_WhenTypeIsString_ReturnsRecipe()
    {
        string html = """
            <script type="application/ld+json">
            {
                "@type": "Recipe",
                "name": "String Type Recipe"
            }
            </script>
            """;
        RecipeJsonLdExtractor extractor = new RecipeJsonLdExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal("String Type Recipe", recipe.Name);
    }

    [Fact]
    public void TryExtract_WhenTypeArrayContainsRecipe_ReturnsRecipe()
    {
        string html = """
            <script type="application/ld+json">
            {
                "@type": ["Thing", "Recipe"],
                "name": "Array Type Recipe"
            }
            </script>
            """;
        RecipeJsonLdExtractor extractor = new RecipeJsonLdExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal("Array Type Recipe", recipe.Name);
    }

    [Fact]
    public void TryExtract_WhenInstructionsAreStrings_ExtractsStepsInOrder()
    {
        string html = """
            <script type="application/ld+json">
            {
                "@type": "Recipe",
                "name": "String Instructions Recipe",
                "recipeInstructions": [
                    "Mix the ingredients.",
                    "Bake the mixture."
                ]
            }
            </script>
            """;
        RecipeJsonLdExtractor extractor = new RecipeJsonLdExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal(
            new[] { "Mix the ingredients.", "Bake the mixture." },
            recipe.Steps);
    }

    [Fact]
    public void TryExtract_WhenInstructionsAreObjects_ExtractsTextInOrder()
    {
        string html = """
            <script type="application/ld+json">
            {
                "@type": "Recipe",
                "name": "Object Instructions Recipe",
                "recipeInstructions": [
                    {
                        "@type": "HowToStep",
                        "text": "Prepare the ingredients."
                    },
                    {
                        "@type": "HowToStep",
                        "text": "Cook the ingredients."
                    }
                ]
            }
            </script>
            """;
        RecipeJsonLdExtractor extractor = new RecipeJsonLdExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal(
            new[] { "Prepare the ingredients.", "Cook the ingredients." },
            recipe.Steps);
    }

    [Fact]
    public void TryExtract_WhenJsonLdIsInvalid_ReturnsNullWithoutThrowing()
    {
        string html = """
            <script type="application/ld+json">
            {
                "@type": "Recipe",
                "name": "Invalid Recipe",
            }
            </script>
            """;
        RecipeJsonLdExtractor extractor = new RecipeJsonLdExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.Null(recipe);
    }

    [Fact]
    public void TryExtract_WhenInvalidBlockPrecedesValidBlock_ReturnsRecipeFromValidBlock()
    {
        string html = """
            <script type="application/ld+json">
            {
                "@type": "Recipe",
                "name": "Invalid Recipe",
            }
            </script>
            <script type="application/ld+json">
            {
                "@type": "Recipe",
                "name": "Valid Recipe"
            }
            </script>
            """;
        RecipeJsonLdExtractor extractor = new RecipeJsonLdExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Equal("Valid Recipe", recipe.Name);
    }

    [Fact]
    public void TryExtract_WhenNameIsMissing_ReturnsNull()
    {
        string html = """
            <script type="application/ld+json">
            {
                "@type": "Recipe",
                "recipeIngredient": ["One ingredient"],
                "recipeInstructions": ["One instruction"]
            }
            </script>
            """;
        RecipeJsonLdExtractor extractor = new RecipeJsonLdExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.Null(recipe);
    }

    [Fact]
    public void TryExtract_WhenIngredientsAreMissing_ReturnsRecipeWithEmptyIngredients()
    {
        string html = """
            <script type="application/ld+json">
            {
                "@type": "Recipe",
                "name": "Recipe Without Ingredients",
                "recipeInstructions": ["One instruction"]
            }
            </script>
            """;
        RecipeJsonLdExtractor extractor = new RecipeJsonLdExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Empty(recipe.Ingredients);
    }

    [Fact]
    public void TryExtract_WhenInstructionsAreMissing_ReturnsRecipeWithEmptySteps()
    {
        string html = """
            <script type="application/ld+json">
            {
                "@type": "Recipe",
                "name": "Recipe Without Instructions",
                "recipeIngredient": ["One ingredient"]
            }
            </script>
            """;
        RecipeJsonLdExtractor extractor = new RecipeJsonLdExtractor();

        ExtractedRecipe? recipe = extractor.TryExtract(html, SourceUrl);

        Assert.NotNull(recipe);
        Assert.Empty(recipe.Steps);
    }
}

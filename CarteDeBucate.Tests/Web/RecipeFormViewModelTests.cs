using System.ComponentModel.DataAnnotations;
using CarteDeBucate.Web.Models;
using Microsoft.AspNetCore.Http;

public class RecipeFormViewModelTests
{
    [Fact]
    public void Name_WhenEmpty_ShouldReturnRequiredValidationError()
    {
        RecipeFormViewModel model = CreateValidModel();
        model.Name = "";

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(result, nameof(RecipeFormViewModel.Name),
                "Numele este obligatoriu."));
    }

    [Fact]
    public void SourceUrl_WhenEmpty_ShouldReturnRequiredValidationError()
    {
        RecipeFormViewModel model = CreateValidModel();
        model.SourceUrl = "";

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(result, nameof(RecipeFormViewModel.SourceUrl),
                "URL-ul sursă este obligatoriu."));
    }

    [Fact]
    public void SourceUrl_WhenInvalid_ShouldReturnUrlValidationError()
    {
        RecipeFormViewModel model = CreateValidModel();
        model.SourceUrl = "not a url";

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(result, nameof(RecipeFormViewModel.SourceUrl),
                "Introdu un URL valid."));
    }

    [Fact]
    public void IngredientsText_WhenEmpty_ShouldReturnRequiredValidationError()
    {
        RecipeFormViewModel model = CreateValidModel();
        model.IngredientsText = "";

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(result, nameof(RecipeFormViewModel.IngredientsText),
                "Ingredientele sunt obligatorii."));
    }

    [Fact]
    public void StepsText_WhenEmpty_ShouldReturnRequiredValidationError()
    {
        RecipeFormViewModel model = CreateValidModel();
        model.StepsText = "";

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(result, nameof(RecipeFormViewModel.StepsText),
                "Pașii sunt obligatorii."));
    }

    [Fact]
    public void ValidModel_ShouldPassValidation()
    {
        RecipeFormViewModel model = CreateValidModel();

        List<ValidationResult> results = Validate(model);

        Assert.Empty(results);
    }

    [Fact]
    public void Photo_WhenMissing_ShouldRemainOptional()
    {
        RecipeFormViewModel model = CreateValidModel();
        model.Photo = null;

        List<ValidationResult> results = Validate(model);

        Assert.Empty(results);
    }

    [Fact]
    public void FromRecipe_ShouldLeavePhotoEmpty()
    {
        Recipe recipe = new Recipe
        {
            Id = 17,
            Name = "Supa",
            SourceUrl = "https://example.com/supa",
            Ingredients = ["Apa", "Sare"],
            Steps = ["Fierbe apa"],
            Notes = "Serveste calda",
            Status = RecipeStatus.Favorite,
            SavedAt = new DateTime(2026, 11, 9),
            UserId = 5
        };

        RecipeFormViewModel model = RecipeFormViewModel.FromRecipe(recipe);

        Assert.Equal(recipe.Id, model.Id);
        Assert.Equal(recipe.Name, model.Name);
        Assert.Equal(recipe.SourceUrl, model.SourceUrl);
        Assert.Equal("Apa" + Environment.NewLine + "Sare", model.IngredientsText);
        Assert.Equal("Fierbe apa", model.StepsText);
        Assert.Equal(recipe.Notes, model.Notes);
        Assert.Equal(recipe.Status, model.Status);
        Assert.Equal(recipe.SavedAt, model.SavedAt);
        Assert.Equal(recipe.UserId, model.UserId);
        Assert.Null(model.Photo);
    }

    [Fact]
    public void ToRecipe_WithPhoto_ShouldMapOnlyRecipeData()
    {
        using MemoryStream content = new MemoryStream([1, 2, 3]);
        IFormFile photo = new FormFile(
            content,
            0,
            content.Length,
            "Photo",
            "supa.jpg");
        RecipeFormViewModel model = CreateValidModel();
        model.Id = 17;
        model.Notes = "Serveste calda";
        model.Status = RecipeStatus.Favorite;
        model.SavedAt = new DateTime(2026, 11, 9);
        model.UserId = 5;
        model.Photo = photo;

        Recipe recipe = model.ToRecipe();

        Assert.Equal(model.Id, recipe.Id);
        Assert.Equal(model.Name, recipe.Name);
        Assert.Equal(model.SourceUrl, recipe.SourceUrl);
        Assert.Equal(new List<string> { "Apa" }, recipe.Ingredients);
        Assert.Equal(new List<string> { "Fierbe apa" }, recipe.Steps);
        Assert.Equal(model.Notes, recipe.Notes);
        Assert.Equal(model.Status, recipe.Status);
        Assert.Equal(model.SavedAt, recipe.SavedAt);
        Assert.Equal(model.UserId, recipe.UserId);
        Assert.Equal(0, content.Position);
        Assert.True(content.CanRead);
        Assert.Null(typeof(Recipe).GetProperty(nameof(RecipeFormViewModel.Photo)));
    }

    private static RecipeFormViewModel CreateValidModel()
    {
        return new RecipeFormViewModel
        {
            Name = "Supa",
            SourceUrl = "https://example.com/supa",
            IngredientsText = "Apa",
            StepsText = "Fierbe apa"
        };
    }

    private static List<ValidationResult> Validate(RecipeFormViewModel model)
    {
        ValidationContext context = new ValidationContext(model);
        List<ValidationResult> results = new List<ValidationResult>();

        Validator.TryValidateObject(
            model,
            context,
            results,
            validateAllProperties: true);

        return results;
    }

    private static bool HasError(
        ValidationResult result,
        string memberName,
        string errorMessage)
    {
        return result.ErrorMessage == errorMessage
            && result.MemberNames.Contains(memberName);
    }
}

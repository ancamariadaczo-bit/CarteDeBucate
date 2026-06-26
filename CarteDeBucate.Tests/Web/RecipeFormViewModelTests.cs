using System.ComponentModel.DataAnnotations;
using CarteDeBucate.Web.Models;

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
                "URL-ul sursa este obligatoriu."));
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
                "Pasii sunt obligatorii."));
    }

    [Fact]
    public void ValidModel_ShouldPassValidation()
    {
        RecipeFormViewModel model = CreateValidModel();

        List<ValidationResult> results = Validate(model);

        Assert.Empty(results);
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

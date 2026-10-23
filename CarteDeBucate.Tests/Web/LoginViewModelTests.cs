using System.ComponentModel.DataAnnotations;
using CarteDeBucate.Web.Models;

public class LoginViewModelTests
{
    [Fact]
    public void Username_WhenEmpty_ShouldReturnRequiredValidationError()
    {
        LoginViewModel model = CreateValidModel();
        model.Username = "";

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(result, nameof(LoginViewModel.Username),
                "Username-ul este obligatoriu."));
    }

    [Fact]
    public void Password_WhenEmpty_ShouldReturnRequiredValidationError()
    {
        LoginViewModel model = CreateValidModel();
        model.Password = "";

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(result, nameof(LoginViewModel.Password),
                "Parola este obligatorie."));
    }

    [Fact]
    public void ValidModel_ShouldPassValidation()
    {
        LoginViewModel model = CreateValidModel();

        List<ValidationResult> results = Validate(model);

        Assert.Empty(results);
    }

    private static LoginViewModel CreateValidModel()
    {
        return new LoginViewModel
        {
            Username = "anca",
            Password = "password"
        };
    }

    private static List<ValidationResult> Validate(LoginViewModel model)
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

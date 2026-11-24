using System.ComponentModel.DataAnnotations;
using CarteDeBucate.Web.Models;

public class RegisterViewModelTests
{
    [Fact]
    public void Email_WhenEmpty_ShouldReturnRequiredValidationError()
    {
        RegisterViewModel model = CreateValidModel();
        model.Email = "";

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(result, nameof(RegisterViewModel.Email),
                "Adresa de e-mail este obligatorie."));
    }

    [Fact]
    public void Email_WhenInvalid_ShouldReturnEmailValidationError()
    {
        RegisterViewModel model = CreateValidModel();
        model.Email = "not-an-email";

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(result, nameof(RegisterViewModel.Email),
                "Introdu o adresă de e-mail validă."));
    }

    [Fact]
    public void Password_WhenEmpty_ShouldReturnRequiredValidationError()
    {
        RegisterViewModel model = CreateValidModel();
        model.Password = "";

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(result, nameof(RegisterViewModel.Password),
                "Parola este obligatorie."));
    }

    [Fact]
    public void Password_WhenShorterThanTwelveCharacters_ShouldReturnLengthError()
    {
        RegisterViewModel model = CreateValidModel();
        model.Password = "too-short";
        model.ConfirmPassword = "too-short";

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(result, nameof(RegisterViewModel.Password),
                "Parola trebuie să aibă cel puțin 12 caractere."));
    }

    [Fact]
    public void ConfirmPassword_WhenEmpty_ShouldReturnRequiredValidationError()
    {
        RegisterViewModel model = CreateValidModel();
        model.ConfirmPassword = "";

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(result, nameof(RegisterViewModel.ConfirmPassword),
                "Confirmarea parolei este obligatorie."));
    }

    [Fact]
    public void ConfirmPassword_WhenDifferentFromPassword_ShouldReturnCompareValidationError()
    {
        RegisterViewModel model = CreateValidModel();
        model.ConfirmPassword = "other-password";

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(result, nameof(RegisterViewModel.ConfirmPassword),
                "Parolele nu coincid."));
    }

    [Fact]
    public void ValidModel_ShouldPassValidation()
    {
        RegisterViewModel model = CreateValidModel();

        List<ValidationResult> results = Validate(model);

        Assert.Empty(results);
    }

    private static RegisterViewModel CreateValidModel()
    {
        return new RegisterViewModel
        {
            Email = "anca@example.com",
            Password = "secure-password",
            ConfirmPassword = "secure-password"
        };
    }

    private static List<ValidationResult> Validate(RegisterViewModel model)
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

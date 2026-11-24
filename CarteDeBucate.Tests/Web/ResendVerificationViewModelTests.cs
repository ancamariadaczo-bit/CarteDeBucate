using System.ComponentModel.DataAnnotations;
using CarteDeBucate.Web.Models;

public class ResendVerificationViewModelTests
{
    [Fact]
    public void Email_WhenEmpty_ShouldReturnRequiredValidationError()
    {
        ResendVerificationViewModel model = new();

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(
                result,
                nameof(ResendVerificationViewModel.Email),
                "Adresa de e-mail este obligatorie."));
    }

    [Fact]
    public void Email_WhenInvalid_ShouldReturnEmailValidationError()
    {
        ResendVerificationViewModel model = new()
        {
            Email = "not-an-email"
        };

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => HasError(
                result,
                nameof(ResendVerificationViewModel.Email),
                "Introdu o adresă de e-mail validă."));
    }

    [Fact]
    public void ValidModel_ShouldPassValidation()
    {
        ResendVerificationViewModel model = new()
        {
            Email = "anca@example.com"
        };

        Assert.Empty(Validate(model));
    }

    private static List<ValidationResult> Validate(
        ResendVerificationViewModel model)
    {
        ValidationContext context = new(model);
        List<ValidationResult> results = [];

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
        return result.ErrorMessage == errorMessage &&
            result.MemberNames.Contains(memberName);
    }
}

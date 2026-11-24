using System.ComponentModel.DataAnnotations;

namespace CarteDeBucate.Web.Models;

public class RegisterViewModel
{
    [Required(
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.EmailRequired))]
    [EmailAddress(
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.EmailInvalid))]
    [StringLength(EmailNormalizer.MaximumEmailLength,
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.EmailLength))]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = "";

    [Required(
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.PasswordRequired))]
    [MinLength(AccountRegistrationService.MinimumPasswordLength,
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.RegistrationPasswordLength))]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.ConfirmPasswordRequired))]
    [DataType(DataType.Password)]
    [Compare(nameof(Password),
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.PasswordMismatch))]
    public string ConfirmPassword { get; set; } = "";
}

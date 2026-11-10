using System.ComponentModel.DataAnnotations;

namespace CarteDeBucate.Web.Models;

public class RegisterViewModel
{
    [Required(
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.UsernameRequired))]
    [StringLength(30, MinimumLength = 3,
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.UsernameLength))]
    [RegularExpression(@"^[\p{L}\p{N} ._-]+$",
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.UsernameCharacters))]
    public string Username { get; set; } = "";

    [Required(
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.PasswordRequired))]
    [MinLength(5,
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.PasswordLength))]
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

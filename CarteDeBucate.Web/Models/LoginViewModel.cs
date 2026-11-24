using System.ComponentModel.DataAnnotations;

namespace CarteDeBucate.Web.Models;

public class LoginViewModel
{
    [Required(
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.EmailRequired))]
    [EmailAddress(
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.EmailInvalid))]
    [StringLength(254,
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.EmailLength))]
    public string Email { get; set; } = "";

    [Required(
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.PasswordRequired))]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

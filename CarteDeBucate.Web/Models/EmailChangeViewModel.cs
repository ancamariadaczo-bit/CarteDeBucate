using System.ComponentModel.DataAnnotations;

namespace CarteDeBucate.Web.Models;

public sealed class EmailChangeViewModel
{
    [Required(
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.EmailRequired))]
    [EmailAddress(
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.EmailInvalid))]
    [StringLength(
        254,
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.EmailLength))]
    public string NewEmail { get; set; } = string.Empty;

    [Required(
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.PasswordRequired))]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;
}

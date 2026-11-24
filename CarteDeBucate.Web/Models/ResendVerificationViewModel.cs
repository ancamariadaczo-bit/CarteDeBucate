using System.ComponentModel.DataAnnotations;

namespace CarteDeBucate.Web.Models;

public sealed class ResendVerificationViewModel
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
    public string Email { get; set; } = string.Empty;
}

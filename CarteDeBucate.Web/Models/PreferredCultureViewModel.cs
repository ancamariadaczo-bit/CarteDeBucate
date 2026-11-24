using System.ComponentModel.DataAnnotations;

namespace CarteDeBucate.Web.Models;

public sealed class PreferredCultureViewModel
{
    [Required(
        ErrorMessageResourceType = typeof(Resources.ValidationMessages),
        ErrorMessageResourceName = nameof(Resources.ValidationMessages.CultureRequired))]
    public string PreferredCulture { get; set; } = "";
}

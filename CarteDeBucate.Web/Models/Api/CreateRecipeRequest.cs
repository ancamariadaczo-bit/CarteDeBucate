using System.ComponentModel.DataAnnotations;

namespace CarteDeBucate.Web.Models.Api;

public class CreateRecipeRequest : IValidatableObject
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string SourceUrl { get; set; } = string.Empty;

    [Required]
    public List<string> Ingredients { get; set; } = [];

    [Required]
    public List<string> Steps { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(SourceUrl) &&
            !UrlValidator.IsValidHttpUrl(SourceUrl))
        {
            yield return new ValidationResult(
                AppTexts.RecipeSourceUrlInvalid,
                new[] { nameof(SourceUrl) });
        }
    }
}

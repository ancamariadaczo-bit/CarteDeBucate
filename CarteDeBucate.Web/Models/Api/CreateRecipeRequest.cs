using System.ComponentModel.DataAnnotations;

namespace CarteDeBucate.Web.Models.Api;

public class CreateRecipeRequest : IValidatableObject
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    [Required]
    public List<string> Ingredients { get; set; } = [];

    public List<string>? Steps { get; set; }

    public string? ImageUrl { get; set; }

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

        if (!string.IsNullOrWhiteSpace(ImageUrl) &&
            !UrlValidator.IsValidHttpUrl(ImageUrl))
        {
            yield return new ValidationResult(
                "The image URL must be a valid HTTP or HTTPS URL.",
                new[] { nameof(ImageUrl) });
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace CarteDeBucate.Web.Models.Api;

public class CreateRecipeRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string SourceUrl { get; set; } = string.Empty;

    [Required]
    public List<string> Ingredients { get; set; } = [];

    [Required]
    public List<string> Steps { get; set; } = [];
}
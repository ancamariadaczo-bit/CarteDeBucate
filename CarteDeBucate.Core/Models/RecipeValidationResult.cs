public class RecipeValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; } = new();
}
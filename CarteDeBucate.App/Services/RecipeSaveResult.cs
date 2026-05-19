public class RecipeSaveResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public Recipe? Recipe { get; set; }

    public static RecipeSaveResult Success(string message, Recipe recipe)
    {
        return new RecipeSaveResult
        {
            IsSuccess = true,
            Message = message,
            Recipe = recipe
        };
    }

    public static RecipeSaveResult Fail(string message)
    {
        return new RecipeSaveResult
        {
            IsSuccess = false,
            Message = message
        };
    }
}
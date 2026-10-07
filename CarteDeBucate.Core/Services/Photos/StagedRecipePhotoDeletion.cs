internal sealed class StagedRecipePhotoDeletion
{
    internal StagedRecipePhotoDeletion(string token)
    {
        Token = token;
    }

    internal string Token { get; }
}

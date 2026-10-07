internal sealed class StagedRecipePhotoUpload
{
    internal StagedRecipePhotoUpload(string token, long length)
    {
        Token = token;
        Length = length;
    }

    internal string Token { get; }

    public long Length { get; }
}

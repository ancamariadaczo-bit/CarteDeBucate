internal sealed class RecipePhotoStorageLimitExceededException : IOException
{
    public RecipePhotoStorageLimitExceededException(long maximumBytes)
        : base($"The photo content exceeds the limit of {maximumBytes} bytes.")
    {
        MaximumBytes = maximumBytes;
    }

    public long MaximumBytes { get; }
}

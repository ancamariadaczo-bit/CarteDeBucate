public class RecipePhoto
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string StorageFileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int DisplayOrder { get; set; }
    public RecipePhotoOrigin Origin { get; set; } = RecipePhotoOrigin.Unspecified;
}

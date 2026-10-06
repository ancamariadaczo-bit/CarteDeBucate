public sealed class RecipePhotoValidator
{
    private const int MaximumSignatureLength = 12;

    public RecipePhotoValidationResult Validate(
        Stream content,
        string declaredContentType)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanRead || !content.CanSeek)
        {
            throw new ArgumentException(
                "The photo content stream must be readable and seekable.",
                nameof(content));
        }

        long initialPosition = content.Position;

        try
        {
            content.Position = 0;

            Span<byte> signature = stackalloc byte[MaximumSignatureLength];
            int signatureLength = ReadSignature(content, signature);

            if (signatureLength == 0)
            {
                return RecipePhotoValidationResult.Failure(
                    RecipePhotoResultCode.EmptyContent);
            }

            RecipePhotoFormat detectedFormat =
                RecipePhotoFormats.DetectFormat(signature[..signatureLength]);
            RecipePhotoFormat declaredFormat =
                RecipePhotoFormats.GetFormatFromContentType(declaredContentType);

            if (detectedFormat == RecipePhotoFormat.Unknown)
            {
                RecipePhotoResultCode code =
                    declaredFormat == RecipePhotoFormat.Unknown
                        ? RecipePhotoResultCode.UnsupportedFormat
                        : RecipePhotoResultCode.InvalidContent;

                return RecipePhotoValidationResult.Failure(code);
            }

            if (declaredFormat == RecipePhotoFormat.Unknown)
            {
                return RecipePhotoValidationResult.Failure(
                    RecipePhotoResultCode.UnsupportedFormat);
            }

            if (detectedFormat != declaredFormat)
            {
                return RecipePhotoValidationResult.Failure(
                    RecipePhotoResultCode.InvalidContent);
            }

            string normalizedContentType =
                RecipePhotoFormats.GetContentType(detectedFormat)!;

            return RecipePhotoValidationResult.Success(
                detectedFormat,
                normalizedContentType);
        }
        finally
        {
            content.Position = initialPosition;
        }
    }

    private static int ReadSignature(
        Stream content,
        Span<byte> signature)
    {
        int totalBytesRead = 0;

        while (totalBytesRead < signature.Length)
        {
            int bytesRead = content.Read(signature[totalBytesRead..]);

            if (bytesRead == 0)
            {
                break;
            }

            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }
}

public sealed class RecipePhotoValidationResult
{
    private RecipePhotoValidationResult(
        bool isValid,
        RecipePhotoResultCode code,
        RecipePhotoFormat format,
        string contentType)
    {
        IsValid = isValid;
        Code = code;
        Format = format;
        ContentType = contentType;
    }

    public bool IsValid { get; }

    public RecipePhotoResultCode Code { get; }

    public RecipePhotoFormat Format { get; }

    public string ContentType { get; }

    internal static RecipePhotoValidationResult Success(
        RecipePhotoFormat format,
        string contentType)
    {
        return new RecipePhotoValidationResult(
            true,
            RecipePhotoResultCode.Success,
            format,
            contentType);
    }

    internal static RecipePhotoValidationResult Failure(
        RecipePhotoResultCode code)
    {
        return new RecipePhotoValidationResult(
            false,
            code,
            RecipePhotoFormat.Unknown,
            string.Empty);
    }
}

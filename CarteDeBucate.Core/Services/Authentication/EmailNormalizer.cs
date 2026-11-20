public static class EmailNormalizer
{
    public const int MaximumEmailLength = 254;

    public static bool TryNormalize(string? email, out string normalizedEmail)
    {
        normalizedEmail = string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        string trimmedEmail = email.Trim();

        if (trimmedEmail.Length > MaximumEmailLength)
        {
            return false;
        }

        normalizedEmail = trimmedEmail.ToLowerInvariant();
        return true;
    }

    public static string Normalize(string email)
    {
        if (!TryNormalize(email, out string normalizedEmail))
        {
            throw new ArgumentException(
                $"Email must be non-empty and no longer than {MaximumEmailLength} characters.",
                nameof(email));
        }

        return normalizedEmail;
    }
}

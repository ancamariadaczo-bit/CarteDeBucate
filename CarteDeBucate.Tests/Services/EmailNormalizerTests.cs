public class EmailNormalizerTests
{
    [Fact]
    public void TryNormalize_ShouldTrimAndLowercaseInvariant()
    {
        bool isValid = EmailNormalizer.TryNormalize(
            "  AnCa.Example+Tag@Example.COM  ",
            out string normalizedEmail);

        Assert.True(isValid);
        Assert.Equal("anca.example+tag@example.com", normalizedEmail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalize_WithEmptyValue_ShouldReject(string? email)
    {
        bool isValid = EmailNormalizer.TryNormalize(email, out string normalizedEmail);

        Assert.False(isValid);
        Assert.Equal(string.Empty, normalizedEmail);
    }

    [Fact]
    public void TryNormalize_WithValueLongerThanMaximum_ShouldReject()
    {
        string email = new string('a', EmailNormalizer.MaximumEmailLength + 1);

        bool isValid = EmailNormalizer.TryNormalize(email, out string normalizedEmail);

        Assert.False(isValid);
        Assert.Equal(string.Empty, normalizedEmail);
    }
}

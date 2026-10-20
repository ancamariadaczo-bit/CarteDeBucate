public class UrlValidatorTests
{
    [Theory]
    [InlineData("https://example.com/recipe")]
    [InlineData("http://example.com/recipe")]
    public void IsValidHttpUrl_WithValidHttpUrl_ShouldReturnTrue(string url)
    {
        bool result = UrlValidator.IsValidHttpUrl(url);

        Assert.True(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("www.example.com")]
    [InlineData("example.com")]
    [InlineData("ftp://example.com")]
    [InlineData("mailto:test@example.com")]
    public void IsValidHttpUrl_WithInvalidUrl_ShouldReturnFalse(string url)
    {
        bool result = UrlValidator.IsValidHttpUrl(url);

        Assert.False(result);
    }
}
public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_ShouldCreateVerifiableHashWithExpectedLengths()
    {
        PasswordHasher passwordHasher = new PasswordHasher();

        (string hash, string salt) = passwordHasher.HashPassword("secret-password");

        Assert.Equal(32, Convert.FromBase64String(hash).Length);
        Assert.Equal(16, Convert.FromBase64String(salt).Length);
        Assert.True(passwordHasher.VerifyPassword("secret-password", hash, salt));
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        PasswordHasher passwordHasher = new PasswordHasher();
        (string hash, string salt) = passwordHasher.HashPassword("secret-password");

        bool isValid = passwordHasher.VerifyPassword("wrong-password", hash, salt);

        Assert.False(isValid);
    }
}

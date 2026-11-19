using System.Security.Cryptography;

public class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public (string hash, string salt) HashPassword(string password)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return (
            Convert.ToBase64String(hashBytes),
            Convert.ToBase64String(saltBytes)
        );
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);

        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        byte[] storedHashBytes = Convert.FromBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(hashBytes, storedHashBytes);
    }
}

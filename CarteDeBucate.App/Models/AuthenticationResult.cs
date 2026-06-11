public class AuthenticationResult
{
    public bool IsSuccess { get; set; }

    public string Message { get; set; } = string.Empty;

    public User? User { get; set; }
}
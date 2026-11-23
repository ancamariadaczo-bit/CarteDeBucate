public sealed class ResendOptions
{
    public bool Enabled { get; set; }

    public string ApiToken { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public TimeSpan DeliveryTimeout { get; set; } = TimeSpan.FromSeconds(10);
}

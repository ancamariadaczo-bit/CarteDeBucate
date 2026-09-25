using System.Net;

public sealed class DnsHostAddressResolver : IHostAddressResolver
{
    public Task<IPAddress[]> GetHostAddressesAsync(
        string host,
        CancellationToken cancellationToken)
    {
        return Dns.GetHostAddressesAsync(host, cancellationToken);
    }
}

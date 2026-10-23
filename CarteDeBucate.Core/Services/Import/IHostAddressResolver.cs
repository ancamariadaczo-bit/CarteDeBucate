using System.Net;

public interface IHostAddressResolver
{
    Task<IPAddress[]> GetHostAddressesAsync(
        string host,
        CancellationToken cancellationToken);
}

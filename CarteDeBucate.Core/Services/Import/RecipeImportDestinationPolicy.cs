using System.Net;
using System.Net.Sockets;

public sealed record RecipeImportDestinationValidationResult(
    bool IsAllowed,
    IReadOnlyList<IPAddress> Addresses);

public sealed class RecipeImportDestinationPolicy
{
    private readonly IHostAddressResolver _hostAddressResolver;

    public RecipeImportDestinationPolicy(
        IHostAddressResolver hostAddressResolver)
    {
        _hostAddressResolver = hostAddressResolver;
    }

    public async Task<RecipeImportDestinationValidationResult> ValidateAsync(
        Uri destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!IsAllowedUri(destination))
        {
            return Rejected();
        }

        IPAddress[] addresses = await _hostAddressResolver.GetHostAddressesAsync(
            destination.IdnHost,
            cancellationToken);

        if (addresses.Length == 0 || addresses.Any(address => !IsPublicAddress(address)))
        {
            return Rejected();
        }

        return new RecipeImportDestinationValidationResult(
            true,
            addresses.Distinct().ToArray());
    }

    private static bool IsAllowedUri(Uri destination)
    {
        if (!destination.IsAbsoluteUri || string.IsNullOrWhiteSpace(destination.Host))
        {
            return false;
        }

        bool hasAllowedScheme =
            destination.Scheme == Uri.UriSchemeHttp ||
            destination.Scheme == Uri.UriSchemeHttps;

        return hasAllowedScheme && destination.Port is 80 or 443;
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return false;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            return IsPublicAddress(address.MapToIPv4());
        }

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsPublicIpv4Address(address),
            AddressFamily.InterNetworkV6 => IsPublicIpv6Address(address),
            _ => false
        };
    }

    private static bool IsPublicIpv4Address(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        byte first = bytes[0];
        byte second = bytes[1];
        byte third = bytes[2];

        if (first is 0 or 10 or 127)
        {
            return false;
        }

        if (first == 100 && second is >= 64 and <= 127)
        {
            return false;
        }

        if (first == 169 && second == 254)
        {
            return false;
        }

        if (first == 172 && second is >= 16 and <= 31)
        {
            return false;
        }

        if (first == 192 && second == 0 && third == 0)
        {
            return false;
        }

        if (first == 192 && second == 0 && third == 2)
        {
            return false;
        }

        if (first == 192 && second == 88 && third == 99)
        {
            return false;
        }

        if (first == 192 && second == 168)
        {
            return false;
        }

        if (first == 198 && second is 18 or 19)
        {
            return false;
        }

        if (first == 198 && second == 51 && third == 100)
        {
            return false;
        }

        if (first == 203 && second == 0 && third == 113)
        {
            return false;
        }

        return first < 224;
    }

    private static bool IsPublicIpv6Address(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();

        bool isGlobalUnicast = (bytes[0] & 0xe0) == 0x20;

        if (!isGlobalUnicast)
        {
            return false;
        }

        bool isIetfSpecialPurposeBlock =
            bytes[0] == 0x20 &&
            bytes[1] == 0x01 &&
            bytes[2] <= 0x01;

        bool isDocumentationBlock =
            bytes[0] == 0x20 &&
            bytes[1] == 0x01 &&
            bytes[2] == 0x0d &&
            bytes[3] == 0xb8;

        bool isSixToFour =
            bytes[0] == 0x20 &&
            bytes[1] == 0x02;

        bool isAdditionalDocumentationBlock =
            bytes[0] == 0x3f &&
            bytes[1] == 0xff &&
            (bytes[2] & 0xf0) == 0;

        return !isIetfSpecialPurposeBlock &&
            !isDocumentationBlock &&
            !isSixToFour &&
            !isAdditionalDocumentationBlock;
    }

    private static RecipeImportDestinationValidationResult Rejected()
    {
        return new RecipeImportDestinationValidationResult(
            false,
            Array.Empty<IPAddress>());
    }
}

using System.Net;
using System.Net.Sockets;

public sealed class RecipeImportHttpMessageHandler : DelegatingHandler
{
    private readonly RecipeImportDestinationPolicy _destinationPolicy;

    public RecipeImportHttpMessageHandler(
        RecipeImportDestinationPolicy destinationPolicy)
    {
        _destinationPolicy = destinationPolicy;

        InnerHandler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            ConnectCallback = ConnectAsync
        };
    }

    private async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        Uri? requestUri = context.InitialRequestMessage.RequestUri;

        if (requestUri == null)
        {
            throw CreateRejectedDestinationException();
        }

        IReadOnlyList<IPAddress> approvedAddresses =
            await GetApprovedAddressesAsync(
                context,
                requestUri,
                cancellationToken);

        foreach (IPAddress address in approvedAddresses)
        {
            Socket socket = new Socket(
                address.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp)
            {
                NoDelay = true
            };

            try
            {
                await socket.ConnectAsync(
                    new IPEndPoint(address, context.DnsEndPoint.Port),
                    cancellationToken);

                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException)
            {
                socket.Dispose();
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        throw new HttpRequestException("Recipe import destination could not be reached.");
    }

    private async Task<IReadOnlyList<IPAddress>> GetApprovedAddressesAsync(
        SocketsHttpConnectionContext context,
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        if (RecipeImportRequestOptions.TryGetApprovedDestination(
                context.InitialRequestMessage,
                out ApprovedRecipeImportDestination? approvedDestination) &&
            approvedDestination != null &&
            approvedDestination.Port == context.DnsEndPoint.Port &&
            string.Equals(
                approvedDestination.Host,
                context.DnsEndPoint.Host,
                StringComparison.OrdinalIgnoreCase))
        {
            return approvedDestination.Addresses;
        }

        Uri destination = new UriBuilder(
            requestUri.Scheme,
            context.DnsEndPoint.Host,
            context.DnsEndPoint.Port).Uri;

        RecipeImportDestinationValidationResult validationResult =
            await _destinationPolicy.ValidateAsync(destination, cancellationToken);

        if (!validationResult.IsAllowed)
        {
            throw CreateRejectedDestinationException();
        }

        return validationResult.Addresses;
    }

    private static HttpRequestException CreateRejectedDestinationException()
    {
        return new HttpRequestException("Recipe import destination is not allowed.");
    }
}

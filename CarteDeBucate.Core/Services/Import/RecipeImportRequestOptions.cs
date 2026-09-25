using System.Net;

internal sealed record ApprovedRecipeImportDestination(
    string Host,
    int Port,
    IReadOnlyList<IPAddress> Addresses);

internal static class RecipeImportRequestOptions
{
    private static readonly HttpRequestOptionsKey<ApprovedRecipeImportDestination>
        ApprovedDestinationKey = new("RecipeImport.ApprovedDestination");

    public static void SetApprovedDestination(
        HttpRequestMessage request,
        Uri destination,
        IReadOnlyList<IPAddress> addresses)
    {
        request.Options.Set(
            ApprovedDestinationKey,
            new ApprovedRecipeImportDestination(
                destination.IdnHost,
                destination.Port,
                addresses.ToArray()));
    }

    public static bool TryGetApprovedDestination(
        HttpRequestMessage request,
        out ApprovedRecipeImportDestination? destination)
    {
        return request.Options.TryGetValue(
            ApprovedDestinationKey,
            out destination);
    }
}

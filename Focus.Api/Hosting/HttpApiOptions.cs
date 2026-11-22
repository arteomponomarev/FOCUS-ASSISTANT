namespace Focus.Api.Hosting;

public sealed class HttpApiOptions
{
    public int MaximumPageSize { get; set; } = 100;
    public string[] AllowedOrigins { get; set; } = [];

    internal static bool ValidOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        uri.Scheme is "http" or "https" &&
        origin == uri.GetLeftPart(UriPartial.Authority) &&
        string.IsNullOrEmpty(uri.UserInfo);
}

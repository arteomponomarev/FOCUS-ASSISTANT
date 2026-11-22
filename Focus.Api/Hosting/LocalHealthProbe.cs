namespace Focus.Api.Hosting;

internal static class LocalHealthProbe
{
    public static async Task<bool> IsReadyAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(7) };
        try
        {
            using var response = await client.GetAsync("http://127.0.0.1:8080/health/ready");
            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            return false;
        }
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Focus.Agent.Sync;

internal sealed class AgentApi(HttpClient http, TimeProvider clock) : IAgentEndpoint, IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly SemaphoreSlim _refreshLock = new(1);
    private Credentials? _credentials;

    public static Uri ParseOrigin(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)) ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.AbsolutePath != "/")
            throw new ArgumentException("Use an HTTPS origin or a local loopback HTTP origin.");
        return uri;
    }

    public async Task<Account> SignInAsync(string email, string password, CancellationToken ct)
    {
        using var reply = await http.PostAsJsonAsync("api/v1/auth/login", new { email, password }, Json, ct);
        _credentials = await ReadAsync<Credentials>(reply, ct);
        return await GetAsync<Account>("api/v1/auth/me", ct);
    }

    public async Task<Authority> CheckAsync(Guid session, CancellationToken ct)
    {
        var privacy = await GetAsync<Permission>("api/v1/settings/privacy", ct);
        var through = clock.GetUtcNow();
        if (!privacy.TelemetryEnabled || privacy.ConsentedAt is null)
            return new Authority(privacy, null, through);

        var timeline = await GetAsync<SessionWindow>($"api/v1/sessions/{session:D}/timeline?PageSize=1", ct);
        return new Authority(privacy, timeline, through);
    }

    public async Task<UploadReceipt> UploadAsync(Guid session, IReadOnlyList<Activity> items, CancellationToken ct)
    {
        using var reply = await SendAsync(HttpMethod.Post, $"api/v1/sessions/{session:D}/activity/batch", new { items }, ct);
        return await ReadAsync<UploadReceipt>(reply, ct);
    }

    public async Task SignOutAsync(CancellationToken ct)
    {
        if (_credentials is null)
            return;

        try
        {
            using var reply = await SendAsync(HttpMethod.Post, "api/v1/auth/logout", null, ct);
            EnsureSuccess(reply);
        }
        finally
        {
            _credentials = null;
        }
    }

    private async Task<T> GetAsync<T>(string route, CancellationToken ct)
    {
        using var reply = await SendAsync(HttpMethod.Get, route, null, ct);
        return await ReadAsync<T>(reply, ct);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string route, object? body, CancellationToken ct)
    {
        var credentials = _credentials ?? throw new InvalidOperationException("Sign in before accessing the API.");
        var first = await SendWithTokenAsync(method, route, body, credentials.AccessToken, ct);
        if (first.StatusCode != HttpStatusCode.Unauthorized)
            return first;

        first.Dispose();
        await _refreshLock.WaitAsync(ct);
        try
        {
            if (ReferenceEquals(_credentials, credentials))
            {
                using var reply = await http.PostAsJsonAsync("api/v1/auth/refresh",
                    new { refreshToken = credentials.RefreshToken }, Json, ct);
                _credentials = await ReadAsync<Credentials>(reply, ct);
            }

            credentials = _credentials ?? throw new InvalidOperationException("The Agent login ended.");
        }
        finally
        {
            _refreshLock.Release();
        }

        return await SendWithTokenAsync(method, route, body, credentials.AccessToken, ct);
    }

    private async Task<HttpResponseMessage> SendWithTokenAsync(HttpMethod method, string route, object? body,
        string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, route);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: Json);

        return await http.SendAsync(request, ct);
    }

    private async Task<T> ReadAsync<T>(HttpResponseMessage reply, CancellationToken ct)
    {
        EnsureSuccess(reply);
        return await reply.Content.ReadFromJsonAsync<T>(Json, ct)
            ?? throw new JsonException("The API returned an empty response.");
    }

    private void EnsureSuccess(HttpResponseMessage reply)
    {
        if (reply.IsSuccessStatusCode)
            return;

        var retry = reply.Headers.RetryAfter;
        throw new RemoteFailure(reply.StatusCode, retry?.Delta ??
            (retry?.Date is { } date ? date - clock.GetUtcNow() : null));
    }

    public void Dispose() => _refreshLock.Dispose();
}

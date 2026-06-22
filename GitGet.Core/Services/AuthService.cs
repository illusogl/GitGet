using System.Net.Http.Json;
using System.Text.Json;
using GitGet.Core.Interfaces;

namespace GitGet.Core.Services;

public class AuthService : IAuthService
{
    private const string ClientId = "Ov23liZeGHY2vsaN4gyq";
    private const string TokenKey = "github_oauth_token";

    private readonly HttpClient _http;
    private readonly ISecureDataStore _secureStore;
    private string? _accessToken;

    public bool IsLoggedIn => !string.IsNullOrEmpty(_accessToken);
    public string? AccessToken => _accessToken;

    public AuthService(HttpClient http, ISecureDataStore secureStore)
    {
        _http = http;
        _secureStore = secureStore;
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
        _http.DefaultRequestHeaders.Add("User-Agent", "GitGet/1.0");
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<DeviceCodeResponse> RequestDeviceCodeAsync(CancellationToken ct = default)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["scope"] = "user repo"
        });

        var response = await _http.PostAsync("https://github.com/login/device/code", content, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        return new DeviceCodeResponse
        {
            DeviceCode = GetString(root, "device_code"),
            UserCode = GetString(root, "user_code"),
            VerificationUri = GetString(root, "verification_uri"),
            ExpiresIn = GetInt(root, "expires_in"),
            Interval = GetInt(root, "interval"),
        };
    }

    public async Task<(DeviceFlowStatus Status, AccessTokenResponse? Token)> PollForTokenAsync(
        string deviceCode, int intervalSeconds, CancellationToken ct = default)
    {
        await Task.Delay(intervalSeconds * 1000, ct);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["device_code"] = deviceCode,
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
        });

        var response = await _http.PostAsync("https://github.com/login/oauth/access_token", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var errorProp))
        {
            var error = errorProp.GetString() ?? "";
            var desc = GetString(root, "error_description");

            return error switch
            {
                "authorization_pending" => (DeviceFlowStatus.Pending, null),
                "slow_down" => (DeviceFlowStatus.Pending, null),
                "expired_token" => (DeviceFlowStatus.Expired, null),
                "access_denied" => (DeviceFlowStatus.Denied, null),
                _ => (DeviceFlowStatus.Error, new AccessTokenResponse
                {
                    Error = error,
                    ErrorDescription = desc
                })
            };
        }

        // Success
        var token = GetString(root, "access_token");
        _accessToken = token;
        await _secureStore.SaveTokenAsync(TokenKey, token);

        var result = new AccessTokenResponse
        {
            AccessToken = token,
            TokenType = GetString(root, "token_type"),
            Scope = GetString(root, "scope"),
        };

        return (DeviceFlowStatus.Authorized, result);
    }

    public async Task<bool> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await _http.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> TryRestoreTokenAsync(CancellationToken ct = default)
    {
        var token = await _secureStore.GetTokenAsync(TokenKey, ct);
        if (string.IsNullOrEmpty(token))
            return false;

        _accessToken = token;
        return true;
    }

    public void SetToken(string token)
    {
        _accessToken = token;
    }

    public void Logout()
    {
        _accessToken = null;
        _secureStore.ClearTokenAsync(TokenKey).GetAwaiter().GetResult();
    }

    private static string GetString(JsonElement el, string name)
        => el.TryGetProperty(name, out var p) ? p.GetString() ?? "" : "";

    private static int GetInt(JsonElement el, string name)
        => el.TryGetProperty(name, out var p) && p.TryGetInt32(out var v) ? v : 0;
}
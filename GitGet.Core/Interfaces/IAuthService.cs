namespace GitGet.Core.Interfaces;

public class DeviceCodeResponse
{
    public string DeviceCode { get; set; } = "";
    public string UserCode { get; set; } = "";
    public string VerificationUri { get; set; } = "";
    public int ExpiresIn { get; set; }
    public int Interval { get; set; }
}

public class AccessTokenResponse
{
    public string AccessToken { get; set; } = "";
    public string TokenType { get; set; } = "";
    public string Scope { get; set; } = "";
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
}

public enum DeviceFlowStatus
{
    Pending,
    Authorized,
    Expired,
    Denied,
    Error
}

public interface IAuthService
{
    bool IsLoggedIn { get; }
    string? AccessToken { get; }
    Task<DeviceCodeResponse> RequestDeviceCodeAsync(CancellationToken ct = default);
    Task<(DeviceFlowStatus Status, AccessTokenResponse? Token)> PollForTokenAsync(string deviceCode, int intervalSeconds, CancellationToken ct = default);
    Task<bool> ValidateTokenAsync(string token, CancellationToken ct = default);
    Task<bool> TryRestoreTokenAsync(CancellationToken ct = default);
    void SetToken(string token);
    void Logout();
}
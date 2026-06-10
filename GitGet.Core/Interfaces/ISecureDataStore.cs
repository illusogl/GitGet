namespace GitGet.Core.Interfaces;

public interface ISecureDataStore
{
    Task SaveTokenAsync(string key, string token, CancellationToken ct = default);
    Task<string?> GetTokenAsync(string key, CancellationToken ct = default);
    Task ClearTokenAsync(string key, CancellationToken ct = default);
}
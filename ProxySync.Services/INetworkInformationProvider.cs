namespace ProxySync.Services;

public sealed record NetworkSnapshot(string? LocalIpAddress, string? DefaultGatewayAddress);

public interface INetworkInformationProvider
{
    Task<NetworkSnapshot> GetSnapshotAsync();
}
using ProxySync.Core.Models;

namespace ProxySync.Services;

public interface INetworkDetectionService
{
    Task<string?> GetCurrentNetworkIdentifierAsync();

    Task<ProxyProfile?> DetectMatchingProfileAsync();
}
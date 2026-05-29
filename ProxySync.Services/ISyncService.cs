using ProxySync.Core.Models;

namespace ProxySync.Services;

public interface ISyncService
{
    Task ApplyAllAsync(ProxyConfig config);

    Task DisableAllAsync();
}
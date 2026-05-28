using ProxySync.Core.Models;
using ProxySync.Core.Helpers;

namespace ProxySync.Services;

public class EnvProxyService
{
    private readonly ProxySync.Services.SystemEnvironment.EnvironmentProxyService _environmentProxyService =
        new ProxySync.Services.SystemEnvironment.EnvironmentProxyService();

    public async Task ApplyAsync(ProxyConfig config)
    {
        string proxyUrl = ProxyFormatter.ToUrl(config);

        _environmentProxyService.Apply(proxyUrl);

        await Task.CompletedTask;
    }

    public async Task DisableAsync()
    {
        _environmentProxyService.Disable();

        await Task.CompletedTask;
    }
}
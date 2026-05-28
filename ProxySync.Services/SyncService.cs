using ProxySync.Core.Models;

namespace ProxySync.Services;

public class SyncService
{
    private readonly EnvProxyService _envService;
    private readonly GitProxyService _gitService;
    private readonly NpmProxyService _npmService;

    public SyncService(
        EnvProxyService envService,
        GitProxyService gitService,
        NpmProxyService npmService)
    {
        _envService = envService;
        _gitService = gitService;
        _npmService = npmService;
    }

    public async Task ApplyAllAsync(ProxyConfig config)
    {
        Console.WriteLine("Starting proxy sync...");

        await _envService.ApplyAsync(config);
        await _gitService.ApplyAsync(config);
        await _npmService.ApplyAsync(config);

        Console.WriteLine("Proxy sync completed.");
    }

    public async Task DisableAllAsync()
    {
        Console.WriteLine("Disabling all proxy settings...");

        await _envService.DisableAsync();
        await _gitService.DisableAsync();
        await _npmService.DisableAsync();

        Console.WriteLine("All proxy settings disabled.");
    }
}
using ProxySync.Core.Models;

namespace ProxySync.Services;

public class GitProxyService
{
    private readonly ICommandRunner _runner;

    public GitProxyService(ICommandRunner runner)
    {
        _runner = runner;
    }

    public async Task ApplyAsync(ProxyConfig config)
    {
        string proxyUrl = BuildProxyUrl(config);

        await _runner.RunAsync(
            "git",
            $"config --global http.proxy {proxyUrl}");

        await _runner.RunAsync(
            "git",
            $"config --global https.proxy {proxyUrl}");

        Console.WriteLine("Git proxy applied.");
    }

    public async Task DisableAsync()
    {
        await _runner.RunAsync(
            "git",
            "config --global --unset http.proxy");

        await _runner.RunAsync(
            "git",
            "config --global --unset https.proxy");

        Console.WriteLine("Git proxy disabled.");
    }

    private string BuildProxyUrl(ProxyConfig config)
    {
        return $"http://{config.Host}:{config.Port}";
    }
}
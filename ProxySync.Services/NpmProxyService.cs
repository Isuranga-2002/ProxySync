using ProxySync.Core.Models;

namespace ProxySync.Services;

public class NpmProxyService
{
    private readonly ICommandRunner _runner;

    public NpmProxyService(ICommandRunner runner)
    {
        _runner = runner;
    }

    public async Task ApplyAsync(ProxyConfig config)
    {
        string proxyUrl = BuildProxyUrl(config);

        var result1 = await _runner.RunAsync(
            "npm.cmd",
            $"config set proxy {proxyUrl}");

        var result2 = await _runner.RunAsync(
            "npm.cmd",
            $"config set https-proxy {proxyUrl}");

        if (result1 == 0 && result2 == 0)
        {
            Console.WriteLine("npm proxy applied.");
        }
        else
        {
            Console.WriteLine("Failed to apply npm proxy.");
        }
    }

    public async Task DisableAsync()
    {
        await _runner.RunAsync(
            "npm.cmd",
            "config delete proxy");

        await _runner.RunAsync(
            "npm.cmd",
            "config delete https-proxy");

        Console.WriteLine("npm proxy disabled.");
    }

    private string BuildProxyUrl(ProxyConfig config)
    {
        return $"http://{config.Host}:{config.Port}";
    }
}
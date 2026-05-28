using ProxySync.Core.Models;

namespace ProxySync.Services;

public class EnvProxyService
{
    public async Task ApplyAsync(ProxyConfig config)
    {
        string proxyUrl = BuildProxyUrl(config);

        Environment.SetEnvironmentVariable(
            "HTTP_PROXY",
            proxyUrl,
            EnvironmentVariableTarget.User);

        Environment.SetEnvironmentVariable(
            "HTTPS_PROXY",
            proxyUrl,
            EnvironmentVariableTarget.User);

        Environment.SetEnvironmentVariable(
            "ALL_PROXY",
            proxyUrl,
            EnvironmentVariableTarget.User);

        Console.WriteLine("Environment proxy applied.");

        await Task.CompletedTask;
    }

    public async Task DisableAsync()
    {
        Environment.SetEnvironmentVariable(
            "HTTP_PROXY",
            null,
            EnvironmentVariableTarget.User);

        Environment.SetEnvironmentVariable(
            "HTTPS_PROXY",
            null,
            EnvironmentVariableTarget.User);

        Environment.SetEnvironmentVariable(
            "ALL_PROXY",
            null,
            EnvironmentVariableTarget.User);

        Console.WriteLine("Environment proxy disabled.");

        await Task.CompletedTask;
    }

    private string BuildProxyUrl(ProxyConfig config)
    {
        return $"http://{config.Host}:{config.Port}";
    }
}
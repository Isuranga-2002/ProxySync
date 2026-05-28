using ProxySync.Core.Models;
using ProxySync.Core.Helpers;

namespace ProxySync.Services;

public class EnvProxyService
{
    public async Task ApplyAsync(ProxyConfig config)
    {
        string proxyUrl = ProxyFormatter.ToUrl(config);

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
}
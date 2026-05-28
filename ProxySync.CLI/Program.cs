using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ProxySync.Core.Models;
using ProxySync.Services;

// 1. Handle basic validations for args
if (args.Length == 0)
{
    Console.WriteLine("Usage: proxysync [sync|disable]");
    return;
}

string command = args[0].ToLowerInvariant();

// 2. Create and initialize all required services
var services = new ServiceCollection();
services.AddSingleton<ICommandRunner, CommandRunner>();
services.AddSingleton<GitProxyService>();
services.AddSingleton<NpmProxyService>();
services.AddSingleton<EnvProxyService>();
services.AddSingleton<SyncService>();
services.AddSingleton<ConfigService>();

var serviceProvider = services.BuildServiceProvider();
var syncService = serviceProvider.GetRequiredService<SyncService>();

// 3. Load the proxy configuration
var configService = serviceProvider.GetRequiredService<ConfigService>();
var config = configService.Load();

if (config == null)
{
    Console.WriteLine("No proxy configuration found. Please configure a proxy first.");
    return;
}

// 4. Handle commands properly using async/await
try
{
    switch (command)
    {
        case "sync":
            Console.WriteLine("Applying proxy settings...");
            await syncService.ApplyAllAsync(config);
            break;

        case "disable":
            Console.WriteLine("Disabling proxy settings...");
            await syncService.DisableAllAsync();
            break;

        default:
            Console.WriteLine($"Unknown command: {command}");
            Console.WriteLine("Supported commands: sync, disable");
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}

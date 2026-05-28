using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ProxySync.Core.Models;
using ProxySync.Services;

// 1. Handle basic validations for args
if (args.Length == 0)
{
    Console.WriteLine("Usage: proxysync [sync|disable|set]");
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

// 3. Handle commands properly using async/await
try
{
    var syncService = serviceProvider.GetRequiredService<SyncService>();

    switch (command)
    {
        case "set":
            var setConfigService = serviceProvider.GetRequiredService<ConfigService>();
            
            Console.Write("Host: ");
            var host = Console.ReadLine() ?? string.Empty;

            Console.Write("Port: ");
            int.TryParse(Console.ReadLine(), out int port);

            Console.Write("Username (optional): ");
            var username = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(username)) username = null;

            Console.Write("Password (optional): ");
            var password = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(password)) password = null;

            var newConfig = new ProxyConfig
            {
                Host = host,
                Port = port,
                Username = username,
                Password = password
            };

            setConfigService.Save(newConfig);
            Console.WriteLine("Proxy configuration saved successfully.");
            break;

        case "sync":
            var configService = serviceProvider.GetRequiredService<ConfigService>();
            var config = configService.Load();
            if (config == null)
            {
                Console.WriteLine("No proxy configuration found. Please configure a proxy first.");
                return;
            }
            
            Console.WriteLine("Applying proxy settings...");
            await syncService.ApplyAllAsync(config);
            break;

        case "disable":
            Console.WriteLine("Disabling proxy settings...");
            await syncService.DisableAllAsync();
            break;

        default:
            Console.WriteLine($"Unknown command: {command}");
            Console.WriteLine("Supported commands: sync, disable, set");
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
    Environment.ExitCode = 1;
}

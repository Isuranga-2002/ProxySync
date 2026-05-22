using ProxySync.Core.Models;
using ProxySync.Services;
using ProxySync.Services.SystemEnvironment;
using ProxySync.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddSingleton<ConfigService>();

services.AddSingleton<EnvironmentProxyService>();

services.AddSingleton<ICommandRunner, CommandRunner>();

services.AddSingleton<GitProxyService>();

services.AddSingleton<NpmProxyService>();

var serviceProvider = services.BuildServiceProvider();

var configService =
    serviceProvider.GetRequiredService<ConfigService>();

var envService =
    serviceProvider.GetRequiredService<EnvironmentProxyService>();

if (args.Length > 0 && args[0] == "set")
{
    Console.Write("Host: ");
    var host = Console.ReadLine() ?? string.Empty;

    Console.Write("Port: ");
    var port = int.Parse(Console.ReadLine() ?? "0");

    Console.Write("Username (optional): ");
    var username = Console.ReadLine();

    Console.Write("Password (optional): ");
    var password = Console.ReadLine();

    var config = new ProxyConfig
    {
        Host = host,
        Port = port,
        Username = username,
        Password = password
    };

    configService.Save(config);

    Console.WriteLine("Proxy saved successfully!");
}

else if (args.Length > 0 && args[0] == "show")
{
    var config = configService.Load();

    if (config == null)
    {
        Console.WriteLine("No proxy configured.");
        return;
    }

    Console.WriteLine("Current Proxy:");
    Console.WriteLine($"Host: {config.Host}");
    Console.WriteLine($"Port: {config.Port}");
    Console.WriteLine($"Username: {config.Username}");
}

else if (args.Length >= 2 &&
         args[0] == "apply" &&
         args[1] == "env")
{
    var config = configService.Load();

    if (config == null)
    {
        Console.WriteLine("No proxy configured.");
        return;
    }

    var proxyUrl = ProxyFormatter.ToUrl(config);

    envService.Apply(proxyUrl);
}

else if (args.Length >= 2 &&
         args[0] == "disable" &&
         args[1] == "env")
{
    envService.Disable();
}

else if (args.Length >= 2 &&
         args[0] == "apply" &&
         args[1] == "git")
{
    var config = configService.Load();

    if (config == null)
    {
        Console.WriteLine("No proxy configured.");
        return;
    }

    var gitService =
        serviceProvider.GetRequiredService<GitProxyService>();

    await gitService.ApplyAsync(config);
}

else if (args.Length >= 2 &&
         args[0] == "disable" &&
         args[1] == "git")
{
    var gitService =
        serviceProvider.GetRequiredService<GitProxyService>();

    await gitService.DisableAsync();
}

else if (args.Length >= 2 &&
         args[0] == "apply" &&
         args[1] == "npm")
{
    var config = configService.Load();

    if (config == null)
    {
        Console.WriteLine("No proxy configured.");
        return;
    }

    var npmService =
        serviceProvider.GetRequiredService<NpmProxyService>();

    await npmService.ApplyAsync(config);
}

else if (args.Length >= 2 &&
         args[0] == "disable" &&
         args[1] == "npm")
{
    var npmService =
        serviceProvider.GetRequiredService<NpmProxyService>();

    await npmService.DisableAsync();
}

else
{
    Console.WriteLine("Unknown command.");
}
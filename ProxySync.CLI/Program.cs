
using ProxySync.Core.Models;
using ProxySync.Services;

var configService = new ConfigService();

if (args.Length > 0 && args[0] == "set")
{
    Console.Write("Host: ");
    var host = Console.ReadLine();

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
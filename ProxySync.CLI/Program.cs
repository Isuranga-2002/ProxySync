using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ProxySync.Core.Models;
using ProxySync.Services;

// 1. Handle basic validations for args
if (args.Length == 0)
{
    Console.WriteLine("Usage: proxysync [sync|disable|set|profile]");
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
services.AddSingleton<ProfileService>();

var serviceProvider = services.BuildServiceProvider();

// 3. Handle commands properly using async/await
try
{
    var syncService = serviceProvider.GetRequiredService<SyncService>();

    // Local helpers to reduce duplicated console parsing logic
    static string ReadHostFromConsole()
    {
        Console.Write("Host: ");
        return Console.ReadLine() ?? string.Empty;
    }

    static int? ReadPortFromConsole()
    {
        Console.Write("Port: ");
        var portInputLocal = Console.ReadLine();
        if (!int.TryParse(portInputLocal, out int p)) return null;
        return p;
    }

    switch (command)
    {
        case "profile":
        {
            // profile subcommands: add <name>, list, switch <name>
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: proxysync profile [add|list|switch] [name]");
                return;
            }

            var profileService = serviceProvider.GetRequiredService<ProfileService>();
            var sub = args[1].ToLowerInvariant();

            switch (sub)
            {
                case "add":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Usage: proxysync profile add <name>");
                        return;
                    }

                    var name = args[2];

                    var hostInput = ReadHostFromConsole();
                    var portProfileNullable = ReadPortFromConsole();
                    if (!portProfileNullable.HasValue || portProfileNullable.Value <= 0)
                    {
                        Console.WriteLine("Invalid port. Please enter a positive integer.");
                        Environment.ExitCode = 1;
                        return;
                    }
                    var portProfile = portProfileNullable.Value;

                    // Validate inputs before attempting to add
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        Console.WriteLine("Profile name is required.");
                        return;
                    }

                    if (name.IndexOfAny(new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }) >= 0)
                    {
                        Console.WriteLine("Profile name contains invalid characters.");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(hostInput))
                    {
                        Console.WriteLine("Host is required.");
                        return;
                    }

                    if (portProfile <= 0 || portProfile > 65535)
                    {
                        Console.WriteLine("Port must be between 1 and 65535.");
                        return;
                    }

                    var newProfile = new ProxyProfile
                    {
                        Name = name,
                        Host = hostInput,
                        Port = portProfile
                    };

                    try
                    {
                        await profileService.AddProfileAsync(newProfile);
                        Console.WriteLine($"Profile '{name}' added.");
                    }
                    catch (ArgumentException ae)
                    {
                        Console.WriteLine($"Failed to add profile: {ae.Message}");
                    }
                    break;

                case "list":
                    try
                    {
                        var profiles = (await profileService.ListProfilesAsync()).ToList();
                        var doc = await profileService.LoadAsync();
                        var activeName = doc.ActiveProfile;

                        Console.WriteLine("Profiles:");
                        if (!profiles.Any())
                        {
                            Console.WriteLine("  (no profiles)");
                        }
                        else
                        {
                            foreach (var p in profiles)
                            {
                                if (!string.IsNullOrEmpty(activeName) && string.Equals(p.Name, activeName, StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.WriteLine($"* {p.Name}");
                                }
                                else
                                {
                                    Console.WriteLine($"  {p.Name}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to list profiles: {ex.Message}");
                    }
                    break;

                case "switch":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Usage: proxysync profile switch <name>");
                        return;
                    }

                    var switchName = args[2];
                    try
                    {
                        var ok = await profileService.SwitchActiveProfileAsync(switchName);
                        if (ok)
                            Console.WriteLine($"Active profile set to '{switchName}'.");
                        else
                            Console.WriteLine($"Profile '{switchName}' not found.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to switch profile: {ex.Message}");
                    }

                    break;

                default:
                    Console.WriteLine("Unknown profile command. Supported: add, list, switch");
                    break;
            }

            break;
        }
        case "set":
            var setConfigService = serviceProvider.GetRequiredService<ConfigService>();

            var host = ReadHostFromConsole();
            var portNullable = ReadPortFromConsole();
            if (!portNullable.HasValue || portNullable.Value <= 0)
            {
                Console.WriteLine("Invalid port. Please enter a positive integer.");
                Environment.ExitCode = 1;
                return;
            }
            var port = portNullable.Value;

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
            var profileServiceForSync = serviceProvider.GetRequiredService<ProfileService>();
            ProxyConfig configToUse = null;
            try
            {
                var activeProfile = await profileServiceForSync.GetActiveProfileAsync();
                if (activeProfile != null)
                {
                    configToUse = new ProxyConfig
                    {
                        Host = activeProfile.Host,
                        Port = activeProfile.Port,
                        Username = null,
                        Password = null
                    };

                    Console.WriteLine($"Applying proxy settings from profile '{activeProfile.Name}'...");
                }
                else
                {
                    // Fallback to legacy config.json
                    var configService = serviceProvider.GetRequiredService<ConfigService>();
                    var legacyConfig = configService.Load();
                    if (legacyConfig != null)
                    {
                        Console.WriteLine("No active profile found. Using legacy configuration from config.json...");
                        configToUse = legacyConfig;
                    }
                    else
                    {
                        Console.WriteLine("No active profile found and no legacy configuration present. Please add a profile or run 'set' to create legacy config.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load profile configuration: {ex.Message}");
                return;
            }

            await syncService.ApplyAllAsync(configToUse);
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

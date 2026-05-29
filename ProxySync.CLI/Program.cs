using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ProxySync.Core.Models;
using ProxySync.Services;

// 1. Handle basic validations for args
static void PrintUsage()
{
    Console.WriteLine("Usage: proxysync [on|off|sync|disable|set|profile|detect|auto-switch]");
    Console.WriteLine("Profile commands: profile add <name> | profile list | profile switch <name>");
    Console.WriteLine("Automation commands: on | off | detect | auto-switch");
}

if (args.Length == 0)
{
    PrintUsage();
    return;
}

string command = args[0].ToLowerInvariant();

// 2. Create and initialize all required services
var services = new ServiceCollection();
services.AddSingleton<ICommandRunner, CommandRunner>();
services.AddSingleton<ISyncService, SyncService>();
services.AddSingleton<GitProxyService>();
services.AddSingleton<NpmProxyService>();
services.AddSingleton<EnvProxyService>();
services.AddSingleton<IProfileService, ProfileService>();
services.AddSingleton<INetworkInformationProvider, SystemNetworkInformationProvider>();
services.AddSingleton<INetworkDetectionService, NetworkDetectionService>();
services.AddSingleton<IAutomationService, AutomationService>();
services.AddSingleton<ConfigService>();

var serviceProvider = services.BuildServiceProvider();

// 3. Handle commands properly using async/await
try
{
    var syncService = serviceProvider.GetRequiredService<ISyncService>();
    var profileService = serviceProvider.GetRequiredService<IProfileService>();
    var automationService = serviceProvider.GetRequiredService<IAutomationService>();

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

                    Console.Write("Network identifier (optional): ");
                    var networkIdentifier = Console.ReadLine();

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
                        Port = portProfile,
                        NetworkIdentifier = string.IsNullOrWhiteSpace(networkIdentifier) ? null : networkIdentifier.Trim()
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
        case "on":
        {
            try
            {
                var result = await automationService.EnableAsync();
                Console.WriteLine(result.Message);
                if (!result.Success)
                    Environment.ExitCode = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to enable proxy: {ex.Message}");
                Environment.ExitCode = 1;
            }

            break;
        }
        case "off":
        {
            try
            {
                var result = await automationService.DisableAsync();
                Console.WriteLine(result.Message);
                if (!result.Success)
                    Environment.ExitCode = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to disable proxy: {ex.Message}");
                Environment.ExitCode = 1;
            }

            break;
        }
        case "detect":
        {
            try
            {
                var result = await automationService.DetectAsync();
                Console.WriteLine(result.Message);
                if (!result.Success)
                    Environment.ExitCode = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to detect network: {ex.Message}");
                Environment.ExitCode = 1;
            }

            break;
        }
        case "auto-switch":
        {
            try
            {
                var result = await automationService.AutoSwitchAsync();
                Console.WriteLine(result.Message);
                if (!result.Success)
                    Environment.ExitCode = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to auto-switch profile: {ex.Message}");
                Environment.ExitCode = 1;
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
            ProxyConfig? configToUse = null;
            try
            {
                var activeProfile = await profileService.GetActiveProfileAsync();
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
            PrintUsage();
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
    Environment.ExitCode = 1;
}

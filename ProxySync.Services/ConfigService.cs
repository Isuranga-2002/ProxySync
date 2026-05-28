using System.Text.Json;
using ProxySync.Core.Models;

namespace ProxySync.Services
{
    public class ConfigService
    {
        private readonly string configPath;

        public ConfigService()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".proxysync"
            );

            Directory.CreateDirectory(dir);
            configPath = Path.Combine(dir, "config.json");
        }

        public void Save(ProxyConfig config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(configPath, json);
        }

        public ProxyConfig? Load()
        {
            if (!File.Exists(configPath))
                return null;

            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<ProxyConfig>(json);
        }
    }
}
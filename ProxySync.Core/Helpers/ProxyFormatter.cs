using ProxySync.Core.Models;

namespace ProxySync.Core.Helpers
{
    public static class ProxyFormatter
    {
        public static string ToUrl(ProxyConfig config)
        {
            if (!string.IsNullOrEmpty(config.Username))
            {
                return $"http://{config.Username}:{config.Password}@{config.Host}:{config.Port}";
            }

            return $"http://{config.Host}:{config.Port}";
        }
    }
}
namespace ProxySync.Core.Models
{
    public class ProxyConfig
    {
        public required string Host { get; set; }
        public required int Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }

        public string ToProxyUrl()
        {
            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
                return $"http://{Username}:{Password}@{Host}:{Port}";

            return $"http://{Host}:{Port}";
        }
    }
}
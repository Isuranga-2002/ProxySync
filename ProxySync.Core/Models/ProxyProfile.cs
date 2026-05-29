namespace ProxySync.Core.Models
{
    public class ProxyProfile
    {
        public required string Name { get; set; }

        public required string Host { get; set; }

        public required int Port { get; set; }

        public string? NetworkIdentifier { get; set; }
    }
}

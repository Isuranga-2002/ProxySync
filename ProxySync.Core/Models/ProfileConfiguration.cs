using System.Collections.Generic;

namespace ProxySync.Core.Models
{
    public class ProfileConfiguration
    {
        public string? ActiveProfile { get; set; }

        public required Dictionary<string, ProxyProfile> Profiles { get; set; } = new Dictionary<string, ProxyProfile>();
    }
}

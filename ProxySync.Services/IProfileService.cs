using ProxySync.Core.Models;

namespace ProxySync.Services;

public interface IProfileService
{
    Task<ProfileConfiguration> LoadAsync();

    Task SaveAsync(ProfileConfiguration configuration);

    Task AddProfileAsync(ProxyProfile profile);

    Task<IEnumerable<ProxyProfile>> ListProfilesAsync();

    Task<bool> SwitchActiveProfileAsync(string name);

    Task<ProxyProfile?> GetActiveProfileAsync();
}
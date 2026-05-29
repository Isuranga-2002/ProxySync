using ProxySync.Core.Models;
using ProxySync.Services;

namespace ProxySync.Tests;

internal sealed class FakeProfileService : IProfileService
{
    private readonly ProfileConfiguration _configuration;

    public FakeProfileService(ProfileConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<ProfileConfiguration> LoadAsync() => Task.FromResult(_configuration);

    public Task SaveAsync(ProfileConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    public Task AddProfileAsync(ProxyProfile profile)
    {
        _configuration.Profiles[profile.Name] = profile;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ProxyProfile>> ListProfilesAsync() => Task.FromResult<IEnumerable<ProxyProfile>>(_configuration.Profiles.Values.ToList());

    public Task<bool> SwitchActiveProfileAsync(string name)
    {
        if (!_configuration.Profiles.ContainsKey(name))
            return Task.FromResult(false);

        _configuration.ActiveProfile = name;
        return Task.FromResult(true);
    }

    public Task<ProxyProfile?> GetActiveProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(_configuration.ActiveProfile))
            return Task.FromResult<ProxyProfile?>(null);

        _configuration.Profiles.TryGetValue(_configuration.ActiveProfile, out var profile);
        return Task.FromResult(profile);
    }
}

internal sealed class FakeSyncService : ISyncService
{
    public List<ProxyConfig> AppliedConfigs { get; } = new();

    public int DisableCalls { get; private set; }

    public Task ApplyAllAsync(ProxyConfig config)
    {
        AppliedConfigs.Add(config);
        return Task.CompletedTask;
    }

    public Task DisableAllAsync()
    {
        DisableCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeNetworkInformationProvider : INetworkInformationProvider
{
    private readonly NetworkSnapshot _snapshot;

    public FakeNetworkInformationProvider(NetworkSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public Task<NetworkSnapshot> GetSnapshotAsync() => Task.FromResult(_snapshot);
}
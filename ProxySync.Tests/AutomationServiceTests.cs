using ProxySync.Core.Models;
using ProxySync.Services;
using Xunit;

namespace ProxySync.Tests;

public class AutomationServiceTests
{
    [Fact]
    public async Task EnableAsync_UsesActiveProfile()
    {
        var profileConfiguration = new ProfileConfiguration
        {
            ActiveProfile = "university",
            Profiles = new Dictionary<string, ProxyProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["university"] = new ProxyProfile { Name = "university", Host = "proxy.example.com", Port = 3128, NetworkIdentifier = "10.0.0." }
            }
        };

        var profileService = new FakeProfileService(profileConfiguration);
        var syncService = new FakeSyncService();
        var networkDetectionService = new NetworkDetectionService(profileService, new FakeNetworkInformationProvider(new NetworkSnapshot("10.0.0.25", "10.0.0.1")));
        var automationService = new AutomationService(profileService, syncService, networkDetectionService);

        var result = await automationService.EnableAsync();

        Assert.True(result.Success);
        Assert.Equal("university", result.ProfileName);
        Assert.Single(syncService.AppliedConfigs);
        Assert.Equal("proxy.example.com", syncService.AppliedConfigs[0].Host);
        Assert.Equal(3128, syncService.AppliedConfigs[0].Port);
    }

    [Fact]
    public async Task DisableAsync_DisablesProxySettings()
    {
        var profileService = new FakeProfileService(new ProfileConfiguration());
        var syncService = new FakeSyncService();
        var automationService = new AutomationService(profileService, syncService, new NetworkDetectionService(profileService, new FakeNetworkInformationProvider(new NetworkSnapshot(null, null))));

        var result = await automationService.DisableAsync();

        Assert.True(result.Success);
        Assert.Equal(1, syncService.DisableCalls);
    }

    [Fact]
    public async Task DetectAsync_ReturnsSuggestedProfile()
    {
        var profileConfiguration = new ProfileConfiguration
        {
            Profiles = new Dictionary<string, ProxyProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["university"] = new ProxyProfile { Name = "university", Host = "proxy.example.com", Port = 3128, NetworkIdentifier = "10.0.0." }
            }
        };

        var profileService = new FakeProfileService(profileConfiguration);
        var networkDetectionService = new NetworkDetectionService(profileService, new FakeNetworkInformationProvider(new NetworkSnapshot("10.0.0.25", "10.0.0.1")));
        var automationService = new AutomationService(profileService, new FakeSyncService(), networkDetectionService);

        var result = await automationService.DetectAsync();

        Assert.True(result.Success);
        Assert.Equal("university", result.ProfileName);
        Assert.Contains("Suggested profile", result.Message);
    }

    [Fact]
    public async Task AutoSwitchAsync_SwitchesAndAppliesMatchingProfile()
    {
        var profileConfiguration = new ProfileConfiguration
        {
            ActiveProfile = "home",
            Profiles = new Dictionary<string, ProxyProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["home"] = new ProxyProfile { Name = "home", Host = "home-proxy", Port = 8080, NetworkIdentifier = "192.168.1." },
                ["university"] = new ProxyProfile { Name = "university", Host = "proxy.example.com", Port = 3128, NetworkIdentifier = "10.0.0." }
            }
        };

        var profileService = new FakeProfileService(profileConfiguration);
        var syncService = new FakeSyncService();
        var networkDetectionService = new NetworkDetectionService(profileService, new FakeNetworkInformationProvider(new NetworkSnapshot("10.0.0.99", "10.0.0.1")));
        var automationService = new AutomationService(profileService, syncService, networkDetectionService);

        var result = await automationService.AutoSwitchAsync();

        Assert.True(result.Success);
        Assert.Equal("university", result.ProfileName);
        Assert.Equal("university", profileConfiguration.ActiveProfile);
        Assert.Single(syncService.AppliedConfigs);
        Assert.Equal("proxy.example.com", syncService.AppliedConfigs[0].Host);
    }

    [Fact]
    public async Task AutoSwitchAsync_NoMatch_ReturnsFailure()
    {
        var profileConfiguration = new ProfileConfiguration
        {
            Profiles = new Dictionary<string, ProxyProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["office"] = new ProxyProfile { Name = "office", Host = "office-proxy", Port = 8080, NetworkIdentifier = "172.16.0." }
            }
        };

        var profileService = new FakeProfileService(profileConfiguration);
        var syncService = new FakeSyncService();
        var networkDetectionService = new NetworkDetectionService(profileService, new FakeNetworkInformationProvider(new NetworkSnapshot("10.0.0.25", "10.0.0.1")));
        var automationService = new AutomationService(profileService, syncService, networkDetectionService);

        var result = await automationService.AutoSwitchAsync();

        Assert.False(result.Success);
        Assert.Empty(syncService.AppliedConfigs);
        Assert.Null(profileConfiguration.ActiveProfile);
    }
}
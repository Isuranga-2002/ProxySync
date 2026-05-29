using ProxySync.Core.Models;
using ProxySync.Services;

namespace ProxySync.Tests;

public class NetworkDetectionServiceTests
{
    [Fact]
    public async Task DetectMatchingProfileAsync_MatchesPrefixIdentifier()
    {
        var configuration = new ProfileConfiguration
        {
            Profiles = new Dictionary<string, ProxyProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["university"] = new ProxyProfile { Name = "university", Host = "proxy.example.com", Port = 3128, NetworkIdentifier = "10.0.0." }
            }
        };

        var profileService = new FakeProfileService(configuration);
        var detectionService = new NetworkDetectionService(profileService, new FakeNetworkInformationProvider(new NetworkSnapshot("10.0.0.25", "10.0.0.1")));

        var matchedProfile = await detectionService.DetectMatchingProfileAsync();

        Assert.NotNull(matchedProfile);
        Assert.Equal("university", matchedProfile!.Name);
    }

    [Fact]
    public async Task DetectMatchingProfileAsync_ReturnsNullWhenNoMatch()
    {
        var configuration = new ProfileConfiguration
        {
            Profiles = new Dictionary<string, ProxyProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["office"] = new ProxyProfile { Name = "office", Host = "office-proxy", Port = 8080, NetworkIdentifier = "172.16.0." }
            }
        };

        var profileService = new FakeProfileService(configuration);
        var detectionService = new NetworkDetectionService(profileService, new FakeNetworkInformationProvider(new NetworkSnapshot("10.0.0.25", "10.0.0.1")));

        var matchedProfile = await detectionService.DetectMatchingProfileAsync();

        Assert.Null(matchedProfile);
    }

    [Fact]
    public async Task GetCurrentNetworkIdentifierAsync_ReturnsGatewayPrefixWhenAvailable()
    {
        var configuration = new ProfileConfiguration();
        var profileService = new FakeProfileService(configuration);
        var detectionService = new NetworkDetectionService(profileService, new FakeNetworkInformationProvider(new NetworkSnapshot("10.0.0.25", "10.0.0.1")));

        var identifier = await detectionService.GetCurrentNetworkIdentifierAsync();

        Assert.Equal("10.0.0.", identifier);
    }
}
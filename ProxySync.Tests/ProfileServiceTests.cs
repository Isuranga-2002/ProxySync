using ProxySync.Core.Models;
using ProxySync.Services;
using Xunit;

namespace ProxySync.Tests;

public class ProfileServiceTests
{
    [Fact]
    public async Task LoadAsync_CreatesNewFile_WhenMissing()
    {
        var tempDirectory = CreateTempDirectory();
        var configPath = Path.Combine(tempDirectory, "profiles.json");
        var service = new ProfileService(configPath);

        var configuration = await service.LoadAsync();

        Assert.True(File.Exists(configPath));
        Assert.NotNull(configuration);
        Assert.Null(configuration.ActiveProfile);
        Assert.Empty(configuration.Profiles);
    }

    [Fact]
    public async Task LoadAsync_RecoversFromCorruptJson_ByBackingUpAndRecreatingFile()
    {
        var tempDirectory = CreateTempDirectory();
        var configPath = Path.Combine(tempDirectory, "profiles.json");
        await File.WriteAllTextAsync(configPath, "{ not valid json }");

        var service = new ProfileService(configPath);

        var configuration = await service.LoadAsync();

        var backupFile = Directory.GetFiles(tempDirectory, "profiles.json.corrupt.*.bak").SingleOrDefault();

        Assert.NotNull(configuration);
        Assert.True(File.Exists(configPath));
        Assert.NotNull(backupFile);

        var freshContent = await File.ReadAllTextAsync(configPath);
        Assert.Contains("Profiles", freshContent);
        Assert.Empty(configuration.Profiles);
    }

    [Fact]
    public async Task AddProfileAsync_SetsActiveProfileAndTrimsNetworkIdentifier()
    {
        var tempDirectory = CreateTempDirectory();
        var configPath = Path.Combine(tempDirectory, "profiles.json");
        var service = new ProfileService(configPath);

        await service.AddProfileAsync(new ProxyProfile
        {
            Name = "university",
            Host = "proxy.example.com",
            Port = 3128,
            NetworkIdentifier = " 10.0.0. "
        });

        var configuration = await service.LoadAsync();

        Assert.Equal("university", configuration.ActiveProfile);
        Assert.True(configuration.Profiles.TryGetValue("university", out var profile));
        Assert.NotNull(profile);
        Assert.Equal("10.0.0.", profile!.NetworkIdentifier);
    }

    [Fact]
    public async Task SwitchActiveProfileAsync_IsCaseInsensitive()
    {
        var tempDirectory = CreateTempDirectory();
        var configPath = Path.Combine(tempDirectory, "profiles.json");
        var service = new ProfileService(configPath);

        await service.AddProfileAsync(new ProxyProfile
        {
            Name = "University",
            Host = "proxy.example.com",
            Port = 3128,
            NetworkIdentifier = "10.0.0."
        });

        var switched = await service.SwitchActiveProfileAsync("university");
        var configuration = await service.LoadAsync();

        Assert.True(switched);
        Assert.Equal("university", configuration.ActiveProfile);
    }

    [Fact]
    public async Task AddProfileAsync_RejectsInvalidPort()
    {
        var tempDirectory = CreateTempDirectory();
        var configPath = Path.Combine(tempDirectory, "profiles.json");
        var service = new ProfileService(configPath);

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddProfileAsync(new ProxyProfile
        {
            Name = "bad",
            Host = "proxy.example.com",
            Port = 0
        }));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "proxysync-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
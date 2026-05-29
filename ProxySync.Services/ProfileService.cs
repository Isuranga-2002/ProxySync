using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ProxySync.Core.Models;

namespace ProxySync.Services;

public class ProfileService
{
    private readonly string configPath;

    public ProfileService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".proxysync"
        );

        Directory.CreateDirectory(dir);
        configPath = Path.Combine(dir, "profiles.json");
    }

    public async Task<ProfileConfiguration> LoadAsync()
    {
        if (!File.Exists(configPath))
        {
            var empty = new ProfileConfiguration();
            await SaveAsync(empty);
            return empty;
        }

        var json = await File.ReadAllTextAsync(configPath);
        try
        {
            var doc = JsonSerializer.Deserialize<ProfileConfiguration>(json);
            if (doc == null) return new ProfileConfiguration();
            if (doc.Profiles == null)
                doc.Profiles = new Dictionary<string, ProxyProfile>();
            return doc;
        }
        catch (JsonException)
        {
            // If file is corrupt, return an empty configuration to avoid crashes.
            return new ProfileConfiguration();
        }
    }

    public async Task SaveAsync(ProfileConfiguration configuration)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(configuration, options);

        // Atomic write: write to temp file then replace
        var tmp = configPath + ".tmp";
        await File.WriteAllTextAsync(tmp, json);

        try
        {
            File.Copy(tmp, configPath, true);
            File.Delete(tmp);
        }
        catch
        {
            // Best-effort cleanup; swallow to keep API simple.
        }
    }

    public async Task AddProfileAsync(ProxyProfile profile)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));

        var doc = await LoadAsync();
        if (doc.Profiles == null)
            doc.Profiles = new Dictionary<string, ProxyProfile>();

        doc.Profiles[profile.Name] = profile;

        // If there is no active profile, set this as active.
        if (string.IsNullOrEmpty(doc.ActiveProfile))
            doc.ActiveProfile = profile.Name;

        await SaveAsync(doc);
    }

    public async Task<IEnumerable<ProxyProfile>> ListProfilesAsync()
    {
        var doc = await LoadAsync();
        return doc.Profiles?.Values ?? Enumerable.Empty<ProxyProfile>();
    }

    public async Task<bool> SwitchActiveProfileAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        var doc = await LoadAsync();
        if (doc.Profiles == null || !doc.Profiles.ContainsKey(name))
            return false;

        doc.ActiveProfile = name;
        await SaveAsync(doc);
        return true;
    }

    public async Task<ProxyProfile?> GetActiveProfileAsync()
    {
        var doc = await LoadAsync();
        if (string.IsNullOrEmpty(doc.ActiveProfile) || doc.Profiles == null)
            return null;

        doc.Profiles.TryGetValue(doc.ActiveProfile, out var profile);
        return profile;
    }
}

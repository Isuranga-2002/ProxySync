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
        // When the file exists, reading/parsing errors should surface to the caller
        // so that we do not accidentally overwrite or discard valid data.
        var json = await File.ReadAllTextAsync(configPath);
        var doc = JsonSerializer.Deserialize<ProfileConfiguration>(json);
        if (doc == null) return new ProfileConfiguration();

        // Ensure profiles dictionary is not null and uses case-insensitive keys.
        if (doc.Profiles == null)
        {
            doc.Profiles = new Dictionary<string, ProxyProfile>(StringComparer.OrdinalIgnoreCase);
        }
        else if (doc.Profiles.Comparer != StringComparer.OrdinalIgnoreCase)
        {
            var normalized = new Dictionary<string, ProxyProfile>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in doc.Profiles)
            {
                if (kv.Key == null) continue;
                normalized[kv.Key] = kv.Value;
            }

            doc.Profiles = normalized;
        }

        return doc;
    }

    public async Task SaveAsync(ProfileConfiguration configuration)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var json = JsonSerializer.Serialize(configuration, options);

        // Ensure directory exists
        var dir = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // Atomic write: write to a unique temp file then move/replace
        var tmp = Path.Combine(dir ?? Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            await File.WriteAllTextAsync(tmp, json);

            if (File.Exists(configPath))
            {
                // Replace existing file atomically
                try
                {
                    File.Replace(tmp, configPath, null);
                }
                catch
                {
                    // Fallback to overwrite if Replace fails
                    File.Copy(tmp, configPath, true);
                    File.Delete(tmp);
                }
            }
            else
            {
                File.Move(tmp, configPath);
            }
        }
        finally
        {
            // Ensure temp file is removed if it still exists
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    public async Task AddProfileAsync(ProxyProfile profile)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));

        // Basic validation
        var name = profile.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Profile name is required.", nameof(profile));

        if (name.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0)
            throw new ArgumentException("Profile name contains invalid path characters.", nameof(profile));

        if (string.IsNullOrWhiteSpace(profile.Host))
            throw new ArgumentException("Profile host is required.", nameof(profile));

        if (profile.Port <= 0 || profile.Port > 65535)
            throw new ArgumentException("Profile port must be a positive integer between 1 and 65535.", nameof(profile));

        var doc = await LoadAsync();
        if (doc.Profiles == null)
            doc.Profiles = new Dictionary<string, ProxyProfile>(StringComparer.OrdinalIgnoreCase);

        doc.Profiles[name] = new ProxyProfile { Name = name, Host = profile.Host, Port = profile.Port };

        // If there is no active profile, set this as active.
        if (string.IsNullOrEmpty(doc.ActiveProfile))
            doc.ActiveProfile = name;

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

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
        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            var doc = JsonSerializer.Deserialize<ProfileConfiguration>(json);
            if (doc == null) return new ProfileConfiguration();
            if (doc.Profiles == null)
                doc.Profiles = new Dictionary<string, ProxyProfile>();
            return doc;
        }
        catch (JsonException)
        {
            // If file is corrupt, back it up and return an empty configuration to avoid crashes.
            try
            {
                var backup = configPath + ".corrupt." + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".bak";
                File.Move(configPath, backup);
            }
            catch
            {
                // ignore backup failures
            }

            var empty = new ProfileConfiguration();
            await SaveAsync(empty);
            return empty;
        }
        catch (IOException)
        {
            // IO problems reading the file — return an empty configuration to keep behavior safe.
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

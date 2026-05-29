using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ProxySync.Core.Models;

namespace ProxySync.Services;

public class ProfileService : IProfileService
{
    private readonly string configPath;

    public ProfileService(string? configPath = null)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".proxysync"
            );

            Directory.CreateDirectory(dir);
            this.configPath = Path.Combine(dir, "profiles.json");
        }
        else
        {
            var dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            this.configPath = configPath;
        }
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
            if (doc == null)
                throw new JsonException("profiles.json could not be deserialized into a ProfileConfiguration.");

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
        catch (JsonException ex)
        {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var backupPath = configPath + ".corrupt." + timestamp + ".bak";

            try
            {
                File.Move(configPath, backupPath);
                    Console.WriteLine($"Warning: detected invalid profiles.json. Backed up corrupted file to '{Path.GetFileName(backupPath)}' and created a fresh profiles.json.");

                    var empty = new ProfileConfiguration();
                    await SaveAsync(empty);
                    return empty;
            }
            catch (IOException)
            {
                // Preserve the original exception chain if the recovery move fails.
                throw new IOException("Unable to recover corrupted profiles.json.", ex);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
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

        // Atomic write: write to a unique temp file then replace via rename/move semantics.
        // Copy/delete fallback is intentionally avoided because it can leave a partially
        // written destination if the process crashes mid-copy. Renaming the fully written
        // temp file into place preserves the safest behavior the platform allows.
        var tmp = Path.Combine(dir ?? Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            await File.WriteAllTextAsync(tmp, json);

            if (File.Exists(configPath))
            {
                // Prefer File.Replace when available because it performs an atomic swap.
                try
                {
                    File.Replace(tmp, configPath, null);
                }
                catch (IOException)
                {
                    // If Replace cannot be used, fall back to a rename/move-based swap.
                    // This is safer than copy/delete because the destination is only
                    // replaced with the already-complete temp file.
                    File.Move(tmp, configPath, true);
                }
                catch (PlatformNotSupportedException)
                {
                    // Some platforms do not support Replace; use move-overwrite semantics.
                    File.Move(tmp, configPath, true);
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
        doc.Profiles[name].NetworkIdentifier = profile.NetworkIdentifier?.Trim();

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

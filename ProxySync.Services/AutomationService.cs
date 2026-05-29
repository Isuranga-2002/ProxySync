using ProxySync.Core.Models;

namespace ProxySync.Services;

public class AutomationService : IAutomationService
{
    private readonly IProfileService _profileService;
    private readonly ISyncService _syncService;
    private readonly INetworkDetectionService _networkDetectionService;

    public AutomationService(
        IProfileService profileService,
        ISyncService syncService,
        INetworkDetectionService networkDetectionService)
    {
        _profileService = profileService;
        _syncService = syncService;
        _networkDetectionService = networkDetectionService;
    }

    public async Task<AutomationResult> EnableAsync()
    {
        var profile = await _profileService.GetActiveProfileAsync();
        if (profile == null)
        {
            return new AutomationResult(false, "No active profile found. Use 'proxysync profile switch <name>' or 'proxysync auto-switch'.");
        }

        await ApplyProfileAsync(profile);
        return new AutomationResult(true, $"Proxy enabled.\nApplied profile: {profile.Name}", profile.Name);
    }

    public async Task<AutomationResult> DisableAsync()
    {
        await _syncService.DisableAllAsync();
        return new AutomationResult(true, "Proxy disabled.");
    }

    public async Task<AutomationResult> DetectAsync()
    {
        var networkIdentifier = await _networkDetectionService.GetCurrentNetworkIdentifierAsync();
        var matchingProfile = await _networkDetectionService.DetectMatchingProfileAsync();

        if (matchingProfile == null)
        {
            var displayIdentifier = string.IsNullOrWhiteSpace(networkIdentifier) ? "(unknown)" : networkIdentifier;
            return new AutomationResult(false, $"Detected network:\n{displayIdentifier}\n\nNo matching profile found.", null, networkIdentifier);
        }

        return new AutomationResult(true, $"Detected network:\n{networkIdentifier ?? "(unknown)"}\n\nSuggested profile:\n{matchingProfile.Name}", matchingProfile.Name, networkIdentifier);
    }

    public async Task<AutomationResult> AutoSwitchAsync()
    {
        var networkIdentifier = await _networkDetectionService.GetCurrentNetworkIdentifierAsync();
        var matchingProfile = await _networkDetectionService.DetectMatchingProfileAsync();

        if (matchingProfile == null)
        {
            var displayIdentifier = string.IsNullOrWhiteSpace(networkIdentifier) ? "(unknown)" : networkIdentifier;
            return new AutomationResult(false, $"Detected network:\n{displayIdentifier}\n\nNo matching profile found.", null, networkIdentifier);
        }

        var switched = await _profileService.SwitchActiveProfileAsync(matchingProfile.Name);
        if (!switched)
        {
            return new AutomationResult(false, $"Detected profile '{matchingProfile.Name}', but the profile could not be activated.", matchingProfile.Name, networkIdentifier);
        }

        var activeProfile = await _profileService.GetActiveProfileAsync();
        if (activeProfile == null)
        {
            return new AutomationResult(false, "Switched profile, but the active profile could not be loaded.", matchingProfile.Name, networkIdentifier);
        }

        await ApplyProfileAsync(activeProfile);
        return new AutomationResult(true, $"Detected profile: {activeProfile.Name}\nSwitched active profile.\nProxy synchronized successfully.", activeProfile.Name, networkIdentifier);
    }

    private async Task ApplyProfileAsync(ProxyProfile profile)
    {
        var config = new ProxyConfig
        {
            Host = profile.Host,
            Port = profile.Port,
            Username = null,
            Password = null
        };

        await _syncService.ApplyAllAsync(config);
    }
}
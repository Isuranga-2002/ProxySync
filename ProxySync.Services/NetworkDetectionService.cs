using System.Net;
using ProxySync.Core.Models;

namespace ProxySync.Services;

public class NetworkDetectionService : INetworkDetectionService
{
    private readonly IProfileService _profileService;
    private readonly INetworkInformationProvider _networkInformationProvider;

    public NetworkDetectionService(
        IProfileService profileService,
        INetworkInformationProvider networkInformationProvider)
    {
        _profileService = profileService;
        _networkInformationProvider = networkInformationProvider;
    }

    public async Task<string?> GetCurrentNetworkIdentifierAsync()
    {
        var snapshot = await _networkInformationProvider.GetSnapshotAsync();
        return BuildPrimaryNetworkIdentifier(snapshot);
    }

    public async Task<ProxyProfile?> DetectMatchingProfileAsync()
    {
        var snapshot = await _networkInformationProvider.GetSnapshotAsync();
        var profiles = await _profileService.ListProfilesAsync();
        var candidates = BuildCandidateIdentifiers(snapshot).ToArray();

        return profiles.FirstOrDefault(profile => IsProfileMatch(profile, candidates));
    }

    internal static bool IsProfileMatch(ProxyProfile profile, IEnumerable<string> candidateIdentifiers)
    {
        var profileIdentifier = profile.NetworkIdentifier?.Trim();
        if (string.IsNullOrWhiteSpace(profileIdentifier))
            return false;

        foreach (var candidate in candidateIdentifiers)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            if (candidate.StartsWith(profileIdentifier, StringComparison.OrdinalIgnoreCase) ||
                profileIdentifier.StartsWith(candidate, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate, profileIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> BuildCandidateIdentifiers(NetworkSnapshot snapshot)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddIfNotNull(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                seen.Add(value.Trim());
        }

        AddIfNotNull(BuildPrefix(snapshot.DefaultGatewayAddress));
        AddIfNotNull(snapshot.DefaultGatewayAddress);
        AddIfNotNull(BuildPrefix(snapshot.LocalIpAddress));
        AddIfNotNull(snapshot.LocalIpAddress);

        return seen;
    }

    private static string? BuildPrimaryNetworkIdentifier(NetworkSnapshot snapshot)
    {
        var gatewayPrefix = BuildPrefix(snapshot.DefaultGatewayAddress);
        if (!string.IsNullOrWhiteSpace(gatewayPrefix))
            return gatewayPrefix;

        var localPrefix = BuildPrefix(snapshot.LocalIpAddress);
        if (!string.IsNullOrWhiteSpace(localPrefix))
            return localPrefix;

        return snapshot.LocalIpAddress?.Trim();
    }

    private static string? BuildPrefix(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        if (IPAddress.TryParse(address, out var parsed) && parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var octets = parsed.ToString().Split('.');
            if (octets.Length == 4)
                return string.Join('.', octets.Take(3)) + ".";
        }

        var segments = address.Trim().Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 3)
            return string.Join('.', segments.Take(3)) + ".";

        return address.Trim();
    }
}
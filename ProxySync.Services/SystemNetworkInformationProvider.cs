using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ProxySync.Services;

public class SystemNetworkInformationProvider : INetworkInformationProvider
{
    public Task<NetworkSnapshot> GetSnapshotAsync()
    {
        string? localIpAddress = null;
        string? defaultGatewayAddress = null;

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
                continue;

            var properties = networkInterface.GetIPProperties();

            defaultGatewayAddress ??= properties.GatewayAddresses
                .Select(address => address.Address)
                .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.Any.Equals(address))
                ?.ToString();

            localIpAddress ??= properties.UnicastAddresses
                .Select(address => address.Address)
                .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                ?.ToString();

            if (localIpAddress != null && defaultGatewayAddress != null)
                break;
        }

        return Task.FromResult(new NetworkSnapshot(localIpAddress, defaultGatewayAddress));
    }
}
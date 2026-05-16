using System;

namespace ProxySync.Services.SystemEnvironment
{
    public class EnvironmentProxyService
    {
        public void Apply(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl, EnvironmentVariableTarget.User);

            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl, EnvironmentVariableTarget.User);

            Environment.SetEnvironmentVariable("ALL_PROXY", proxyUrl, EnvironmentVariableTarget.User);

            Console.WriteLine("Proxy environment variables applied.");
        }

        public void Disable()
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", null, EnvironmentVariableTarget.User);

            Environment.SetEnvironmentVariable("HTTPS_PROXY", null, EnvironmentVariableTarget.User);

            Environment.SetEnvironmentVariable("ALL_PROXY", null, EnvironmentVariableTarget.User);

            Console.WriteLine("Proxy environment variables removed.");
        }
    }
}
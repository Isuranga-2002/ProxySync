namespace ProxySync.Services;

public interface ICommandRunner
{
    Task<int> RunAsync(string fileName, string arguments);
}
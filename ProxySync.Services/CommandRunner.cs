using System.Diagnostics;

namespace ProxySync.Services;

public class CommandRunner : ICommandRunner
{
    public async Task<int> RunAsync(string fileName, string arguments)
    {
        var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {fileName} {arguments}",

            RedirectStandardOutput = true,
            RedirectStandardError = true,

            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start {fileName}");
            Console.WriteLine(ex.Message);

            return -1;
        }

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        Console.WriteLine(output);

        if (!string.IsNullOrWhiteSpace(error))
            Console.WriteLine(error);

        return process.ExitCode;
    }
}
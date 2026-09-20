using System.Diagnostics;

namespace Windows11Optimizer.Services;

public static class CommandRunner
{
    public static async Task<(int ExitCode, string Output, string Error)> RunAsync(
        string fileName,
        IEnumerable<string> arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = psi };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        return (process.ExitCode, await outputTask, await errorTask);
    }
}

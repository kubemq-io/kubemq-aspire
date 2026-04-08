using System.Diagnostics;
using Xunit;

namespace KubeMQ.Aspire.Hosting.Tests;

/// <summary>
/// xUnit fact attribute that skips the test when Docker is not available.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class DockerAvailableAttribute : FactAttribute
{
    private static readonly Lazy<bool> IsAvailable = new(CheckDocker);

    public DockerAvailableAttribute()
    {
        if (!IsAvailable.Value)
        {
            Skip = "Docker is not available on this machine";
        }
    }

    private static bool CheckDocker()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null)
            {
                return false;
            }

            process.WaitForExit(TimeSpan.FromSeconds(5));
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

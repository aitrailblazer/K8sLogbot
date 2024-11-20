using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: dotnet run <accessCode>");
            return;
        }

        string accessCode = args[0];
        string issueTitle = "Kubernetes Pod Log Analysis Report"; // Default issue title

        // Embed the log analysis content as a string
        string logAnalysisContent = @"
### Kubernetes Pod Log Analysis

Here's a snippet from the Kubernetes pod logs:

2023-11-19T12:30:00.000Z [info]  Application starting...
2023-11-19T12:30:15.000Z [info]  Database connection established.
2023-11-19T12:32:00.000Z [warn]  Database query took longer than 500ms, potential performance bottleneck.
2023-11-19T12:33:00.000Z [error] Failed to connect to external service: Connection refused
2023-11-19T12:35:00.000Z [info]  External service now reachable, resuming normal operation.
2023-11-19T12:40:00.000Z [info]  Processing batch job #12345.
2023-11-19T12:45:00.000Z [warn]  Memory usage is at 85%, monitor for potential issues.
2023-11-19T12:50:00.000Z [error] Out of memory error during batch job processing. Job #12345 was terminated.
2023-11-19T12:55:00.000Z [info]  Application restarted after OutOfMemoryError, health checks passed.
2023-11-19T13:00:00.000Z [info]  New request received at /api/v1/data endpoint.
2023-11-19T13:05:00.000Z [warn]  Retrying operation after temporary network issue.

**Analysis Results:**
- **Warnings Detected:**
  - At 12:32:00Z, a performance bottleneck was noted with database queries.
  - Memory usage warning at 12:45:00Z indicating potential for OutOfMemory errors.
  - Network retry warning at 13:05:00Z due to temporary issues.

- **Errors Found:**
  - Connection to external service failed at 12:33:00Z.
  - An OutOfMemoryError occurred at 12:50:00Z during job processing.

- **Info Logs:**
  - Application startup and normal operations were logged.

**Recommendations:**
- Investigate the database query performance issue.
- Review the application's memory usage and consider increasing resource limits or optimizing memory usage.
- Check configurations for external service connections and implement retry logic with backoff.
- Monitor network stability and consider implementing circuit breaker patterns for network-related operations.";

        string knownSecretHash = Environment.GetEnvironmentVariable("ACCESS_CODE_HASH");

        if (string.IsNullOrEmpty(knownSecretHash))
        {
            Console.WriteLine("ACCESS_CODE_HASH environment variable not found. Please set it before running the application.");
            return;
        }

        if (await ValidateAccessCode(accessCode, knownSecretHash))
        {
            Console.WriteLine("Access code is valid. Triggering GitHub Action...");
            await TriggerGitHubAction(accessCode, issueTitle, logAnalysisContent);
        }
        else
        {
            Console.WriteLine("Invalid access code. Access denied.");
        }
    }

    private static async Task<bool> ValidateAccessCode(string accessCode, string knownSecretHash)
    {
        using (var sha256 = SHA256.Create())
        {
            var codeBytes = Encoding.UTF8.GetBytes(accessCode);
            var computedHash = BitConverter.ToString(sha256.ComputeHash(codeBytes)).Replace("-", "").ToLower();

            return knownSecretHash.Equals(computedHash, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task TriggerGitHubAction(string accessCode, string issueTitle, string logAnalysisContent)
    {
        // Escape quotes in the inputs to prevent command injection
        string escapedAccessCode = accessCode.Replace("\"", "\\\"");
        string escapedIssueTitle = issueTitle.Replace("\"", "\\\"");
        string escapedLogAnalysis = logAnalysisContent.Replace("\"", "\\\"").Replace(Environment.NewLine, "\\n");

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "gh",
            Arguments = $"workflow run simple-log-analysis-test.yml -f access_code=\"{escapedAccessCode}\" -f issue_title=\"{escapedIssueTitle}\" -f log_analysis=\"{escapedLogAnalysis}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(outputTask, errorTask);

                Console.WriteLine($"GitHub Action Output: {await outputTask}");

                if (process.WaitForExit(30000)) // Wait for 30 seconds
                {
                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine($"Error when triggering GitHub Action: {await errorTask}");
                    }
                    else
                    {
                        Console.WriteLine("GitHub Action successfully triggered.");
                    }
                }
                else
                {
                    Console.WriteLine("GitHub Action did not complete within the expected timeframe.");
                    process.Kill(); // Ensure the process is terminated if it hangs
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while triggering the GitHub Action: {ex.Message}");
        }
    }
}

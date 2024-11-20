using System;
using System.Diagnostics;
using System.IO;
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

        string knownSecretHash = Environment.GetEnvironmentVariable("ACCESS_CODE_HASH") ?? string.Empty;

        if (string.IsNullOrEmpty(knownSecretHash))
        {
            Console.WriteLine("ACCESS_CODE_HASH environment variable not found. Please set it before running the application.");
            return;
        }

        if (await ValidateAccessCode(accessCode, knownSecretHash))
        {
            Console.WriteLine("Access code is valid. Triggering GitHub Action...");
            await TriggerGitHubAction(accessCode);
        }
        else
        {
            Console.WriteLine("Invalid access code. Access denied.");
        }
    }

    // This method is required to suppress the CS1998 warning
    private static Task CompletedTask => Task.CompletedTask;

    private static async Task<bool> ValidateAccessCode(string accessCode, string knownSecretHash)
    {
        using (var sha256 = SHA256.Create())
        {
            var codeBytes = Encoding.UTF8.GetBytes(accessCode);
            var computedHash = BitConverter.ToString(sha256.ComputeHash(codeBytes)).Replace("-", "").ToLower();

            return await Task.FromResult(knownSecretHash.Equals(computedHash, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static async Task TriggerGitHubAction(string accessCode)
    {
        // Escape quotes in the access code to prevent command injection
        string escapedAccessCode = accessCode.Replace("\"", "\\\"");

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "gh",
            Arguments = $"workflow run simple-log-analysis-test.yml -f access_code=\"{escapedAccessCode}\"",
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

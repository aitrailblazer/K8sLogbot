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
        string knownSecretHash = Environment.GetEnvironmentVariable("ACCESS_CODE_HASH");

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

    private static async Task<bool> ValidateAccessCode(string accessCode, string knownSecretHash)
    {
        using var sha256 = SHA256.Create();
        var codeBytes = Encoding.UTF8.GetBytes(accessCode);
        var computedHash = BitConverter.ToString(sha256.ComputeHash(codeBytes)).Replace("-", "").ToLower();

        return knownSecretHash.Equals(computedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task TriggerGitHubAction(string accessCode)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "gh",
            Arguments = $"workflow run simple-log-analysis-test.yml -f accessCode=\"{accessCode}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = new Process { StartInfo = processStartInfo })
        {
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            Console.WriteLine(output);

            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode != 0)
            {
                string error = await process.StandardError.ReadToEndAsync();
                Console.WriteLine($"Error: {error}");
            }
        }
    }
}
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Azure;
using Azure.AI.Inference;

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
            Console.WriteLine("Access code is valid. Generating GPT-based analysis...");

            // Original log data
            string logData = @"
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
2023-11-19T13:05:00.000Z [warn]  Retrying operation after temporary network issue.";

            // Call GPT for analysis
            var (issueTitle, logAnalysisContent) = await GenerateGPTAnalysis(logData);

            if (string.IsNullOrEmpty(issueTitle) || string.IsNullOrEmpty(logAnalysisContent))
            {
                Console.WriteLine("Failed to generate GPT-based analysis. Exiting.");
                return;
            }

            Console.WriteLine("GPT-based analysis generated. Triggering GitHub Action...");
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

    private static async Task<(string issueTitle, string logAnalysisContent)> GenerateGPTAnalysis(string logData)
    {
        try
        {
            var endpoint = new Uri(Environment.GetEnvironmentVariable("ENDPOINT"));
            var credential = new AzureKeyCredential(Environment.GetEnvironmentVariable("API_KEY"));
            var model = Environment.GetEnvironmentVariable("MODEL");

            var client = new ChatCompletionsClient(endpoint, credential, new ChatCompletionsClientOptions());

            string prompt = $@"
Analyze the following log data and create:
1. A concise, descriptive title for the analysis.
2. A structured summary of key events, warnings, and errors.

Log Data:
{logData}";

            var requestOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatRequestSystemMessage("You are a helpful assistant specialized in analyzing logs."),
                    new ChatRequestUserMessage(prompt),
                },
                Temperature = 0.7f,
                NucleusSamplingFactor = 0.9f,
                MaxTokens = 1000,
                Model = model
            };

            Response<ChatCompletions> response = await client.CompleteAsync(requestOptions);

            string[] output = response.Value.Choices[0].Message.Content.Split(new[] { "\n" }, 2, StringSplitOptions.RemoveEmptyEntries);

            string issueTitle = output[0]?.Replace("Title: ", "").Trim();
            string logAnalysisContent = output.Length > 1 ? output[1].Trim() : string.Empty;

            return (issueTitle, logAnalysisContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during GPT analysis: {ex.Message}");
            return (string.Empty, string.Empty);
        }
    }

    private static async Task TriggerGitHubAction(string accessCode, string issueTitle, string logAnalysisContent)
    {
        string escapedAccessCode = EscapeForCLI(accessCode);
        string escapedIssueTitle = EscapeForCLI(issueTitle);
        string escapedLogAnalysis = EscapeForCLI(logAnalysisContent);

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

                Console.WriteLine($"GitHub Action Output:\n{await outputTask}");

                if (process.WaitForExit(30000)) // Wait for 30 seconds
                {
                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine($"Error when triggering GitHub Action:\n{await errorTask}");
                    }
                    else
                    {
                        Console.WriteLine("GitHub Action successfully triggered.");
                    }
                }
                else
                {
                    Console.WriteLine("GitHub Action did not complete within the expected timeframe.");
                    process.Kill();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while triggering the GitHub Action: {ex.Message}");
        }
    }

    private static string EscapeForCLI(string input)
    {
        return input.Replace("\"", "\\\"").Replace(Environment.NewLine, "\\n");
    }
}

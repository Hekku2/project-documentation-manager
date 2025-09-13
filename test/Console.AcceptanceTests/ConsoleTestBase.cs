using System.Text;

namespace ProjectDocumentationManager.Console.AcceptanceTests;

public abstract class ConsoleTestBase
{
    private static readonly SemaphoreSlim _gate = new(1, 1);
    protected const int SuccessExitCode = 0;
    protected const int ErrorExitCode = 1;
    protected static async Task<CommandResult> RunConsoleCommandDirectlyAsync(params string[] args)
    {
        await _gate.WaitAsync();

        try
        {
            using var process = new System.Diagnostics.Process();
            var consolePath = Path.Combine(AppContext.BaseDirectory, "Console.dll");

            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = $"\"{consolePath}\" " + string.Join(" ", args.Select(arg => $"\"{arg}\""));
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var output = StripAnsiColorCodes(outputBuilder.ToString());
            var error = StripAnsiColorCodes(errorBuilder.ToString());
            var combinedOutput = string.IsNullOrEmpty(error) ? output : $"{output}\nERROR: {error}";

            return new CommandResult
            {
                ExitCode = process.ExitCode,
                Output = combinedOutput.Trim()
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string StripAnsiColorCodes(string input)
    {
        // Remove ANSI escape sequences (color codes)
        return System.Text.RegularExpressions.Regex.Replace(input, @"\x1b\[[0-9;]*m", string.Empty);
    }

    protected record CommandResult
    {
        public required int ExitCode { get; init; }
        public required string Output { get; init; }
    }
}
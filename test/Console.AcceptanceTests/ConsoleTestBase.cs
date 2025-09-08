using System.Text;

namespace ProjectDocumentationManager.Console.AcceptanceTests;

public abstract class ConsoleTestBase
{
    protected static async Task<CommandResult> RunConsoleCommandDirectlyAsync(params string[] args)
    {
        var originalOut = System.Console.Out;
        var originalError = System.Console.Error;
        var originalLogLevel = Environment.GetEnvironmentVariable("Logging__LogLevel__Default");

        var outputBuffer = new StringBuilder();
        var errorBuffer = new StringBuilder();

        // Create persistent TextWriters that won't be disposed during execution
        var outputWriter = new PersistentStringWriter(outputBuffer);
        var errorWriter = new PersistentStringWriter(errorBuffer);

        try
        {
            // Redirect console output
            System.Console.SetOut(outputWriter);
            System.Console.SetError(errorWriter);

            // Set logging level to capture more output in tests
            Environment.SetEnvironmentVariable("Logging__LogLevel__Default", "Information");

            // Call the Program.Main method directly
            var exitCode = await Program.Main(args);

            // Give a small delay to ensure all output is captured
            await Task.Delay(50);

            var output = outputBuffer.ToString();
            var error = errorBuffer.ToString();
            var combinedOutput = string.IsNullOrEmpty(error) ? output : $"{output}\nERROR: {error}";

            return new CommandResult
            {
                ExitCode = exitCode,
                Output = combinedOutput.Trim()
            };
        }
        finally
        {
            // Restore original console output first
            System.Console.SetOut(originalOut);
            System.Console.SetError(originalError);

            // Restore original log level
            if (originalLogLevel != null)
                Environment.SetEnvironmentVariable("Logging__LogLevel__Default", originalLogLevel);
            else
                Environment.SetEnvironmentVariable("Logging__LogLevel__Default", null);
        }
    }

    // Custom StringWriter that doesn't throw when disposed
    private class PersistentStringWriter : TextWriter
    {
        private readonly StringBuilder _buffer;
        private bool _disposed;

        public PersistentStringWriter(StringBuilder buffer)
        {
            _buffer = buffer;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            if (!_disposed)
                _buffer.Append(value);
        }

        public override void Write(string? value)
        {
            if (!_disposed && value != null)
                _buffer.Append(value);
        }

        public override void WriteLine(string? value)
        {
            if (!_disposed)
                _buffer.AppendLine(value);
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }
    }

    protected record CommandResult
    {
        public required int ExitCode { get; init; }
        public required string Output { get; init; }
    }
}
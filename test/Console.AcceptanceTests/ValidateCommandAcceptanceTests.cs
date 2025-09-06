using System.Diagnostics;

namespace Console.AcceptanceTests;

[TestFixture]
public class ValidateCommandAcceptanceTests
{
    private string _consolePath = null!;

    [SetUp]
    public void Setup()
    {
        // Get the solution root directory and build from there
        var solutionRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
        var buildResult = RunDotnetCommandInDirectory(solutionRoot, "build");
        Assert.That(buildResult.ExitCode, Is.EqualTo(0), $"Build failed: {buildResult.Output}");
        
        // Find the built executable
        var buildOutputPath = Path.Combine(TestContext.CurrentContext.TestDirectory, 
            "../../src/Console/bin/Debug/net9.0");
        _consolePath = Path.Combine(buildOutputPath, "Console.dll");
        
        Assert.That(File.Exists(_consolePath), Is.True, $"Console executable not found at {_consolePath}");
    }

    [Test]
    public void ValidateCommand_Should_Pass_For_Valid_Templates()
    {
        var inputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/BasicScenario");
        var result = RunConsoleCommand("validate", inputFolder);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(0), $"Validation should pass: {result.Output}");
            Assert.That(result.Output, Contains.Substring("validated successfully"), 
                "Should show success message");
        });
    }

    [Test]
    public void ValidateCommand_Should_Fail_For_Invalid_Templates()
    {
        var inputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/ErrorScenario");
        var result = RunConsoleCommand("validate", inputFolder);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(1), "Validation should fail for invalid templates");
            Assert.That(result.Output, Contains.Substring("errors"), "Should show error information");
        });
    }

    [Test]
    public void ValidateCommand_Should_Return_Error_For_Nonexistent_Input_Folder()
    {
        var nonExistentFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var result = RunConsoleCommand("validate", nonExistentFolder);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(1), "Command should fail for nonexistent folder");
            Assert.That(result.Output, Contains.Substring("does not exist"), "Should show error message");
        });
    }

    [Test]
    public void ValidateCommand_Should_Handle_Empty_Input_Folder()
    {
        var emptyFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(emptyFolder);
        
        try
        {
            var result = RunConsoleCommand("validate", emptyFolder);

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.EqualTo(0), "Command should succeed for empty folder");
                Assert.That(result.Output, Contains.Substring("No markdown template files found"), 
                    "Should show warning for no files");
            });
        }
        finally
        {
            if (Directory.Exists(emptyFolder))
                Directory.Delete(emptyFolder, true);
        }
    }

    [Test]
    public void ValidateCommand_Should_Show_Validation_Summary()
    {
        var inputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/BasicScenario");
        var result = RunConsoleCommand("validate", inputFolder);

        Assert.Multiple(() =>
        {
            Assert.That(result.Output, Contains.Substring("Valid files"), "Should show valid file count");
            Assert.That(result.Output, Contains.Substring("Invalid files"), "Should show invalid file count");
            Assert.That(result.Output, Contains.Substring("Total files"), "Should show total file count");
        });
    }

    private CommandResult RunConsoleCommand(string command, string inputFolder)
    {
        // Get the solution root directory (go up from test output directory)
        var solutionRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
        var consoleProjectPath = Path.Combine(solutionRoot, "src/Console");
        
        return RunDotnetCommandInDirectory(consoleProjectPath, "run", "--", command, inputFolder);
    }

    private CommandResult RunDotnetCommand(params string[] arguments)
    {
        return RunDotnetCommandInDirectory(null, arguments);
    }

    private CommandResult RunDotnetCommandInDirectory(string? workingDirectory, params string[] arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = string.Join(" ", arguments.Select(arg => 
                    arg.Contains(' ') ? $"\"{arg}\"" : arg)),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? TestContext.CurrentContext.TestDirectory
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var combinedOutput = string.IsNullOrEmpty(error) ? output : $"{output}\nERROR: {error}";
        
        return new CommandResult
        {
            ExitCode = process.ExitCode,
            Output = combinedOutput.Trim()
        };
    }

    private record CommandResult
    {
        public required int ExitCode { get; init; }
        public required string Output { get; init; }
    }
}
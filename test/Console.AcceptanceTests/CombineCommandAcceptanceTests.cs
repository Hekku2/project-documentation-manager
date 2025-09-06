using System.Diagnostics;

namespace Console.AcceptanceTests;

[TestFixture]
public class CombineCommandAcceptanceTests
{
    private string _testOutputFolder = null!;
    private string _consolePath = null!;

    [SetUp]
    public void Setup()
    {
        _testOutputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputFolder);
        
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

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testOutputFolder))
            Directory.Delete(_testOutputFolder, true);
    }

    [Test]
    public void CombineCommand_Should_Process_Basic_Scenario_Successfully()
    {
        var inputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/BasicScenario");
        var result = RunConsoleCommand("combine", inputFolder, _testOutputFolder);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(0), $"Command failed: {result.Output}");
            
            var outputFile = Path.Combine(_testOutputFolder, "input.md");
            Assert.That(File.Exists(outputFile), Is.True, "Output file was not created");
            
            var actualContent = File.ReadAllText(outputFile).Trim();
            var expectedContent = File.ReadAllText(Path.Combine(inputFolder, "expected-output.md")).Trim();
            Assert.That(actualContent, Is.EqualTo(expectedContent), "Output content does not match expected");
        });
    }

    [Test]
    public void CombineCommand_Should_Return_Error_For_Nonexistent_Input_Folder()
    {
        var nonExistentFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var result = RunConsoleCommand("combine", nonExistentFolder, _testOutputFolder);

        Assert.That(result.ExitCode, Is.EqualTo(1), "Command should fail for nonexistent folder");
        Assert.That(result.Output, Contains.Substring("does not exist"), "Should show error message");
    }

    [Test]
    public void CombineCommand_Should_Create_Output_Directory()
    {
        var inputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/BasicScenario");
        var newOutputFolder = Path.Combine(_testOutputFolder, "new-output");
        
        Assert.That(Directory.Exists(newOutputFolder), Is.False, "Output directory should not exist initially");
        
        var result = RunConsoleCommand("combine", inputFolder, newOutputFolder);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(0), "Command should succeed");
            Assert.That(Directory.Exists(newOutputFolder), Is.True, "Output directory should be created");
        });
    }

    [Test]
    public void CombineCommand_Should_Handle_Empty_Input_Folder()
    {
        var emptyFolder = Path.Combine(_testOutputFolder, "empty");
        Directory.CreateDirectory(emptyFolder);
        
        var result = RunConsoleCommand("combine", emptyFolder, _testOutputFolder);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(0), "Command should succeed for empty folder");
            Assert.That(result.Output, Contains.Substring("No markdown template files found"), 
                "Should show warning for no files");
        });
    }

    private CommandResult RunConsoleCommand(string command, string inputFolder, string outputFolder)
    {
        // Get the solution root directory (go up from test output directory)
        var solutionRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
        var consoleProjectPath = Path.Combine(solutionRoot, "src/Console");
        
        return RunDotnetCommandInDirectory(consoleProjectPath, "run", "--", command, inputFolder, outputFolder);
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
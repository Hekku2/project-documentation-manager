namespace MarkdownCompiler.Console.AcceptanceTests;

[TestFixture]
[NonParallelizable]
public class CombineCommandAcceptanceTests : ConsoleTestBase
{
    private string _testOutputFolder = null!;

    [SetUp]
    public void Setup()
    {
        _testOutputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputFolder);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testOutputFolder))
            Directory.Delete(_testOutputFolder, true);
    }

    [Test]
    public async Task CombineCommand_Should_Process_Basic_Scenario_Successfully()
    {
        var inputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/BasicScenario");
        var result = await RunConsoleCommandDirectlyAsync("combine", inputFolder, _testOutputFolder);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(SuccessExitCode), $"Command failed: {result.Output}");

            var outputFile = Path.Combine(_testOutputFolder, "input.md");
            Assert.That(File.Exists(outputFile), Is.True, "Output file was not created");

            var actualContent = File.ReadAllText(outputFile).Trim();
            var expectedContent = File.ReadAllText(Path.Combine(inputFolder, "expected-output.md")).Trim();
            Assert.That(actualContent, Is.EqualTo(expectedContent), "Output content does not match expected");
        });
    }

    [Test]
    public async Task CombineCommand_Should_Return_Error_For_Nonexistent_Input_Folder()
    {
        var nonExistentFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var result = await RunConsoleCommandDirectlyAsync("combine", nonExistentFolder, _testOutputFolder);

        Assert.That(result.ExitCode, Is.EqualTo(ErrorExitCode), "Command should fail for nonexistent folder");
    }

    [Test]
    public async Task CombineCommand_Should_Create_Output_Directory()
    {
        var inputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/BasicScenario");
        var newOutputFolder = Path.Combine(_testOutputFolder, "new-output");

        Assert.That(Directory.Exists(newOutputFolder), Is.False, "Output directory should not exist initially");

        var result = await RunConsoleCommandDirectlyAsync("combine", inputFolder, newOutputFolder);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(SuccessExitCode), "Command should succeed");
            Assert.That(Directory.Exists(newOutputFolder), Is.True, "Output directory should be created");
        });
    }

    [Test]
    public async Task CombineCommand_Should_Handle_Empty_Input_Folder()
    {
        var emptyFolder = Path.Combine(_testOutputFolder, "empty");
        Directory.CreateDirectory(emptyFolder);

        var result = await RunConsoleCommandDirectlyAsync("combine", emptyFolder, _testOutputFolder);

        Assert.That(result.ExitCode, Is.EqualTo(ErrorExitCode), "Command should fail for empty folder with no template files");
    }

}
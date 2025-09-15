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

    [Test]
    public async Task CombineCommand_Should_Process_Nested_Folder_Structure_Successfully()
    {
        var inputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/FolderTreeScenario");
        var result = await RunConsoleCommandDirectlyAsync("combine", inputFolder, _testOutputFolder);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(SuccessExitCode), $"Command failed: {result.Output}");

            // Verify API documentation files
            var apiEndpointsOutput = Path.Combine(_testOutputFolder, "docs/api/endpoints.md");
            var apiAuthOutput = Path.Combine(_testOutputFolder, "docs/api/auth.md");
            
            Assert.That(File.Exists(apiEndpointsOutput), Is.True, "API endpoints output file was not created");
            Assert.That(File.Exists(apiAuthOutput), Is.True, "API auth output file was not created");

            // Verify guide files
            var guidesGettingStartedOutput = Path.Combine(_testOutputFolder, "docs/guides/getting-started.md");
            var guidesAdvancedOutput = Path.Combine(_testOutputFolder, "docs/guides/advanced.md");
            
            Assert.That(File.Exists(guidesGettingStartedOutput), Is.True, "Getting started guide output file was not created");
            Assert.That(File.Exists(guidesAdvancedOutput), Is.True, "Advanced guide output file was not created");

            // Verify overview file
            var overviewOutput = Path.Combine(_testOutputFolder, "docs/overview.md");
            Assert.That(File.Exists(overviewOutput), Is.True, "Overview output file was not created");

            // Verify existing markdown file is copied
            var existingContentOutput = Path.Combine(_testOutputFolder, "assets/existing-content.md");
            Assert.That(File.Exists(existingContentOutput), Is.True, "Existing content file was not copied");

            // Verify content matches expected output for key files
            var expectedOutputsFolder = Path.Combine(inputFolder, "expected-outputs");
            
            var actualApiEndpoints = File.ReadAllText(apiEndpointsOutput).Trim();
            var expectedApiEndpoints = File.ReadAllText(Path.Combine(expectedOutputsFolder, "api/endpoints.md")).Trim();
            Assert.That(actualApiEndpoints, Is.EqualTo(expectedApiEndpoints), "API endpoints content does not match expected");

            var actualGettingStarted = File.ReadAllText(guidesGettingStartedOutput).Trim();
            var expectedGettingStarted = File.ReadAllText(Path.Combine(expectedOutputsFolder, "guides/getting-started.md")).Trim();
            Assert.That(actualGettingStarted, Is.EqualTo(expectedGettingStarted), "Getting started content does not match expected");

            var actualOverview = File.ReadAllText(overviewOutput).Trim();
            var expectedOverview = File.ReadAllText(Path.Combine(expectedOutputsFolder, "overview.md")).Trim();
            Assert.That(actualOverview, Is.EqualTo(expectedOverview), "Overview content does not match expected");
        });
    }

}
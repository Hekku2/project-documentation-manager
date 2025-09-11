using System.Runtime;

namespace ProjectDocumentationManager.Console.AcceptanceTests;

[TestFixture]
[NonParallelizable]
public class ValidateCommandAcceptanceTests : ConsoleTestBase
{
    [Test]
    public async Task ValidateCommand_Should_Pass_For_Valid_Templates()
    {
        var inputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "BasicScenario");
        var result = await RunConsoleCommandDirectlyAsync("validate", inputFolder);

        Assert.That(result.ExitCode, Is.EqualTo(SuccessExitCode), $"Validation should pass: {result.Output}");
    }

    [Test]
    public async Task ValidateCommand_Should_Fail_For_Invalid_Templates()
    {
        var inputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ErrorScenario");
        var result = await RunConsoleCommandDirectlyAsync("validate", inputFolder);

        Assert.That(result.ExitCode, Is.EqualTo(ErrorExitCode), "Validation should fail for invalid templates");
    }

    [Test]
    public async Task ValidateCommand_Should_Return_Error_For_Nonexistent_Input_Folder()
    {
        var nonExistentFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var result = await RunConsoleCommandDirectlyAsync("validate", nonExistentFolder);

        Assert.That(result.ExitCode, Is.EqualTo(ErrorExitCode), "Command should fail for nonexistent folder");
    }

    [Test]
    public async Task ValidateCommand_Should_Handle_Empty_Input_Folder()
    {
        var emptyFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(emptyFolder);

        try
        {
            var result = await RunConsoleCommandDirectlyAsync("validate", emptyFolder);

            Assert.That(result.ExitCode, Is.EqualTo(SuccessExitCode), "Command should succeed for empty folder");
        }
        finally
        {
            if (Directory.Exists(emptyFolder))
                Directory.Delete(emptyFolder, true);
        }
    }

    [Test]
    public async Task ValidateCommand_Should_Show_Validation_Summary()
    {
        var inputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "BasicScenario");
        var result = await RunConsoleCommandDirectlyAsync("validate", inputFolder);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(SuccessExitCode), "Validation should succeed for basic scenario");
            var output = result.Output;
            Assert.That(output, Is.Not.Empty, "Command output should not be empty");
            Assert.That(output, Does.Contain("Found 1 template files and 1 source files"), "Validation completion message missing");
            Assert.That(output, Does.Contain("│ Valid files   │ 1     │"), "Validation result summary missing");
            Assert.That(output, Does.Contain("│ Invalid files │ 0     │"), "Validation result summary missing");
            Assert.That(output, Does.Contain("│ Total files   │ 1     │"), "Validation result summary missing");
        });
    }

}
using Business.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Business.Tests.Services;

[TestFixture]
public class MarkdownFileCollectorServiceTests
{
    private ILogger<MarkdownFileCollectorService> _logger = null!;
    private MarkdownFileCollectorService _service = null!;
    private string _testDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = NullLoggerFactory.Instance.CreateLogger<MarkdownFileCollectorService>();
        _service = new MarkdownFileCollectorService(_logger);
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Test]
    public async Task CollectTemplateFilesAsync_Should_Collect_Mdext_Files()
    {
        // Arrange
        var templateFile1 = Path.Combine(_testDirectory, "template1.mdext");
        var templateFile2 = Path.Combine(_testDirectory, "template2.mdext");
        var sourceFile = Path.Combine(_testDirectory, "source.mdsrc");
        var regularFile = Path.Combine(_testDirectory, "readme.md");

        await File.WriteAllTextAsync(templateFile1, "# Template 1\n<insert source.mdsrc>");
        await File.WriteAllTextAsync(templateFile2, "# Template 2\nContent");
        await File.WriteAllTextAsync(sourceFile, "Source content");
        await File.WriteAllTextAsync(regularFile, "Regular markdown");

        // Act
        var result = await _service.CollectTemplateFilesAsync(_testDirectory);
        var templateFiles = result.ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(templateFiles, Has.Count.EqualTo(2), "Should collect only .mdext files");

            var template1 = templateFiles.FirstOrDefault(t => t.FileName == "template1.mdext");
            var template2 = templateFiles.FirstOrDefault(t => t.FileName == "template2.mdext");

            Assert.That(template1, Is.Not.Null, "Should find template1.mdext");
            Assert.That(template2, Is.Not.Null, "Should find template2.mdext");
            Assert.That(template1!.Content, Does.Contain("Template 1"), "Should contain template content");
            Assert.That(template1.Content, Does.Contain("<insert source.mdsrc>"), "Should contain insert directive");
            Assert.That(template2!.Content, Does.Contain("Template 2"), "Should contain template content");
        });
    }

    [Test]
    public async Task CollectSourceFilesAsync_Should_Collect_Mdsrc_Files()
    {
        // Arrange
        var templateFile = Path.Combine(_testDirectory, "template.mdext");
        var sourceFile1 = Path.Combine(_testDirectory, "source1.mdsrc");
        var sourceFile2 = Path.Combine(_testDirectory, "source2.mdsrc");
        var regularFile = Path.Combine(_testDirectory, "readme.md");

        await File.WriteAllTextAsync(templateFile, "# Template\n<insert source1.mdsrc>");
        await File.WriteAllTextAsync(sourceFile1, "Source 1 content");
        await File.WriteAllTextAsync(sourceFile2, "Source 2 content");
        await File.WriteAllTextAsync(regularFile, "Regular markdown");

        // Act
        var result = await _service.CollectSourceFilesAsync(_testDirectory);
        var sourceFiles = result.ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(sourceFiles, Has.Count.EqualTo(2), "Should collect only .mdsrc files");

            var source1 = sourceFiles.FirstOrDefault(s => s.FileName == "source1.mdsrc");
            var source2 = sourceFiles.FirstOrDefault(s => s.FileName == "source2.mdsrc");

            Assert.That(source1, Is.Not.Null, "Should find source1.mdsrc");
            Assert.That(source2, Is.Not.Null, "Should find source2.mdsrc");
            Assert.That(source1!.Content, Is.EqualTo("Source 1 content"), "Should contain source content");
            Assert.That(source2!.Content, Is.EqualTo("Source 2 content"), "Should contain source content");
        });
    }

    [Test]
    public async Task CollectAllMarkdownFilesAsync_Should_Collect_Both_Types()
    {
        // Arrange
        var templateFile = Path.Combine(_testDirectory, "template.mdext");
        var sourceFile = Path.Combine(_testDirectory, "source.mdsrc");
        var regularFile = Path.Combine(_testDirectory, "readme.md");

        await File.WriteAllTextAsync(templateFile, "# Template\n<insert source.mdsrc>");
        await File.WriteAllTextAsync(sourceFile, "Source content");
        await File.WriteAllTextAsync(regularFile, "Regular markdown");

        // Act
        var (templateFiles, sourceFiles) = await _service.CollectAllMarkdownFilesAsync(_testDirectory);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(templateFiles.Count(), Is.EqualTo(1), "Should collect 1 template file");
            Assert.That(sourceFiles.Count(), Is.EqualTo(1), "Should collect 1 source file");

            var template = templateFiles.First();
            var source = sourceFiles.First();

            Assert.That(template.FileName, Is.EqualTo("template.mdext"), "Template should have correct filename");
            Assert.That(source.FileName, Is.EqualTo("source.mdsrc"), "Source should have correct filename");
            Assert.That(template.Content, Does.Contain("Template"), "Template should have correct content");
            Assert.That(source.Content, Is.EqualTo("Source content"), "Source should have correct content");
        });
    }

    [Test]
    public async Task CollectTemplateFilesAsync_Should_Collect_From_Subdirectories()
    {
        // Arrange
        var subDirectory = Path.Combine(_testDirectory, "subdirectory");
        Directory.CreateDirectory(subDirectory);

        var templateFile1 = Path.Combine(_testDirectory, "template1.mdext");
        var templateFile2 = Path.Combine(subDirectory, "template2.mdext");

        await File.WriteAllTextAsync(templateFile1, "Template 1 content");
        await File.WriteAllTextAsync(templateFile2, "Template 2 content");

        // Act
        var result = await _service.CollectTemplateFilesAsync(_testDirectory);
        var templateFiles = result.ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(templateFiles, Has.Count.EqualTo(2), "Should collect files from subdirectories");

            var fileNames = templateFiles.Select(t => t.FileName).ToList();
            Assert.That(fileNames, Contains.Item("template1.mdext"), "Should contain root level file");
            Assert.That(fileNames, Contains.Item("subdirectory/template2.mdext"), "Should contain subdirectory file with relative path");
        });
    }

    [Test]
    public void CollectTemplateFilesAsync_Should_Throw_ArgumentException_For_Null_Directory()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.CollectTemplateFilesAsync(null!));
    }

    [Test]
    public void CollectTemplateFilesAsync_Should_Throw_ArgumentException_For_Empty_Directory()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.CollectTemplateFilesAsync(string.Empty));
    }

    [Test]
    public void CollectTemplateFilesAsync_Should_Throw_DirectoryNotFoundException_For_Nonexistent_Directory()
    {
        // Arrange
        var nonexistentDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await _service.CollectTemplateFilesAsync(nonexistentDirectory));
    }

    [Test]
    public async Task CollectTemplateFilesAsync_Should_Handle_Empty_Directory()
    {
        // Act
        var result = await _service.CollectTemplateFilesAsync(_testDirectory);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(0), "Should return empty collection for directory with no .mdext files");
    }

    [Test]
    public async Task CollectTemplateFilesAsync_Should_Handle_File_Read_Errors_Gracefully()
    {
        // Arrange
        var templateFile = Path.Combine(_testDirectory, "template.mdext");
        await File.WriteAllTextAsync(templateFile, "Template content");

        // Make file unreadable (this test might not work on all platforms)
        File.SetAttributes(templateFile, FileAttributes.ReadOnly);

        // Act
        var result = await _service.CollectTemplateFilesAsync(_testDirectory);
        var templateFiles = result.ToList();

        // Assert - Should still collect the file, but with error handling
        Assert.That(templateFiles, Has.Count.EqualTo(1), "Should still collect files even if read errors occur");
    }

    [Test]
    public async Task CollectSourceFilesAsync_Should_Preserve_Relative_Directory_Paths()
    {
        // Arrange
        var level1Dir = Path.Combine(_testDirectory, "level1");
        var level2Dir = Path.Combine(level1Dir, "level2");
        Directory.CreateDirectory(level2Dir);

        var rootFile = Path.Combine(_testDirectory, "root.mdsrc");
        var level1File = Path.Combine(level1Dir, "level1.mdsrc");
        var level2File = Path.Combine(level2Dir, "level2.mdsrc");

        await File.WriteAllTextAsync(rootFile, "Root content");
        await File.WriteAllTextAsync(level1File, "Level 1 content");
        await File.WriteAllTextAsync(level2File, "Level 2 content");

        // Act
        var result = await _service.CollectSourceFilesAsync(_testDirectory);
        var sourceFiles = result.ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(sourceFiles, Has.Count.EqualTo(3), "Should collect all source files");

            var fileNames = sourceFiles.Select(s => s.FileName).OrderBy(name => name).ToList();

            Assert.That(fileNames[0], Is.EqualTo("level1/level1.mdsrc"), "Should preserve level1 relative path");
            Assert.That(fileNames[1], Is.EqualTo("level1/level2/level2.mdsrc"), "Should preserve level2 relative path");
            Assert.That(fileNames[2], Is.EqualTo("root.mdsrc"), "Should have simple name for root level file");

            // Verify content is still correct
            var level2Source = sourceFiles.FirstOrDefault(s => s.FileName == "level1/level2/level2.mdsrc");
            Assert.That(level2Source?.Content, Is.EqualTo("Level 2 content"), "Should preserve content with relative paths");
        });
    }
}
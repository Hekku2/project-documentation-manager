using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ProjectDocumentationManager.Business.Services;

namespace ProjectDocumentationManager.Business.Tests.Services;

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
    public async Task CollectAllMarkdownFilesAsync_Should_Collect_All_Types()
    {
        // Arrange
        var templateFile = Path.Combine(_testDirectory, "template.mdext");
        var sourceFile = Path.Combine(_testDirectory, "source.mdsrc");
        var regularFile = Path.Combine(_testDirectory, "readme.md");

        await File.WriteAllTextAsync(templateFile, "# Template\n<insert source.mdsrc>");
        await File.WriteAllTextAsync(sourceFile, "Source content");
        await File.WriteAllTextAsync(regularFile, "Regular markdown");

        // Act
        var allFiles = await _service.CollectAllMarkdownFilesAsync(_testDirectory);
        var filesList = allFiles.ToList();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(filesList, Has.Count.EqualTo(3), "Should collect all 3 markdown files");

            var templateFiles = filesList.Where(f => f.FileName.EndsWith(".mdext")).ToList();
            var sourceFiles = filesList.Where(f => f.FileName.EndsWith(".mdsrc")).ToList();
            var markdownFiles = filesList.Where(f => f.FileName.EndsWith(".md")).ToList();

            Assert.That(templateFiles, Has.Count.EqualTo(1), "Should collect 1 template file");
            Assert.That(sourceFiles, Has.Count.EqualTo(1), "Should collect 1 source file");
            Assert.That(markdownFiles, Has.Count.EqualTo(1), "Should collect 1 markdown file");

            var template = templateFiles.First();
            var source = sourceFiles.First();
            var markdown = markdownFiles.First();

            Assert.That(template.FileName, Is.EqualTo("template.mdext"), "Template should have correct filename");
            Assert.That(source.FileName, Is.EqualTo("source.mdsrc"), "Source should have correct filename");
            Assert.That(markdown.FileName, Is.EqualTo("readme.md"), "Markdown should have correct filename");
            Assert.That(template.Content, Does.Contain("Template"), "Template should have correct content");
            Assert.That(source.Content, Is.EqualTo("Source content"), "Source should have correct content");
            Assert.That(markdown.Content, Is.EqualTo("Regular markdown"), "Markdown should have correct content");
        }
    }

    [Test]
    public async Task CollectAllMarkdownFilesAsync_Should_Recurse_And_Be_CaseInsensitive()
    {
        var subDir = Path.Combine(_testDirectory, "Sub");
        Directory.CreateDirectory(subDir);
        var upperMd = Path.Combine(subDir, "UPPER.MD");
        await File.WriteAllTextAsync(upperMd, "Upper");

        var files = (await _service.CollectAllMarkdownFilesAsync(_testDirectory)).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(files.Any(f => f.FileName == Path.Combine("Sub", "UPPER.MD")), Is.True);
            Assert.That(files.Count(f => f.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)), Is.GreaterThanOrEqualTo(1));
        }
    }
}
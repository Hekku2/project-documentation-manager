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

    [Test]
    public async Task CollectAllMarkdownFilesAsync_Should_Set_FilePath_As_Relative_Path()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "docs", "templates");
        Directory.CreateDirectory(subDir);
        
        var templateFile = Path.Combine(subDir, "template.mdext");
        var sourceFile = Path.Combine(_testDirectory, "common.mdsrc");
        
        await File.WriteAllTextAsync(templateFile, "# Template content");
        await File.WriteAllTextAsync(sourceFile, "Common content");

        // Act
        var allFiles = await _service.CollectAllMarkdownFilesAsync(_testDirectory);
        var filesList = allFiles.ToList();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(filesList, Has.Count.EqualTo(2), "Should collect both files");

            var templateDoc = filesList.First(f => f.FileName.EndsWith(".mdext"));
            var sourceDoc = filesList.First(f => f.FileName.EndsWith(".mdsrc"));

            // FilePath should be relative path from the base directory
            Assert.That(templateDoc.FilePath, Is.EqualTo(Path.Combine("docs", "templates", "template.mdext")), 
                "Template FilePath should be relative path from base directory");
            Assert.That(sourceDoc.FilePath, Is.EqualTo("common.mdsrc"), 
                "Source FilePath should be relative path from base directory");

            // FileName should also be relative path (as per current implementation)
            Assert.That(templateDoc.FileName, Is.EqualTo(Path.Combine("docs", "templates", "template.mdext")), 
                "Template FileName should be relative path from base directory");
            Assert.That(sourceDoc.FileName, Is.EqualTo("common.mdsrc"), 
                "Source FileName should be relative path from base directory");

            // FilePath should NOT be absolute paths
            Assert.That(Path.IsPathRooted(templateDoc.FilePath), Is.False, 
                "Template FilePath should not be an absolute path");
            Assert.That(Path.IsPathRooted(sourceDoc.FilePath), Is.False, 
                "Source FilePath should not be an absolute path");
        }
    }

    [Test]
    public async Task CollectAllMarkdownFilesAsync_With_NestedStructure_Should_Maintain_Relative_Paths()
    {
        // Arrange
        var level1Dir = Path.Combine(_testDirectory, "level1");
        var level2Dir = Path.Combine(level1Dir, "level2");
        var level3Dir = Path.Combine(level2Dir, "level3");
        
        Directory.CreateDirectory(level3Dir);
        
        var rootFile = Path.Combine(_testDirectory, "root.md");
        var level1File = Path.Combine(level1Dir, "level1.mdsrc");
        var level2File = Path.Combine(level2Dir, "level2.mdext");
        var level3File = Path.Combine(level3Dir, "level3.md");
        
        await File.WriteAllTextAsync(rootFile, "Root content");
        await File.WriteAllTextAsync(level1File, "Level 1 content");
        await File.WriteAllTextAsync(level2File, "Level 2 content");
        await File.WriteAllTextAsync(level3File, "Level 3 content");

        // Act
        var allFiles = await _service.CollectAllMarkdownFilesAsync(_testDirectory);
        var filesList = allFiles.ToList();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(filesList, Has.Count.EqualTo(4), "Should collect all 4 files");

            foreach (var doc in filesList)
            {
                Assert.That(Path.IsPathRooted(doc.FilePath), Is.False, 
                    $"FilePath '{doc.FilePath}' should not be an absolute path");
                
                // Verify the relative path structure is preserved
                if (doc.FileName.Contains("root.md"))
                {
                    Assert.That(doc.FilePath, Is.EqualTo("root.md"));
                }
                else if (doc.FileName.Contains("level1.mdsrc"))
                {
                    Assert.That(doc.FilePath, Is.EqualTo(Path.Combine("level1", "level1.mdsrc")));
                }
                else if (doc.FileName.Contains("level2.mdext"))
                {
                    Assert.That(doc.FilePath, Is.EqualTo(Path.Combine("level1", "level2", "level2.mdext")));
                }
                else if (doc.FileName.Contains("level3.md"))
                {
                    Assert.That(doc.FilePath, Is.EqualTo(Path.Combine("level1", "level2", "level3", "level3.md")));
                }
            }
        }
    }
}
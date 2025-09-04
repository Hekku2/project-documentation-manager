using Business.Models;
using Business.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Business.Tests.Services;

[TestFixture]
public class MarkdownDocumentFileWriterServiceTests
{
    private ILogger<MarkdownDocumentFileWriterService> _mockLogger;
    private MarkdownDocumentFileWriterService _service;
    private string _testOutputFolder;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = NullLoggerFactory.Instance.CreateLogger<MarkdownDocumentFileWriterService>();
        _service = new MarkdownDocumentFileWriterService(_mockLogger);

        // Create a temporary directory for testing
        _testOutputFolder = Path.Combine(Path.GetTempPath(), "MarkdownDocumentFileWriterTests", Guid.NewGuid().ToString());
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up test directory
        if (Directory.Exists(_testOutputFolder))
        {
            Directory.Delete(_testOutputFolder, recursive: true);
        }
    }

    [Test]
    public void WriteDocumentsToFolderAsync_WithNullDocuments_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.WriteDocumentsToFolderAsync(null!, _testOutputFolder));

        Assert.That(exception.ParamName, Is.EqualTo("documents"));
    }

    [Test]
    public void WriteDocumentsToFolderAsync_WithNullOutputFolder_ThrowsArgumentNullException()
    {
        // Arrange
        var documents = new List<MarkdownDocument>();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.WriteDocumentsToFolderAsync(documents, null!));

        Assert.That(exception.ParamName, Is.EqualTo("outputFolder"));
    }

    [Test]
    public void WriteDocumentsToFolderAsync_WithEmptyOutputFolder_ThrowsArgumentException()
    {
        // Arrange
        var documents = new List<MarkdownDocument>();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(
            () => _service.WriteDocumentsToFolderAsync(documents, ""));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ParamName, Is.EqualTo("outputFolder"));
            Assert.That(exception.Message, Does.Contain("Output folder cannot be empty"));
        });
    }

    [Test]
    public void WriteDocumentsToFolderAsync_WithWhitespaceOutputFolder_ThrowsArgumentException()
    {
        // Arrange
        var documents = new List<MarkdownDocument>();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(
            () => _service.WriteDocumentsToFolderAsync(documents, "   "));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ParamName, Is.EqualTo("outputFolder"));
            Assert.That(exception.Message, Does.Contain("Output folder cannot be empty"));
        });
    }

    [Test]
    public async Task WriteDocumentsToFolderAsync_WithEmptyDocumentsList_CreatesDirectoryButWritesNoFiles()
    {
        // Arrange
        var documents = new List<MarkdownDocument>();

        // Act
        await _service.WriteDocumentsToFolderAsync(documents, _testOutputFolder);

        // Assert
        Assert.That(Directory.Exists(_testOutputFolder), Is.True, "Output directory should be created");

        var files = Directory.GetFiles(_testOutputFolder);
        Assert.That(files, Is.Empty, "No files should be written");
    }

    [Test]
    public async Task WriteDocumentsToFolderAsync_WithSingleDocument_WritesFileCorrectly()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "test.md", FilePath = "/test/test.md", Content = "# Test Content\n\nThis is a test document." }
        };

        // Act
        await _service.WriteDocumentsToFolderAsync(documents, _testOutputFolder);

        // Assert
        var filePath = Path.Combine(_testOutputFolder, "test.md");
        Assert.That(File.Exists(filePath), Is.True, "File should be created");

        var content = await File.ReadAllTextAsync(filePath);
        Assert.That(content, Is.EqualTo("# Test Content\n\nThis is a test document."));
    }

    [Test]
    public async Task WriteDocumentsToFolderAsync_WithMultipleDocuments_WritesAllFilesCorrectly()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "doc1.md", FilePath = "/test/doc1.md", Content = "# Document 1\n\nFirst document content." },
            new() { FileName = "doc2.md", FilePath = "/test/doc2.md", Content = "# Document 2\n\nSecond document content." },
            new() { FileName = "doc3.md", FilePath = "/test/doc3.md", Content = "# Document 3\n\nThird document content." }
        };

        // Act
        await _service.WriteDocumentsToFolderAsync(documents, _testOutputFolder);

        // Assert
        Assert.That(Directory.Exists(_testOutputFolder), Is.True, "Output directory should exist");

        var files = Directory.GetFiles(_testOutputFolder).OrderBy(f => f).ToArray();
        Assert.That(files, Has.Length.EqualTo(3), "Three files should be written");

        // Verify each file content
        var doc1Content = await File.ReadAllTextAsync(Path.Combine(_testOutputFolder, "doc1.md"));
        Assert.That(doc1Content, Is.EqualTo("# Document 1\n\nFirst document content."));

        var doc2Content = await File.ReadAllTextAsync(Path.Combine(_testOutputFolder, "doc2.md"));
        Assert.That(doc2Content, Is.EqualTo("# Document 2\n\nSecond document content."));

        var doc3Content = await File.ReadAllTextAsync(Path.Combine(_testOutputFolder, "doc3.md"));
        Assert.That(doc3Content, Is.EqualTo("# Document 3\n\nThird document content."));
    }

    [Test]
    public async Task WriteDocumentsToFolderAsync_WithNullContent_WritesEmptyFile()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "empty.md", FilePath = "/test/empty.md", Content = null! }
        };

        // Act
        await _service.WriteDocumentsToFolderAsync(documents, _testOutputFolder);

        // Assert
        var filePath = Path.Combine(_testOutputFolder, "empty.md");
        Assert.That(File.Exists(filePath), Is.True, "File should be created");

        var content = await File.ReadAllTextAsync(filePath);
        Assert.That(content, Is.EqualTo(string.Empty), "File should be empty");
    }

    [Test]
    public async Task WriteDocumentsToFolderAsync_WithEmptyContent_WritesEmptyFile()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "empty.md", FilePath = "/test/empty.md", Content = "" }
        };

        // Act
        await _service.WriteDocumentsToFolderAsync(documents, _testOutputFolder);

        // Assert
        var filePath = Path.Combine(_testOutputFolder, "empty.md");
        Assert.That(File.Exists(filePath), Is.True, "File should be created");

        var content = await File.ReadAllTextAsync(filePath);
        Assert.That(content, Is.EqualTo(string.Empty), "File should be empty");
    }

    [Test]
    public async Task WriteDocumentsToFolderAsync_WithDocumentsHavingEmptyFilenames_SkipsThoseDocuments()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "valid.md", FilePath = "/test/valid.md", Content = "Valid content" },
            new() { FileName = "", FilePath = "/test/", Content = "Empty filename content" },
            new() { FileName = "   ", FilePath = "/test/   ", Content = "Whitespace filename content" },
            new() { FileName = "another-valid.md", FilePath = "/test/another-valid.md", Content = "Another valid content" }
        };

        // Act
        await _service.WriteDocumentsToFolderAsync(documents, _testOutputFolder);

        // Assert
        var files = Directory.GetFiles(_testOutputFolder).OrderBy(f => f).ToArray();
        Assert.That(files, Has.Length.EqualTo(2), "Only files with valid names should be written");

        var validFileContent = await File.ReadAllTextAsync(Path.Combine(_testOutputFolder, "valid.md"));
        Assert.That(validFileContent, Is.EqualTo("Valid content"));

        var anotherValidFileContent = await File.ReadAllTextAsync(Path.Combine(_testOutputFolder, "another-valid.md"));
        Assert.That(anotherValidFileContent, Is.EqualTo("Another valid content"));
    }

    [Test]
    public async Task WriteDocumentsToFolderAsync_WithNonExistentDirectory_CreatesDirectoryAndWritesFiles()
    {
        // Arrange
        var nestedPath = Path.Combine(_testOutputFolder, "nested", "deeply", "nested", "folder");
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "test.md", FilePath = "/test/test.md", Content = "Test content in nested folder" }
        };

        // Act
        await _service.WriteDocumentsToFolderAsync(documents, nestedPath);

        // Assert
        Assert.That(Directory.Exists(nestedPath), Is.True, "Nested directory should be created");

        var filePath = Path.Combine(nestedPath, "test.md");
        Assert.That(File.Exists(filePath), Is.True, "File should be created in nested directory");

        var content = await File.ReadAllTextAsync(filePath);
        Assert.That(content, Is.EqualTo("Test content in nested folder"));
    }

    [Test]
    public async Task WriteDocumentsToFolderAsync_WithExistingDirectory_OverwritesFiles()
    {
        // Arrange
        Directory.CreateDirectory(_testOutputFolder);
        var existingFilePath = Path.Combine(_testOutputFolder, "existing.md");
        await File.WriteAllTextAsync(existingFilePath, "Original content");

        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "existing.md", FilePath = "/test/existing.md", Content = "Updated content" }
        };

        // Act
        await _service.WriteDocumentsToFolderAsync(documents, _testOutputFolder);

        // Assert
        var content = await File.ReadAllTextAsync(existingFilePath);
        Assert.That(content, Is.EqualTo("Updated content"), "File should be overwritten");
    }

    [Test]
    public async Task WriteDocumentsToFolderAsync_WithSpecialCharactersInContent_WritesCorrectly()
    {
        // Arrange
        var specialContent = "# Special Characters\n\n" +
                           "Unicode: 🚀 💻 📝\n" +
                           "Accents: café résumé naïve\n" +
                           "Symbols: @#$%^&*()[]{}|\\:;\"'<>?/\n" +
                           "Line breaks and tabs:\tTabbed\n\nNew paragraph";

        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "special.md", FilePath = "/test/special.md", Content = specialContent }
        };

        // Act
        await _service.WriteDocumentsToFolderAsync(documents, _testOutputFolder);

        // Assert
        var filePath = Path.Combine(_testOutputFolder, "special.md");
        var content = await File.ReadAllTextAsync(filePath);
        Assert.That(content, Is.EqualTo(specialContent));
    }
}
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Desktop.Services;
using Desktop.Configuration;

namespace Desktop.UITests.Services;

[TestFixture]
public class FileServiceTests
{
    private FileService _fileService;
    private ApplicationOptions _options;

    [SetUp]
    public void Setup()
    {
        _options = new ApplicationOptions
        {
            DefaultProjectFolder = Path.Combine(Path.GetTempPath(), "FileServiceTests", Guid.NewGuid().ToString())
        };
        
        Directory.CreateDirectory(_options.DefaultProjectFolder);
        
        var optionsWrapper = Options.Create(_options);
        var logger = NullLoggerFactory.Instance.CreateLogger<FileService>();
        _fileService = new FileService(logger, optionsWrapper);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_options.DefaultProjectFolder))
        {
            Directory.Delete(_options.DefaultProjectFolder, true);
        }
    }

    [Test]
    public async Task CreateFileAsync_WithValidFolderPath_CreatesFileSuccessfully()
    {
        // Arrange
        var fileName = "newfile.md";
        var expectedFilePath = Path.Combine(_options.DefaultProjectFolder, fileName);

        // Act
        var result = await _fileService.CreateFileAsync(_options.DefaultProjectFolder, fileName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(File.Exists(expectedFilePath), Is.True);
        });
    }


    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task CreateFileAsync_WithInvalidFolderPath_ReturnsFalse(string? folderPath)
    {
        var result = await _fileService.CreateFileAsync(folderPath!, "test.md");
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CreateFileAsync_WithNullFileName_ReturnsFalse()
    {
        // Act
        var result = await _fileService.CreateFileAsync(_options.DefaultProjectFolder, null!);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CreateFileAsync_WithEmptyFileName_ReturnsFalse()
    {
        // Act
        var result = await _fileService.CreateFileAsync(_options.DefaultProjectFolder, "");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CreateFileAsync_WithWhitespaceFileName_ReturnsFalse()
    {
        // Act
        var result = await _fileService.CreateFileAsync(_options.DefaultProjectFolder, "   ");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CreateFileAsync_WithNonExistentFolder_ReturnsFalse()
    {
        // Arrange
        var nonExistentFolder = Path.Combine(Path.GetTempPath(), "NonExistentFolder", Guid.NewGuid().ToString());

        // Act
        var result = await _fileService.CreateFileAsync(nonExistentFolder, "test.md");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(Directory.Exists(nonExistentFolder), Is.False);
        });
    }

    [Test]
    public async Task CreateFileAsync_WhenFileAlreadyExists_ReturnsFalse()
    {
        // Arrange
        var fileName = "existing.md";
        var filePath = Path.Combine(_options.DefaultProjectFolder, fileName);
        await File.WriteAllTextAsync(filePath, "existing content");

        // Act
        var result = await _fileService.CreateFileAsync(_options.DefaultProjectFolder, fileName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            // Verify original content is preserved
            var content = File.ReadAllText(filePath);
            Assert.That(content, Is.EqualTo("existing content"));
        });
    }

    [Test]
    public async Task CreateFileAsync_CreatedFileHasEmptyContent()
    {
        // Arrange
        var fileName = "test.md";
        var filePath = Path.Combine(_options.DefaultProjectFolder, fileName);

        // Act
        var result = await _fileService.CreateFileAsync(_options.DefaultProjectFolder, fileName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(new FileInfo(filePath).Length, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task CreateFileAsync_WithSubfolder_CreatesFileInSubfolder()
    {
        // Arrange
        var subfolderPath = Path.Combine(_options.DefaultProjectFolder, "subfolder");
        Directory.CreateDirectory(subfolderPath);
        var fileName = "subfile.md";
        var expectedFilePath = Path.Combine(subfolderPath, fileName);

        // Act
        var result = await _fileService.CreateFileAsync(subfolderPath, fileName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(File.Exists(expectedFilePath), Is.True);
        });
    }

    [Test]
    public async Task CreateFileAsync_WithCustomExtension_CreatesFileWithCorrectExtension()
    {
        // Arrange
        var fileName = "custom.txt";
        var expectedFilePath = Path.Combine(_options.DefaultProjectFolder, fileName);

        // Act
        var result = await _fileService.CreateFileAsync(_options.DefaultProjectFolder, fileName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(File.Exists(expectedFilePath), Is.True);
        });
    }
}
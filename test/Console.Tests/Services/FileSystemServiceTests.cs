using MarkdownCompiler.Console.Services;

namespace MarkdownCompiler.Console.Tests.Services;

[TestFixture]
public class FileSystemServiceTests
{
    private FileSystemService _service = null!;
    private string _testDirectory = null!;
    private string _nonExistentDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _service = new FileSystemService();
        _testDirectory = Path.Combine(Path.GetTempPath(), "FileSystemServiceTests", Guid.NewGuid().ToString());
        _nonExistentDirectory = Path.Combine(Path.GetTempPath(), "NonExistent", Guid.NewGuid().ToString());
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }

        // Clean up the parent test directory if it exists and is empty
        var parentTestDir = Path.Combine(Path.GetTempPath(), "FileSystemServiceTests");
        if (Directory.Exists(parentTestDir) && !Directory.EnumerateFileSystemEntries(parentTestDir).Any())
        {
            Directory.Delete(parentTestDir);
        }
    }

    [Test]
    public void DirectoryExists_Should_Return_True_For_Existing_Directory()
    {
        Directory.CreateDirectory(_testDirectory);

        var result = _service.DirectoryExists(_testDirectory);

        Assert.That(result, Is.True);
    }

    [Test]
    public void DirectoryExists_Should_Return_False_For_NonExistent_Directory()
    {
        var result = _service.DirectoryExists(_nonExistentDirectory);

        Assert.That(result, Is.False);
    }

    [Test]
    public void EnsureDirectoryExists_Should_Create_Directory_When_It_Does_Not_Exist()
    {
        _service.EnsureDirectoryExists(_testDirectory);

        Assert.That(Directory.Exists(_testDirectory), Is.True);
    }

    [Test]
    public void EnsureDirectoryExists_Should_Not_Throw_When_Directory_Already_Exists()
    {
        Directory.CreateDirectory(_testDirectory);

        Assert.DoesNotThrow(() => _service.EnsureDirectoryExists(_testDirectory));
        Assert.That(Directory.Exists(_testDirectory), Is.True);
    }

    [Test]
    public void EnsureDirectoryExists_Should_Create_Nested_Directory_Structure()
    {
        var nestedPath = Path.Combine(_testDirectory, "level1", "level2", "level3");

        _service.EnsureDirectoryExists(nestedPath);

        Assert.That(Directory.Exists(nestedPath), Is.True);
    }

    [Test]
    public void GetFullPath_Should_Return_Absolute_Path_For_Relative_Path()
    {
        var relativePath = "relative/path";

        var result = _service.GetFullPath(relativePath);

        Assert.Multiple(() =>
        {
            Assert.That(Path.IsPathRooted(result), Is.True);
            Assert.That(result, Does.EndWith("relative/path").Or.EndWith("relative\\path"));
        });
    }

    [Test]
    public void GetFullPath_Should_Return_Same_Path_For_Already_Absolute_Path()
    {
        var absolutePath = Path.GetFullPath(_testDirectory);

        var result = _service.GetFullPath(absolutePath);

        Assert.That(result, Is.EqualTo(absolutePath));
    }

    [Test]
    public void GetFullPath_Should_Normalize_Path_Separators()
    {
        var pathWithMixedSeparators = "some/path\\with/mixed\\separators";

        var result = _service.GetFullPath(pathWithMixedSeparators);

        Assert.Multiple(() =>
        {
            Assert.That(Path.IsPathRooted(result), Is.True);
            // The result should use the OS-appropriate directory separator
            Assert.That(result, Does.Contain(Path.DirectorySeparatorChar.ToString()));
        });
    }

    [Test]
    public void GetFullPath_Should_Handle_Current_Directory_Reference()
    {
        var currentDirPath = ".";

        var result = _service.GetFullPath(currentDirPath);

        Assert.Multiple(() =>
        {
            Assert.That(Path.IsPathRooted(result), Is.True);
            Assert.That(result, Is.EqualTo(Directory.GetCurrentDirectory()));
        });
    }

    [Test]
    public void GetFullPath_Should_Handle_Parent_Directory_Reference()
    {
        var parentDirPath = "..";

        var result = _service.GetFullPath(parentDirPath);

        Assert.Multiple(() =>
        {
            Assert.That(Path.IsPathRooted(result), Is.True);
            Assert.That(result, Is.EqualTo(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName));
        });
    }
}
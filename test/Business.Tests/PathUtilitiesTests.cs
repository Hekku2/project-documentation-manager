using NUnit.Framework;

namespace ProjectDocumentationManager.Business.Tests;

[TestFixture]
public class PathUtilitiesTests
{
    [Test]
    public void NormalizePathKey_WithNull_ReturnsEmptyString()
    {
        // Act
        var result = PathUtilities.NormalizePathKey(null!);

        // Assert
        Assert.That(result, Is.EqualTo(""));
    }

    [TestCase("", ExpectedResult = "")]
    [TestCase("   ", ExpectedResult = "")]
    [TestCase("\t\n", ExpectedResult = "")]
    public string NormalizePathKey_WithWhitespace_ReturnsEmptyString(string input)
    {
        // Act & Assert
        return PathUtilities.NormalizePathKey(input);
    }

    [TestCase("file.txt", ExpectedResult = "file.txt")]
    [TestCase("document.md", ExpectedResult = "document.md")]
    [TestCase("template.mdext", ExpectedResult = "template.mdext")]
    [TestCase("source.mdsrc", ExpectedResult = "source.mdsrc")]
    public string NormalizePathKey_WithSimpleFilename_ReturnsUnchanged(string filename)
    {
        // Act & Assert
        return PathUtilities.NormalizePathKey(filename);
    }

    [Test]
    public void NormalizePathKey_WithUnixStylePath_ReturnsOnlyFilename()
    {
        // Arrange
        var unixPath = "/home/user/documents/file.md";

        // Act
        var result = PathUtilities.NormalizePathKey(unixPath);

        // Assert
        Assert.That(result, Is.EqualTo("file.md"));
    }

    [Test]
    public void NormalizePathKey_WithWindowsStylePath_ReturnsOnlyFilename()
    {
        // Arrange
        var windowsPath = @"C:\Users\User\Documents\file.md";

        // Act
        var result = PathUtilities.NormalizePathKey(windowsPath);

        // Assert
        Assert.That(result, Is.EqualTo("file.md"));
    }

    [Test]
    public void NormalizePathKey_WithMixedSeparators_ReturnsOnlyFilename()
    {
        // Arrange
        var mixedPath = @"C:\Users/User\Documents/file.md";

        // Act
        var result = PathUtilities.NormalizePathKey(mixedPath);

        // Assert
        Assert.That(result, Is.EqualTo("file.md"));
    }

    [Test]
    public void NormalizePathKey_WithRelativePath_ReturnsOnlyFilename()
    {
        // Arrange
        var relativePath = Path.Combine("subfolder", "nested", "file.md");

        // Act
        var result = PathUtilities.NormalizePathKey(relativePath);

        // Assert
        Assert.That(result, Is.EqualTo("file.md"));
    }

    [Test]
    public void NormalizePathKey_WithTrailingSlash_ReturnsEmpty()
    {
        // Arrange
        var pathWithTrailingSlash = "/home/user/documents/";

        // Act
        var result = PathUtilities.NormalizePathKey(pathWithTrailingSlash);

        // Assert
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void NormalizePathKey_WithComplexFilename_PreservesFilenameCharacters()
    {
        // Arrange
        var complexPath = @"/home/user/My Documents/file-name_v2.final.md";

        // Act
        var result = PathUtilities.NormalizePathKey(complexPath);

        // Assert
        Assert.That(result, Is.EqualTo("file-name_v2.final.md"));
    }

    [Test]
    public void NormalizePathKey_WithUnicodeCharacters_PreservesUnicode()
    {
        // Arrange
        var unicodePath = @"/home/user/documents/файл.md";

        // Act
        var result = PathUtilities.NormalizePathKey(unicodePath);

        // Assert
        Assert.That(result, Is.EqualTo("файл.md"));
    }

    [Test]
    public void NormalizePathKey_WithSpacesInFilename_PreservesSpaces()
    {
        // Arrange
        var pathWithSpaces = @"/home/user/documents/my file name.md";

        // Act
        var result = PathUtilities.NormalizePathKey(pathWithSpaces);

        // Assert
        Assert.That(result, Is.EqualTo("my file name.md"));
    }

    [Test]
    public void NormalizePathKey_ConsistentBehaviorAcrossPlatforms()
    {
        // Arrange
        var testCases = new[]
        {
            (@"C:\folder\file.md", "file.md"),
            ("/folder/file.md", "file.md"),
            (@"C:\folder/subfolder\file.md", "file.md"),
            (Path.Combine("relative", "path", "file.md"), "file.md"),
            ("just-filename.md", "just-filename.md")
        };

        foreach (var (input, expected) in testCases)
        {
            // Act
            var result = PathUtilities.NormalizePathKey(input);

            // Assert
            Assert.That(result, Is.EqualTo(expected), $"Failed for input: {input}");
        }
    }

    [Test]
    public void FilePathComparer_IsOrdinalIgnoreCase()
    {
        // Assert
        Assert.That(PathUtilities.FilePathComparer, Is.EqualTo(StringComparer.OrdinalIgnoreCase));
    }

    [Test]
    public void FilePathComparison_IsOrdinalIgnoreCase()
    {
        // Assert
        Assert.That(PathUtilities.FilePathComparison, Is.EqualTo(StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void FilePathComparer_PerformsCaseInsensitiveComparison()
    {
        // Arrange
        var comparer = PathUtilities.FilePathComparer;

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(comparer.Equals("FILE.MD", "file.md"), Is.True);
            Assert.That(comparer.Equals("Template.MDEXT", "template.mdext"), Is.True);
            Assert.That(comparer.Equals("source.MDSRC", "SOURCE.mdsrc"), Is.True);
            Assert.That(comparer.Equals("different.md", "other.md"), Is.False);
        }
    }

    [Test]
    public void FilePathComparison_PerformsCaseInsensitiveComparison()
    {
        // Arrange
        var comparison = PathUtilities.FilePathComparison;

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That("FILE.MD".Equals("file.md", comparison), Is.True);
            Assert.That("Template.MDEXT".Equals("template.mdext", comparison), Is.True);
            Assert.That("source.MDSRC".Equals("SOURCE.mdsrc", comparison), Is.True);
            Assert.That("different.md".Equals("other.md", comparison), Is.False);
        }
    }
}
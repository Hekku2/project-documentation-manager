using NUnit.Framework;

namespace MarkdownCompiler.Console.Tests;

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
    public void NormalizePathKey_WithUnixStylePath_PreservesFullPath()
    {
        // Arrange
        var unixPath = "/home/user/documents/file.md";

        // Act
        var result = PathUtilities.NormalizePathKey(unixPath);

        // Assert
        Assert.That(result, Is.EqualTo("/home/user/documents/file.md"));
    }

    [Test]
    public void NormalizePathKey_WithWindowsStylePath_NormalizesToPlatformSeparators()
    {
        // Arrange
        var windowsPath = @"C:\Users\User\Documents\file.md";

        // Act
        var result = PathUtilities.NormalizePathKey(windowsPath);

        // Assert - Should normalize separators but preserve full path
        var expectedPath = $"C:{Path.DirectorySeparatorChar}Users{Path.DirectorySeparatorChar}User{Path.DirectorySeparatorChar}Documents{Path.DirectorySeparatorChar}file.md";
        Assert.That(result, Is.EqualTo(expectedPath));
    }

    [Test]
    public void NormalizePathKey_WithMixedSeparators_NormalizesToPlatformSeparators()
    {
        // Arrange
        var mixedPath = @"C:\Users/User\Documents/file.md";

        // Act
        var result = PathUtilities.NormalizePathKey(mixedPath);

        // Assert - Should normalize all separators to platform-specific
        var expectedPath = $"C:{Path.DirectorySeparatorChar}Users{Path.DirectorySeparatorChar}User{Path.DirectorySeparatorChar}Documents{Path.DirectorySeparatorChar}file.md";
        Assert.That(result, Is.EqualTo(expectedPath));
    }

    [Test]
    public void NormalizePathKey_WithRelativePath_PreservesRelativePath()
    {
        // Arrange
        var relativePath = Path.Combine("subfolder", "nested", "file.md");

        // Act
        var result = PathUtilities.NormalizePathKey(relativePath);

        // Assert - Should preserve the relative path structure
        Assert.That(result, Is.EqualTo(relativePath));
    }

    [Test]
    public void NormalizePathKey_WithTrailingSlash_PreservesPath()
    {
        // Arrange
        var pathWithTrailingSlash = "/home/user/documents/";

        // Act
        var result = PathUtilities.NormalizePathKey(pathWithTrailingSlash);

        // Assert - Should preserve the path with normalized separators
        var expectedPath = $"/home/user/documents/";
        Assert.That(result, Is.EqualTo(expectedPath));
    }

    [Test]
    public void NormalizePathKey_WithComplexPath_PreservesFullPath()
    {
        // Arrange
        var complexPath = @"/home/user/My Documents/file-name_v2.final.md";

        // Act
        var result = PathUtilities.NormalizePathKey(complexPath);

        // Assert
        Assert.That(result, Is.EqualTo("/home/user/My Documents/file-name_v2.final.md"));
    }

    [Test]
    public void NormalizePathKey_WithUnicodeCharacters_PreservesFullPath()
    {
        // Arrange
        var unicodePath = @"/home/user/documents/файл.md";

        // Act
        var result = PathUtilities.NormalizePathKey(unicodePath);

        // Assert
        Assert.That(result, Is.EqualTo("/home/user/documents/файл.md"));
    }

    [Test]
    public void NormalizePathKey_WithSpacesInPath_PreservesFullPath()
    {
        // Arrange
        var pathWithSpaces = @"/home/user/documents/my file name.md";

        // Act
        var result = PathUtilities.NormalizePathKey(pathWithSpaces);

        // Assert
        Assert.That(result, Is.EqualTo("/home/user/documents/my file name.md"));
    }

    [Test]
    public void NormalizePathKey_ConsistentSeparatorNormalization()
    {
        // Arrange
        var testCases = new[]
        {
            (@"C:\folder\file.md", $"C:{Path.DirectorySeparatorChar}folder{Path.DirectorySeparatorChar}file.md"),
            ("/folder/file.md", "/folder/file.md"),
            (@"C:\folder/subfolder\file.md", $"C:{Path.DirectorySeparatorChar}folder{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}file.md"),
            (Path.Combine("relative", "path", "file.md"), Path.Combine("relative", "path", "file.md")),
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
using Business.Models;
using NUnit.Framework;

namespace Desktop.UITests;

[TestFixture]
public class ValidationIssueExtensionsTests
{
    [Test]
    public void IsFromFile_WithMatchingFileName_ReturnsTrue()
    {
        // Arrange
        var issue = new ValidationIssue { Message = "Error in current file", SourceFile = "file1.mdext", LineNumber = 5 };
        
        // Act & Assert
        Assert.That(issue.IsFromFile("file1.mdext"), Is.True, "Should return true for matching filename");
    }
    
    [Test]
    public void IsFromFile_WithDifferentFileName_ReturnsFalse()
    {
        // Arrange
        var issue = new ValidationIssue { Message = "Error in other file", SourceFile = "file2.mdext", LineNumber = 3 };
        
        // Act & Assert
        Assert.That(issue.IsFromFile("file1.mdext"), Is.False, "Should return false for different filename");
    }
    
    [Test]
    public void IsFromFile_WithoutSourceFile_ReturnsFalse()
    {
        // Arrange
        var issue = new ValidationIssue { Message = "Generic error without filename", LineNumber = 1 };
        
        // Act & Assert
        Assert.That(issue.IsFromFile("file1.mdext"), Is.False, "Should return false for issue without source file");
    }
    
    [Test]
    public void IsFromFile_WithNullFileName_ReturnsFalse()
    {
        // Arrange
        var issueWithSource = new ValidationIssue { Message = "Error in file 1", SourceFile = "file1.mdext", LineNumber = 5 };
        var issueWithoutSource = new ValidationIssue { Message = "Generic error", LineNumber = 1 };
        
        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(issueWithSource.IsFromFile(null), Is.False, "Should return false for issue with source when filename is null");
            Assert.That(issueWithoutSource.IsFromFile(null), Is.False, "Should return false for issue without source when filename is null");
        });
    }
    
    [Test]
    public void IsFromFile_WithEmptyFileName_ReturnsFalse()
    {
        // Arrange
        var issueWithSource = new ValidationIssue { Message = "Error in file 1", SourceFile = "file1.mdext", LineNumber = 5 };
        var issueWithoutSource = new ValidationIssue { Message = "Generic error", LineNumber = 1 };
        
        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(issueWithSource.IsFromFile(""), Is.False, "Should return false for issue with source when filename is empty");
            Assert.That(issueWithoutSource.IsFromFile(""), Is.False, "Should return false for issue without source when filename is empty");
        });
    }
    
    [Test]
    public void IsFromFile_WithCaseInsensitiveMatching_ReturnsTrue()
    {
        // Arrange
        var errorLowerCase = new ValidationIssue { Message = "Error with lowercase filename", SourceFile = "file1.mdext", LineNumber = 5 };
        var errorUpperCase = new ValidationIssue { Message = "Error with uppercase filename", SourceFile = "FILE1.MDEXT", LineNumber = 3 };
        
        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(errorLowerCase.IsFromFile("File1.MDEXT"), Is.True, "Should match lowercase filename case-insensitively");
            Assert.That(errorUpperCase.IsFromFile("file1.mdext"), Is.True, "Should match uppercase filename case-insensitively");
        });
    }

    [Test]
    public void IsFromFile_WithRelativeAndAbsolutePaths_ReturnsTrue()
    {
        // Arrange - Use relative path in SourceFile and absolute path in comparison
        var issueWithRelativePath = new ValidationIssue 
        { 
            Message = "Error in relative path", 
            SourceFile = "test.mdext", 
            LineNumber = 2 
        };
        
        var currentDirectory = System.IO.Directory.GetCurrentDirectory();
        var absolutePath = System.IO.Path.Combine(currentDirectory, "test.mdext");
        
        // Act & Assert
        Assert.That(issueWithRelativePath.IsFromFile(absolutePath), Is.True, 
            "Should match relative path against absolute path");
    }

    [Test]
    public void IsFromFile_WithDifferentPathSeparators_ReturnsTrue()
    {
        // Arrange - Test with different path separators (mainly for cross-platform compatibility)
        var issueWithForwardSlash = new ValidationIssue 
        { 
            Message = "Error with forward slash", 
            SourceFile = "/test/path/file.mdext", 
            LineNumber = 2 
        };
        
        // On Windows, this might be normalized to backslashes, on Unix it stays as forward slashes
        var normalizedPath = System.IO.Path.GetFullPath("/test/path/file.mdext");
        
        // Act & Assert
        Assert.That(issueWithForwardSlash.IsFromFile(normalizedPath), Is.True, 
            "Should match paths with different separators after normalization");
    }

    [Test]
    public void IsFromFile_WithInvalidPaths_FallsBackToStringComparison()
    {
        // Arrange - Use invalid path characters that would cause Path.GetFullPath to throw
        var issueWithInvalidPath = new ValidationIssue 
        { 
            Message = "Error with invalid path", 
            SourceFile = "file<>.mdext", // Contains invalid characters
            LineNumber = 2 
        };
        
        // Act & Assert
        Assert.Multiple(() =>
        {
            // Should match exactly when path resolution fails
            Assert.That(issueWithInvalidPath.IsFromFile("file<>.mdext"), Is.True, 
                "Should fall back to string comparison for invalid paths");
            
            // Should not match different strings when path resolution fails
            Assert.That(issueWithInvalidPath.IsFromFile("different.mdext"), Is.False, 
                "Should not match different strings when path resolution fails");
        });
    }

    [Test]
    public void IsFromFile_WithNormalizedPaths_ReturnsTrue()
    {
        // Arrange - Test with paths that have redundant separators and . or .. components
        var issueWithUnnormalizedPath = new ValidationIssue 
        { 
            Message = "Error with unnormalized path", 
            SourceFile = "/test/./path/../path/file.mdext", 
            LineNumber = 2 
        };
        
        var normalizedPath = "/test/path/file.mdext";
        
        // Act & Assert
        Assert.That(issueWithUnnormalizedPath.IsFromFile(normalizedPath), Is.True, 
            "Should match unnormalized path against normalized path");
    }
}

[TestFixture]
public class ValidationResultExtensionsTests
{
    [Test]
    public void GetErrorsForFile_WithMatchingErrors_ReturnsFilteredList()
    {
        // Arrange
        var validationResult = new ValidationResult
        {
            Errors = new List<ValidationIssue>
            {
                new ValidationIssue { Message = "Error 1", SourceFile = "file1.mdext", LineNumber = 1 },
                new ValidationIssue { Message = "Error 2", SourceFile = "file2.mdext", LineNumber = 2 },
                new ValidationIssue { Message = "Error 3", SourceFile = "file1.mdext", LineNumber = 3 },
                new ValidationIssue { Message = "Error 4", SourceFile = null, LineNumber = 4 }
            }
        };

        // Act
        var filteredErrors = validationResult.GetErrorsForFile("file1.mdext");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(filteredErrors, Has.Count.EqualTo(2), "Should return only errors for the specified file");
            Assert.That(filteredErrors[0].Message, Is.EqualTo("Error 1"), "First error should match");
            Assert.That(filteredErrors[1].Message, Is.EqualTo("Error 3"), "Second error should match");
        });
    }

    [Test]
    public void GetErrorsForFile_WithNoMatchingErrors_ReturnsEmptyList()
    {
        // Arrange
        var validationResult = new ValidationResult
        {
            Errors = new List<ValidationIssue>
            {
                new ValidationIssue { Message = "Error 1", SourceFile = "file2.mdext", LineNumber = 1 },
                new ValidationIssue { Message = "Error 2", SourceFile = "file3.mdext", LineNumber = 2 }
            }
        };

        // Act
        var filteredErrors = validationResult.GetErrorsForFile("file1.mdext");

        // Assert
        Assert.That(filteredErrors, Is.Empty, "Should return empty list when no errors match the file");
    }

    [Test]
    public void GetErrorsForFile_WithNullValidationResult_ReturnsEmptyList()
    {
        // Arrange
        ValidationResult? validationResult = null;

        // Act
        var filteredErrors = validationResult!.GetErrorsForFile("file1.mdext");

        // Assert
        Assert.That(filteredErrors, Is.Empty, "Should return empty list for null validation result");
    }

    [Test]
    public void GetErrorsForFile_WithNullFileName_ReturnsEmptyList()
    {
        // Arrange
        var validationResult = new ValidationResult
        {
            Errors = new List<ValidationIssue>
            {
                new ValidationIssue { Message = "Error 1", SourceFile = "file1.mdext", LineNumber = 1 },
                new ValidationIssue { Message = "Error 2", SourceFile = "file2.mdext", LineNumber = 2 }
            }
        };

        // Act
        var filteredErrors = validationResult.GetErrorsForFile(null);

        // Assert
        Assert.That(filteredErrors, Is.Empty, "Should return empty list when filename is null");
    }
}
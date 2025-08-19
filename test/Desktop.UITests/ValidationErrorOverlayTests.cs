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
    public void IsFromFile_WithoutSourceFile_ReturnsTrue()
    {
        // Arrange
        var issue = new ValidationIssue { Message = "Generic error without filename", LineNumber = 1 };
        
        // Act & Assert
        Assert.That(issue.IsFromFile("file1.mdext"), Is.True, "Should return true for issue without source file");
    }
    
    [Test]
    public void IsFromFile_WithNullFileName_ReturnsTrue()
    {
        // Arrange
        var issueWithSource = new ValidationIssue { Message = "Error in file 1", SourceFile = "file1.mdext", LineNumber = 5 };
        var issueWithoutSource = new ValidationIssue { Message = "Generic error", LineNumber = 1 };
        
        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(issueWithSource.IsFromFile(null), Is.True, "Should return true for issue with source when filename is null");
            Assert.That(issueWithoutSource.IsFromFile(null), Is.True, "Should return true for issue without source when filename is null");
        });
    }
    
    [Test]
    public void IsFromFile_WithEmptyFileName_ReturnsTrue()
    {
        // Arrange
        var issueWithSource = new ValidationIssue { Message = "Error in file 1", SourceFile = "file1.mdext", LineNumber = 5 };
        var issueWithoutSource = new ValidationIssue { Message = "Generic error", LineNumber = 1 };
        
        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(issueWithSource.IsFromFile(""), Is.True, "Should return true for issue with source when filename is empty");
            Assert.That(issueWithoutSource.IsFromFile(""), Is.True, "Should return true for issue without source when filename is empty");
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
}
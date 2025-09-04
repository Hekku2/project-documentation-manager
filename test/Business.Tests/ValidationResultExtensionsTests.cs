using Business.Models;
using NUnit.Framework;

namespace Business.Tests;

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
        // Act
        var filteredErrors = ValidationResultExtensions.GetErrorsForFile(null, "file1.mdext");

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

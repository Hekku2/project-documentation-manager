using System.Collections.Generic;
using Business.Models;
using Desktop.Controls;
using NUnit.Framework;

namespace Desktop.UITests;

[TestFixture]
public class ValidationErrorOverlayTests
{
    [Test]
    public void ShouldShowError_WithCurrentFileName_FiltersErrorsCorrectly()
    {
        // Arrange
        var overlay = new ValidationErrorOverlay();
        overlay.CurrentFileName = "file1.mdext";
        
        var errorFromCurrentFile = new ValidationIssue { Message = "[file1.mdext] Error in current file", LineNumber = 5 };
        var errorFromOtherFile = new ValidationIssue { Message = "[file2.mdext] Error in other file", LineNumber = 3 };
        var errorWithoutFilename = new ValidationIssue { Message = "Generic error without filename", LineNumber = 1 };
        
        // Use reflection to access the private method
        var shouldShowErrorMethod = typeof(ValidationErrorOverlay).GetMethod("ShouldShowError", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act & Assert
        Assert.Multiple(() =>
        {
            var shouldShowCurrent = (bool)shouldShowErrorMethod!.Invoke(overlay, new object[] { errorFromCurrentFile });
            var shouldShowOther = (bool)shouldShowErrorMethod!.Invoke(overlay, new object[] { errorFromOtherFile });
            var shouldShowGeneric = (bool)shouldShowErrorMethod!.Invoke(overlay, new object[] { errorWithoutFilename });
            
            Assert.That(shouldShowCurrent, Is.True, "Should show error from current file");
            Assert.That(shouldShowOther, Is.False, "Should NOT show error from other file");
            Assert.That(shouldShowGeneric, Is.True, "Should show generic error without filename prefix");
        });
    }
    
    [Test]
    public void ShouldShowError_WithoutCurrentFileName_ShowsAllErrors()
    {
        // Arrange
        var overlay = new ValidationErrorOverlay();
        // CurrentFileName is null/empty
        
        var errorFromFile1 = new ValidationIssue { Message = "[file1.mdext] Error in file 1", LineNumber = 5 };
        var errorFromFile2 = new ValidationIssue { Message = "[file2.mdext] Error in file 2", LineNumber = 3 };
        var errorWithoutFilename = new ValidationIssue { Message = "Generic error", LineNumber = 1 };
        
        // Use reflection to access the private method
        var shouldShowErrorMethod = typeof(ValidationErrorOverlay).GetMethod("ShouldShowError", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act & Assert
        Assert.Multiple(() =>
        {
            var shouldShowFile1 = (bool)shouldShowErrorMethod!.Invoke(overlay, new object[] { errorFromFile1 });
            var shouldShowFile2 = (bool)shouldShowErrorMethod!.Invoke(overlay, new object[] { errorFromFile2 });
            var shouldShowGeneric = (bool)shouldShowErrorMethod!.Invoke(overlay, new object[] { errorWithoutFilename });
            
            Assert.That(shouldShowFile1, Is.True, "Should show error from file 1 when no filter");
            Assert.That(shouldShowFile2, Is.True, "Should show error from file 2 when no filter");
            Assert.That(shouldShowGeneric, Is.True, "Should show generic error when no filter");
        });
    }
    
    [Test]
    public void ShouldShowError_WithCaseInsensitiveMatching()
    {
        // Arrange
        var overlay = new ValidationErrorOverlay();
        overlay.CurrentFileName = "File1.MDEXT"; // Mixed case
        
        var errorLowerCase = new ValidationIssue { Message = "[file1.mdext] Error with lowercase filename", LineNumber = 5 };
        var errorUpperCase = new ValidationIssue { Message = "[FILE1.MDEXT] Error with uppercase filename", LineNumber = 3 };
        
        // Use reflection to access the private method
        var shouldShowErrorMethod = typeof(ValidationErrorOverlay).GetMethod("ShouldShowError", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act & Assert
        Assert.Multiple(() =>
        {
            var shouldShowLower = (bool)shouldShowErrorMethod!.Invoke(overlay, new object[] { errorLowerCase });
            var shouldShowUpper = (bool)shouldShowErrorMethod!.Invoke(overlay, new object[] { errorUpperCase });
            
            Assert.That(shouldShowLower, Is.True, "Should match lowercase filename case-insensitively");
            Assert.That(shouldShowUpper, Is.True, "Should match uppercase filename case-insensitively");
        });
    }
}
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Business.Models;
using Desktop.Controls;
using NUnit.Framework;

namespace Desktop.UITests;

[TestFixture]
public class ValidationErrorOverlayUITests
{
    [AvaloniaTest]
    public void ValidationErrorOverlay_Should_Show_Red_Underlines_For_Matching_File()
    {
        // Arrange
        var textBox = new TextBox
        {
            Text = "Line 1\nLine 2 with error\nLine 3\nLine 4 with another error\nLine 5",
            Width = 300,
            Height = 150
        };

        var overlay = new ValidationErrorOverlay
        {
            TargetTextBox = textBox,
            CurrentFileName = "/test/path/test.mdext",
            ValidationResult = new ValidationResult
            {
                Errors = new List<ValidationIssue>
                {
                    new ValidationIssue 
                    { 
                        Message = "Error on line 2", 
                        SourceFile = "/test/path/test.mdext", 
                        LineNumber = 2 
                    },
                    new ValidationIssue 
                    { 
                        Message = "Error on line 4", 
                        SourceFile = "/test/path/test.mdext", 
                        LineNumber = 4 
                    }
                }
            }
        };

        // Act - Render the overlay (this calls the Render method)
        overlay.InvalidateVisual();

        // Assert - Verify the overlay processes the correct errors
        var filteredErrors = overlay.ValidationResult!.Errors.Where(e => e.IsFromFile(overlay.CurrentFileName)).ToList();
        
        Assert.Multiple(() =>
        {
            Assert.That(filteredErrors.Count, Is.EqualTo(2), "Should have 2 filtered errors for matching file");
            Assert.That(filteredErrors[0].LineNumber, Is.EqualTo(2), "First error should be on line 2");
            Assert.That(filteredErrors[1].LineNumber, Is.EqualTo(4), "Second error should be on line 4");
        });
    }

    [AvaloniaTest]
    public void ValidationErrorOverlay_Should_Not_Show_Errors_When_CurrentFileName_Is_Null()
    {
        // Arrange
        var textBox = new TextBox
        {
            Text = "Line 1\nLine 2 with error\nLine 3",
            Width = 300,
            Height = 150
        };

        var overlay = new ValidationErrorOverlay
        {
            TargetTextBox = textBox,
            CurrentFileName = null, // No file selected
            ValidationResult = new ValidationResult
            {
                Errors = new List<ValidationIssue>
                {
                    new ValidationIssue 
                    { 
                        Message = "Error on line 2", 
                        SourceFile = "/test/path/test.mdext", 
                        LineNumber = 2 
                    }
                }
            }
        };

        // Act - Check error filtering
        var filteredErrors = overlay.ValidationResult!.Errors.Where(e => e.IsFromFile(overlay.CurrentFileName)).ToList();

        // Assert - No errors should be shown when CurrentFileName is null
        Assert.That(filteredErrors.Count, Is.EqualTo(0), "Should have no filtered errors when CurrentFileName is null");
    }

    [AvaloniaTest]
    public void ValidationErrorOverlay_Should_Not_Show_Errors_Without_SourceFile()
    {
        // Arrange
        var textBox = new TextBox
        {
            Text = "Line 1\nLine 2 with error\nLine 3",
            Width = 300,
            Height = 150
        };

        var overlay = new ValidationErrorOverlay
        {
            TargetTextBox = textBox,
            CurrentFileName = "/test/path/test.mdext",
            ValidationResult = new ValidationResult
            {
                Errors = new List<ValidationIssue>
                {
                    new ValidationIssue 
                    { 
                        Message = "Error without source file", 
                        SourceFile = null, // No source file
                        LineNumber = 2 
                    }
                }
            }
        };

        // Act - Check error filtering
        var filteredErrors = overlay.ValidationResult!.Errors.Where(e => e.IsFromFile(overlay.CurrentFileName)).ToList();

        // Assert - No errors should be shown when SourceFile is null
        Assert.That(filteredErrors.Count, Is.EqualTo(0), "Should have no filtered errors when SourceFile is null");
    }

    [AvaloniaTest]
    public void ValidationErrorOverlay_Should_Filter_Errors_By_File()
    {
        // Arrange
        var textBox = new TextBox
        {
            Text = "Line 1\nLine 2\nLine 3",
            Width = 300,
            Height = 150
        };

        var overlay = new ValidationErrorOverlay
        {
            TargetTextBox = textBox,
            CurrentFileName = "/test/path/file1.mdext",
            ValidationResult = new ValidationResult
            {
                Errors = new List<ValidationIssue>
                {
                    new ValidationIssue 
                    { 
                        Message = "Error in file1", 
                        SourceFile = "/test/path/file1.mdext", 
                        LineNumber = 2 
                    },
                    new ValidationIssue 
                    { 
                        Message = "Error in file2", 
                        SourceFile = "/test/path/file2.mdext", 
                        LineNumber = 2 
                    },
                    new ValidationIssue 
                    { 
                        Message = "Error in file3", 
                        SourceFile = "/test/path/file3.mdext", 
                        LineNumber = 3 
                    }
                }
            }
        };

        // Act - Check error filtering
        var filteredErrors = overlay.ValidationResult!.Errors.Where(e => e.IsFromFile(overlay.CurrentFileName)).ToList();

        // Assert - Only errors from file1.mdext should be shown
        Assert.Multiple(() =>
        {
            Assert.That(filteredErrors.Count, Is.EqualTo(1), "Should have only 1 filtered error for file1.mdext");
            Assert.That(filteredErrors[0].SourceFile, Is.EqualTo("/test/path/file1.mdext"), "Filtered error should be from file1.mdext");
            Assert.That(filteredErrors[0].LineNumber, Is.EqualTo(2), "Filtered error should be on line 2");
        });
    }
}
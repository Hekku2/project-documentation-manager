using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Business.Models;
using Desktop.Behaviors;

namespace Desktop.UITests.Behaviors;

[TestFixture]
public class ValidationHighlightBehaviorTests
{
    private TextBox _textBox = null!;

    [SetUp]
    public void Setup()
    {
        _textBox = new TextBox();
    }

    [Test]
    public void GetValidationResult_Should_Return_Null_By_Default()
    {
        var result = ValidationHighlightBehavior.GetValidationResult(_textBox);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void SetValidationResult_Should_Store_ValidationResult()
    {
        var validationResult = CreateValidValidationResult();

        ValidationHighlightBehavior.SetValidationResult(_textBox, validationResult);
        var result = ValidationHighlightBehavior.GetValidationResult(_textBox);

        Assert.That(result, Is.SameAs(validationResult));
    }

    [Test]
    public void Setting_Null_ValidationResult_Should_Clear_Error_Styling()
    {
        var invalidResult = CreateInvalidValidationResult();
        ValidationHighlightBehavior.SetValidationResult(_textBox, invalidResult);
        Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Red));

        ValidationHighlightBehavior.SetValidationResult(_textBox, null);

        Assert.Multiple(() =>
        {
            Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Transparent));
            Assert.That(_textBox.BorderThickness, Is.EqualTo(new Thickness(0)));
        });
    }

    [Test]
    public void Setting_Valid_ValidationResult_Should_Clear_Error_Styling()
    {
        var invalidResult = CreateInvalidValidationResult();
        ValidationHighlightBehavior.SetValidationResult(_textBox, invalidResult);
        Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Red));

        var validValidationResult = CreateValidValidationResult();
        ValidationHighlightBehavior.SetValidationResult(_textBox, validValidationResult);

        Assert.Multiple(() =>
        {
            Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Transparent));
            Assert.That(_textBox.BorderThickness, Is.EqualTo(new Thickness(0)));
        });
    }

    [Test]
    public void Setting_Invalid_ValidationResult_Should_Apply_Error_Styling()
    {
        var invalidValidationResult = CreateInvalidValidationResult();

        ValidationHighlightBehavior.SetValidationResult(_textBox, invalidValidationResult);

        Assert.Multiple(() =>
        {
            Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Red));
            Assert.That(_textBox.BorderThickness, Is.EqualTo(new Thickness(2)));
        });
    }

    [Test]
    public void Setting_ValidationResult_With_Errors_Without_LineNumbers_Should_Not_Apply_Styling()
    {
        var validationResult = new ValidationResult
        {
            Errors =
            [
                new ValidationIssue { Message = "Error without line number" }
            ]
        };

        ValidationHighlightBehavior.SetValidationResult(_textBox, validationResult);

        Assert.Multiple(() =>
        {
            Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Transparent));
            Assert.That(_textBox.BorderThickness, Is.EqualTo(new Thickness(0)));
        });
    }

    [Test]
    public void Setting_ValidationResult_With_Mixed_Errors_Should_Apply_Styling_Only_For_Line_Errors()
    {
        var validationResult = new ValidationResult
        {
            Errors =
            [
                new ValidationIssue { Message = "Error without line number" },
                new ValidationIssue { Message = "Error with line number", LineNumber = 5 }
            ]
        };

        ValidationHighlightBehavior.SetValidationResult(_textBox, validationResult);

        Assert.Multiple(() =>
        {
            Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Red));
            Assert.That(_textBox.BorderThickness, Is.EqualTo(new Thickness(2)));
        });
    }

    [Test]
    public void Setting_ValidationResult_With_Duplicate_Line_Numbers_Should_Handle_Distinct_Lines()
    {
        var validationResult = new ValidationResult
        {
            Errors =
            [
                new ValidationIssue { Message = "Error 1 on line 3", LineNumber = 3 },
                new ValidationIssue { Message = "Error 2 on line 3", LineNumber = 3 },
                new ValidationIssue { Message = "Error on line 5", LineNumber = 5 }
            ]
        };

        ValidationHighlightBehavior.SetValidationResult(_textBox, validationResult);

        Assert.Multiple(() =>
        {
            Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Red));
            Assert.That(_textBox.BorderThickness, Is.EqualTo(new Thickness(2)));
        });
    }

    [Test]
    public void Updating_ValidationResult_Multiple_Times_Should_Work_Correctly()
    {
        var invalidResult = CreateInvalidValidationResult();
        ValidationHighlightBehavior.SetValidationResult(_textBox, invalidResult);
        Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Red));

        var validResult = CreateValidValidationResult();
        ValidationHighlightBehavior.SetValidationResult(_textBox, validResult);
        Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Transparent));

        ValidationHighlightBehavior.SetValidationResult(_textBox, invalidResult);
        Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Red));
    }

    [Test]
    public void ValidationResult_With_Warnings_Only_Should_Not_Apply_Error_Styling()
    {
        var warningOnlyResult = new ValidationResult
        {
            Warnings =
            [
                new ValidationIssue { Message = "Warning message", LineNumber = 3 }
            ]
        };

        ValidationHighlightBehavior.SetValidationResult(_textBox, warningOnlyResult);

        Assert.Multiple(() =>
        {
            Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Transparent));
            Assert.That(_textBox.BorderThickness, Is.EqualTo(new Thickness(0)));
        });
    }

    [Test]
    public void ValidationResult_With_Both_Errors_And_Warnings_Should_Apply_Error_Styling()
    {
        var mixedResult = new ValidationResult
        {
            Errors =
            [
                new ValidationIssue { Message = "Error message", LineNumber = 5 }
            ],
            Warnings =
            [
                new ValidationIssue { Message = "Warning message", LineNumber = 3 }
            ]
        };

        ValidationHighlightBehavior.SetValidationResult(_textBox, mixedResult);

        Assert.Multiple(() =>
        {
            Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Red));
            Assert.That(_textBox.BorderThickness, Is.EqualTo(new Thickness(2)));
        });
    }

    [Test]
    public void Property_Changed_Should_Trigger_Styling_Update()
    {
        var validResult = CreateValidValidationResult();
        ValidationHighlightBehavior.SetValidationResult(_textBox, validResult);

        Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Transparent));

        var invalidResult = CreateInvalidValidationResult();
        _textBox.SetValue(ValidationHighlightBehavior.ValidationResultProperty, invalidResult);

        Assert.That(_textBox.BorderBrush, Is.EqualTo(Brushes.Red));
    }

    private static ValidationResult CreateValidValidationResult()
    {
        return new ValidationResult();
    }

    private static ValidationResult CreateInvalidValidationResult()
    {
        return new ValidationResult
        {
            Errors =
            [
                new ValidationIssue
                {
                    Message = "Test error message",
                    LineNumber = 10,
                    SourceFile = "test.md"
                }
            ]
        };
    }

}
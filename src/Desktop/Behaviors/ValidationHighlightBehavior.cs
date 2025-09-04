using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Business.Models;

namespace Desktop.Behaviors;

public static class ValidationHighlightBehavior
{
    public static readonly AttachedProperty<ValidationResult?> ValidationResultProperty =
        AvaloniaProperty.RegisterAttached<TextBox, ValidationResult?>("ValidationResult", typeof(ValidationHighlightBehavior));

    public static ValidationResult? GetValidationResult(TextBox textBox)
    {
        return textBox.GetValue(ValidationResultProperty);
    }

    public static void SetValidationResult(TextBox textBox, ValidationResult? value)
    {
        textBox.SetValue(ValidationResultProperty, value);
    }

    static ValidationHighlightBehavior()
    {
        ValidationResultProperty.Changed.AddClassHandler<TextBox>(OnValidationResultChanged);
    }

    private static void OnValidationResultChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs e)
    {
        var validationResult = e.NewValue as ValidationResult;
        ApplyValidationHighlighting(textBox, validationResult);
    }

    private static void ApplyValidationHighlighting(TextBox textBox, ValidationResult? validationResult)
    {
        // Clear any existing error styling
        ClearErrorStyling(textBox);

        if (validationResult == null || validationResult.IsValid)
            return;

        // Apply error styling for lines with errors
        var errorLines = validationResult.Errors
            .Where(e => e.LineNumber.HasValue)
            .Select(e => e.LineNumber!.Value)
            .Distinct()
            .ToList();

        if (errorLines.Any())
        {
            ApplyErrorStyling(textBox, errorLines);
        }
    }

    private static void ClearErrorStyling(TextBox textBox)
    {
        // Reset text box to normal styling
        textBox.BorderBrush = new SolidColorBrush(Colors.Transparent);
        textBox.BorderThickness = new Thickness(0);
    }

    private static void ApplyErrorStyling(TextBox textBox, List<int> errorLines)
    {
        // Apply the text decoration to the entire textbox when errors are present
        // Note: Avalonia's TextBox doesn't support per-line decorations easily,
        // so we'll use a red border as a visual indicator
        textBox.BorderBrush = new SolidColorBrush(Colors.Red);
        textBox.BorderThickness = new Thickness(2);

        // Store error lines for potential future use
        textBox.SetValue(ErrorLinesProperty, errorLines);
    }

    private static readonly AttachedProperty<List<int>?> ErrorLinesProperty =
        AvaloniaProperty.RegisterAttached<TextBox, List<int>?>("ErrorLines", typeof(ValidationHighlightBehavior));
}
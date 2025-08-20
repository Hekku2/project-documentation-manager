using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Business.Models;

namespace Desktop.Controls;

public class ValidationErrorOverlay : Control
{
    public static readonly StyledProperty<ValidationResult?> ValidationResultProperty =
        AvaloniaProperty.Register<ValidationErrorOverlay, ValidationResult?>(nameof(ValidationResult));

    public static readonly StyledProperty<TextBox?> TargetTextBoxProperty =
        AvaloniaProperty.Register<ValidationErrorOverlay, TextBox?>(nameof(TargetTextBox));

    public static readonly StyledProperty<string?> CurrentFileNameProperty =
        AvaloniaProperty.Register<ValidationErrorOverlay, string?>(nameof(CurrentFileName));

    public ValidationResult? ValidationResult
    {
        get => GetValue(ValidationResultProperty);
        set => SetValue(ValidationResultProperty, value);
    }

    public TextBox? TargetTextBox
    {
        get => GetValue(TargetTextBoxProperty);
        set => SetValue(TargetTextBoxProperty, value);
    }

    public string? CurrentFileName
    {
        get => GetValue(CurrentFileNameProperty);
        set => SetValue(CurrentFileNameProperty, value);
    }

    static ValidationErrorOverlay()
    {
        ValidationResultProperty.Changed.AddClassHandler<ValidationErrorOverlay>(OnValidationResultChanged);
        TargetTextBoxProperty.Changed.AddClassHandler<ValidationErrorOverlay>(OnTargetTextBoxChanged);
        CurrentFileNameProperty.Changed.AddClassHandler<ValidationErrorOverlay>(OnCurrentFileNameChanged);
    }

    private static void OnValidationResultChanged(ValidationErrorOverlay overlay, AvaloniaPropertyChangedEventArgs e)
    {
        overlay.InvalidateVisual();
    }

    private static void OnTargetTextBoxChanged(ValidationErrorOverlay overlay, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is TextBox oldTextBox)
        {
            oldTextBox.PropertyChanged -= overlay.OnTextBoxPropertyChanged;
        }

        if (e.NewValue is TextBox newTextBox)
        {
            newTextBox.PropertyChanged += overlay.OnTextBoxPropertyChanged;
        }

        overlay.InvalidateVisual();
    }

    private static void OnCurrentFileNameChanged(ValidationErrorOverlay overlay, AvaloniaPropertyChangedEventArgs e)
    {
        overlay.InvalidateVisual();
    }

    private void OnTextBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // Redraw when text changes
        if (e.Property == TextBox.TextProperty)
        {
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var errors = ValidationResult?.GetErrorsForFile(CurrentFileName) ?? [];

        DrawErrorUnderlines(context, errors);
    }

    private void DrawErrorUnderlines(DrawingContext context, List<ValidationIssue> errors)
    {
        if (TargetTextBox?.Text == null)
            return;

        var errorLines = errors
            .Where(e => e.LineNumber.HasValue)
            .Select(e => e.LineNumber!.Value)
            .Distinct()
            .ToList();

        if (!errorLines.Any())
            return;

        var text = TargetTextBox.Text;
        var lines = text.Split('\n');
        var fontFamily = TargetTextBox.FontFamily;
        var fontSize = TargetTextBox.FontSize;
        var typeface = new Typeface(fontFamily);

        var pen = new Pen(Brushes.Red, 2);
        var margin = TargetTextBox.Margin;
        var padding = TargetTextBox.Padding;

        double currentY = margin.Top + padding.Top;
        var lineHeight = fontSize * 1.2; // Approximate line height

        for (int i = 0; i < lines.Length; i++)
        {
            var lineNumber = i + 1; // Line numbers are 1-based
            
            if (errorLines.Contains(lineNumber))
            {
                var line = lines[i];
                var textGeometry = new FormattedText(
                    line,
                    System.Globalization.CultureInfo.CurrentCulture,
                    Avalonia.Media.FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    Brushes.White);

                var lineWidth = textGeometry.Width;
                var underlineY = currentY + lineHeight - 2;

                // Draw red underline for the entire line
                context.DrawLine(
                    pen,
                    new Point(margin.Left + padding.Left, underlineY),
                    new Point(margin.Left + padding.Left + lineWidth, underlineY));
            }

            currentY += lineHeight;
        }
    }
}
using System.Windows.Input;

namespace Desktop.Models;

/// <summary>
/// Represents an error entry in the error panel with navigation support
/// </summary>
public class ErrorEntry
{
    public required string Type { get; init; } // "Error" or "Warning"
    public required string Message { get; init; }
    public string? FilePath { get; init; }
    public string? FileName { get; init; }
    public int? LineNumber { get; init; }
    public string? SourceContext { get; init; }
    public ICommand? NavigateCommand { get; init; }
    
    public string DisplayText => 
        $"{Type}: {Message}" + (LineNumber.HasValue ? $" (Line {LineNumber})" : "");
    
    public string? FileDisplayText => 
        !string.IsNullOrEmpty(FileName) ? $"File: {FileName}" : null;
    
    public bool HasFileNavigation => !string.IsNullOrEmpty(FilePath) && NavigateCommand != null;
}
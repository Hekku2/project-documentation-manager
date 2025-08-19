using System;

namespace Business.Models;

public static class ValidationIssueExtensions
{
    /// <summary>
    /// Determines if a validation issue is from a specific file
    /// </summary>
    /// <param name="issue">The validation issue to check</param>
    /// <param name="fileName">The filename to compare against</param>
    /// <returns>True if the issue is from the specified file or if filtering should be bypassed</returns>
    public static bool IsFromFile(this ValidationIssue issue, string? fileName)
    {
        // If no file name is provided, show all errors (backward compatibility)
        if (string.IsNullOrEmpty(fileName))
            return true;

        // If the error has a source file, check if it matches the specified file
        if (!string.IsNullOrEmpty(issue.SourceFile))
        {
            return string.Equals(issue.SourceFile, fileName, StringComparison.OrdinalIgnoreCase);
        }

        // For errors without source file information, always show
        return true;
    }
}
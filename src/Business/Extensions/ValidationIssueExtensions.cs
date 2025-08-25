namespace Business.Models;

public static class ValidationIssueExtensions
{
    /// <summary>
    /// Determines if a validation issue is from a specific file
    /// </summary>
    /// <param name="issue">The validation issue to check</param>
    /// <param name="fileName">The filename to compare against</param>
    /// <returns>True if the issue is from the specified file, false otherwise</returns>
    public static bool IsFromFile(this ValidationIssue issue, string? fileName)
    {
        // If no file name is provided, return false
        if (string.IsNullOrEmpty(fileName))
            return false;

        // If the error has no source file, return false
        if (string.IsNullOrEmpty(issue.SourceFile))
            return false;

        try
        {
            // Resolve both paths to their full canonical forms for comparison
            var resolvedSourceFile = Path.GetFullPath(issue.SourceFile);
            var resolvedFileName = Path.GetFullPath(fileName);
            
            // Compare the resolved full paths
            return string.Equals(resolvedSourceFile, resolvedFileName, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            // If path resolution fails (e.g., invalid characters), fall back to direct string comparison
            return string.Equals(issue.SourceFile, fileName, StringComparison.OrdinalIgnoreCase);
        }
    }
}

public static class ValidationResultExtensions
{
    /// <summary>
    /// Gets all errors from a ValidationResult that belong to a specific file
    /// </summary>
    /// <param name="validationResult">The validation result to filter</param>
    /// <param name="fileName">The filename to filter by</param>
    /// <returns>A list of validation issues for the specified file</returns>
    public static List<ValidationIssue> GetErrorsForFile(this ValidationResult validationResult, string? fileName)
    {
        if (validationResult == null)
            return new List<ValidationIssue>();

        return validationResult.Errors.Where(error => error.IsFromFile(fileName)).ToList();
    }
}
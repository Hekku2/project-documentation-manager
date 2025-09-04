namespace Business.Models;

public static class ValidationResultExtensions
{
    /// <summary>
    /// Gets all errors from a ValidationResult that belong to a specific file
    /// </summary>
    /// <param name="validationResult">The validation result to filter</param>
    /// <param name="fileName">The filename to filter by</param>
    /// <returns>A list of validation issues for the specified file</returns>
    public static List<ValidationIssue> GetErrorsForFile(this ValidationResult? validationResult, string? fileName)
    {
        if (validationResult == null)
            return new List<ValidationIssue>();

        return validationResult.Errors.Where(error => error.IsFromFile(fileName)).ToList();
    }
}

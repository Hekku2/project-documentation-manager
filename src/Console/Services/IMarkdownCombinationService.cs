using ProjectDocumentationManager.Console.Models;

namespace ProjectDocumentationManager.Console.Services;

/// <summary>
/// Service for building documentation by processing template files with insert directives
/// </summary>
public interface IMarkdownCombinationService
{
    /// <summary>
    /// Builds documentation by processing template files and inserting content from source documents.
    /// Template files can contain &lt;insert filename.md&gt; directives that will be replaced with 
    /// content from the corresponding source documents.
    /// </summary>
    /// <param name="documents">Collection of markdown documents (serves as both templates and sources)</param>
    /// <returns>List of processed documents with insert directives resolved</returns>
    IEnumerable<MarkdownDocument> BuildDocumentation(IEnumerable<MarkdownDocument> documents);

    /// <summary>
    /// Validates template documents for insert directive correctness
    /// </summary>
    /// <param name="documents">Collection of markdown documents (serves as both templates and sources)</param>
    /// <returns>Combined validation result with errors and warnings from all templates</returns>
    ValidationResult Validate(IEnumerable<MarkdownDocument> documents);
}
using Business.Models;

namespace Business.Services;

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
    /// <param name="templateDocuments">List of template documents that contain insert directives</param>
    /// <param name="sourceDocuments">List of source documents that provide content for insertions</param>
    /// <returns>List of processed documents with insert directives resolved</returns>
    IEnumerable<MarkdownDocument> BuildDocumentation(
        IEnumerable<MarkdownDocument> templateDocuments,
        IEnumerable<MarkdownDocument> sourceDocuments);

    /// <summary>
    /// Validates template documents for insert directive correctness
    /// </summary>
    /// <param name="templateDocuments">Template documents to validate</param>
    /// <param name="sourceDocuments">Available source documents for validation</param>
    /// <returns>Combined validation result with errors and warnings from all templates</returns>
    ValidationResult Validate(
        IEnumerable<MarkdownDocument> templateDocuments,
        IEnumerable<MarkdownDocument> sourceDocuments);
}
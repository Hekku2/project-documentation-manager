using MarkdownCompiler.Console.Models;

namespace MarkdownCompiler.Console.Services;

/// <summary>
/// Service for compiling markdown templates and sources into documentation
/// </summary>
public interface IMarkdownCompilerService
{
    /// <summary>
    /// Compiles markdown templates by processing template files and inserting content from source documents.
    /// Template files can contain &lt;insert filename.md&gt; directives that will be replaced with 
    /// content from the corresponding source documents.
    /// </summary>
    /// <param name="documents">Collection of markdown documents (serves as both templates and sources)</param>
    /// <returns>List of compiled documents with insert directives resolved</returns>
    IEnumerable<MarkdownDocument> CompileDocuments(IEnumerable<MarkdownDocument> documents);

    /// <summary>
    /// Validates template documents for insert directive correctness
    /// </summary>
    /// <param name="documents">Collection of markdown documents (serves as both templates and sources)</param>
    /// <returns>Combined validation result with errors and warnings from all templates</returns>
    ValidationResult Validate(IEnumerable<MarkdownDocument> documents);
}
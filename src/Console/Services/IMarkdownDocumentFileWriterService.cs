using ProjectDocumentationManager.Console.Models;

namespace ProjectDocumentationManager.Console.Services;

/// <summary>
/// Service for writing MarkdownDocument collections to files in a specified folder
/// </summary>
public interface IMarkdownDocumentFileWriterService
{
    /// <summary>
    /// Writes a collection of MarkdownDocuments to files in the specified folder.
    /// Each document's FileName property determines the output filename.
    /// </summary>
    /// <param name="documents">Collection of MarkdownDocument objects to write</param>
    /// <param name="outputFolder">Target folder path where files will be written</param>
    /// <returns>Task that completes when all files have been written</returns>
    /// <exception cref="ArgumentNullException">Thrown when documents or outputFolder is null</exception>
    /// <exception cref="ArgumentException">Thrown when outputFolder is empty or whitespace</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the output folder doesn't exist and cannot be created</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when there are insufficient permissions to write to the folder</exception>
    /// <exception cref="IOException">Thrown when file write operations fail</exception>
    Task WriteDocumentsToFolderAsync(IEnumerable<MarkdownDocument> documents, string outputFolder);
}
using Business.Models;
using Microsoft.Extensions.Logging;

namespace Business.Services;

/// <summary>
/// Service for writing MarkdownDocument collections to files in a specified folder
/// </summary>
public class MarkdownDocumentFileWriterService(ILogger<MarkdownDocumentFileWriterService> logger) : IMarkdownDocumentFileWriterService
{
    public async Task WriteDocumentsToFolderAsync(IEnumerable<MarkdownDocument> documents, string outputFolder)
    {
        if (documents == null)
            throw new ArgumentNullException(nameof(documents));
        
        if (outputFolder == null)
            throw new ArgumentNullException(nameof(outputFolder));
        
        if (string.IsNullOrWhiteSpace(outputFolder))
            throw new ArgumentException("Output folder cannot be empty or whitespace", nameof(outputFolder));

        var documentList = documents.ToList();
        logger.LogInformation("Starting to write {DocumentCount} documents to folder: {OutputFolder}", 
            documentList.Count, outputFolder);

        // Ensure the output directory exists
        try
        {
            if (!Directory.Exists(outputFolder))
            {
                logger.LogDebug("Creating output directory: {OutputFolder}", outputFolder);
                Directory.CreateDirectory(outputFolder);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create output directory: {OutputFolder}", outputFolder);
            throw new DirectoryNotFoundException($"Unable to create output directory: {outputFolder}", ex);
        }

        // Write each document to a file
        var writeTasks = documentList.Select(async document =>
        {
            if (string.IsNullOrWhiteSpace(document.FileName))
            {
                logger.LogWarning("Skipping document with empty filename");
                return;
            }

            var filePath = Path.Combine(outputFolder, document.FileName);
            
            try
            {
                logger.LogDebug("Writing document to file: {FilePath}", filePath);
                await File.WriteAllTextAsync(filePath, document.Content ?? string.Empty);
                logger.LogDebug("Successfully wrote document to: {FilePath}", filePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogError(ex, "Insufficient permissions to write file: {FilePath}", filePath);
                throw new UnauthorizedAccessException($"Insufficient permissions to write file: {filePath}", ex);
            }
            catch (IOException ex)
            {
                logger.LogError(ex, "IO error writing file: {FilePath}", filePath);
                throw new IOException($"Failed to write file: {filePath}", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error writing file: {FilePath}", filePath);
                throw;
            }
        });

        try
        {
            await Task.WhenAll(writeTasks);
            logger.LogInformation("Successfully wrote {DocumentCount} documents to folder: {OutputFolder}", 
                documentList.Count(d => !string.IsNullOrWhiteSpace(d.FileName)), outputFolder);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while writing documents to folder: {OutputFolder}", outputFolder);
            throw;
        }
    }
}
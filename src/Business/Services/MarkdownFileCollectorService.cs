using Business.Models;
using Microsoft.Extensions.Logging;

namespace Business.Services;

/// <summary>
/// Service for collecting markdown source and template files from a directory
/// </summary>
public class MarkdownFileCollectorService(ILogger<MarkdownFileCollectorService> logger) : IMarkdownFileCollectorService
{
    private const string TemplateFileExtension = ".mdext";
    private const string SourceFileExtension = ".mdsrc";

    public async Task<IEnumerable<MarkdownDocument>> CollectTemplateFilesAsync(string directoryPath)
    {
        ValidateDirectoryPath(directoryPath);
        
        logger.LogInformation("Collecting template files (.mdext) from directory: {DirectoryPath}", directoryPath);
        
        return await CollectFilesByExtensionAsync(directoryPath, TemplateFileExtension);
    }

    public async Task<IEnumerable<MarkdownDocument>> CollectSourceFilesAsync(string directoryPath)
    {
        ValidateDirectoryPath(directoryPath);
        
        logger.LogInformation("Collecting source files (.mdsrc) from directory: {DirectoryPath}", directoryPath);
        
        return await CollectFilesByExtensionAsync(directoryPath, SourceFileExtension);
    }

    public async Task<(IEnumerable<MarkdownDocument> TemplateFiles, IEnumerable<MarkdownDocument> SourceFiles)> CollectAllMarkdownFilesAsync(string directoryPath)
    {
        ValidateDirectoryPath(directoryPath);
        
        logger.LogInformation("Collecting all markdown files (.mdext and .mdsrc) from directory: {DirectoryPath}", directoryPath);
        
        var templateFilesTask = CollectTemplateFilesAsync(directoryPath);
        var sourceFilesTask = CollectSourceFilesAsync(directoryPath);
        
        await Task.WhenAll(templateFilesTask, sourceFilesTask);
        
        var templateFiles = await templateFilesTask;
        var sourceFiles = await sourceFilesTask;
        
        logger.LogInformation("Collected {TemplateCount} template files and {SourceCount} source files from: {DirectoryPath}", 
            templateFiles.Count(), sourceFiles.Count(), directoryPath);
        
        return (templateFiles, sourceFiles);
    }

    private async Task<IEnumerable<MarkdownDocument>> CollectFilesByExtensionAsync(string directoryPath, string extension)
    {
        var documents = new List<MarkdownDocument>();
        
        try
        {
            var files = Directory.GetFiles(directoryPath, $"*{extension}", SearchOption.AllDirectories);
            
            logger.LogDebug("Found {FileCount} files with extension {Extension} in: {DirectoryPath}", 
                files.Length, extension, directoryPath);

            var readTasks = files.Select(async filePath =>
            {
                try
                {
                    logger.LogDebug("Reading file: {FilePath}", filePath);
                    
                    var content = await File.ReadAllTextAsync(filePath);
                    var fileName = Path.GetFileName(filePath);
                    
                    return new MarkdownDocument(fileName, content);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error reading file: {FilePath}", filePath);
                    // Return document with empty content on error
                    return new MarkdownDocument(Path.GetFileName(filePath), string.Empty);
                }
            });

            documents.AddRange(await Task.WhenAll(readTasks));
            
            logger.LogInformation("Successfully collected {DocumentCount} {Extension} files from: {DirectoryPath}", 
                documents.Count, extension, directoryPath);
        }
        catch (DirectoryNotFoundException ex)
        {
            logger.LogError(ex, "Directory not found: {DirectoryPath}", directoryPath);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Access denied to directory: {DirectoryPath}", directoryPath);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error collecting files from directory: {DirectoryPath}", directoryPath);
            throw;
        }

        return documents;
    }

    private static void ValidateDirectoryPath(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));
            
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
    }
}
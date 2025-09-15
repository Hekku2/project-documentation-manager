using Microsoft.Extensions.Logging;
using MarkdownCompiler.Console.Models;

namespace MarkdownCompiler.Console.Services;

/// <summary>
/// Service for collecting markdown source and template files from a directory
/// </summary>
public class MarkdownFileCollectorService(ILogger<MarkdownFileCollectorService> logger) : IMarkdownFileCollectorService
{

    public async Task<IEnumerable<MarkdownDocument>> CollectAllMarkdownFilesAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ValidateDirectoryPath(directoryPath);

        logger.LogDebug("Collecting all markdown files (.md, .mdsrc, and .mdext) from directory: {DirectoryPath}", directoryPath);

        var allFiles = await CollectFilesByExtensionAsync(directoryPath, [MarkdownFileExtensions.Markdown, MarkdownFileExtensions.Template, MarkdownFileExtensions.Source], cancellationToken);

        var markdownFiles = allFiles.Where(f => MarkdownFileExtensions.HasExtension(f.FileName, MarkdownFileExtensions.Markdown));
        var templateFiles = allFiles.Where(f => MarkdownFileExtensions.HasExtension(f.FileName, MarkdownFileExtensions.Template));
        var sourceFiles = allFiles.Where(f => MarkdownFileExtensions.HasExtension(f.FileName, MarkdownFileExtensions.Source));

        logger.LogInformation("Collected {TotalCount} markdown files ({MarkdownCount} .md, {TemplateCount} .mdext, {SourceCount} .mdsrc) from: {DirectoryPath}",
            allFiles.Count(), markdownFiles.Count(), templateFiles.Count(), sourceFiles.Count(), directoryPath);

        return allFiles;
    }

    private async Task<IEnumerable<MarkdownDocument>> CollectFilesByExtensionAsync(string directoryPath, string[] extensions, CancellationToken cancellationToken = default)
    {
        var documents = new List<MarkdownDocument>();

        try
        {
            // Use bounded concurrency to avoid resource exhaustion
            var maxConcurrency = Environment.ProcessorCount;
            using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

            var readTasks = new List<Task<MarkdownDocument>>();

            // Stream files instead of materializing all paths
            var filteredFiles = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                .Where(f => extensions.Any(ext => MarkdownFileExtensions.HasExtension(f, ext)));

            foreach (var filePath in filteredFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var readTask = ReadFileWithSemaphoreAsync(filePath, directoryPath, semaphore, cancellationToken);
                readTasks.Add(readTask);
            }

            logger.LogDebug("Found {FileCount} files with extensions [{Extensions}] in: {DirectoryPath}",
                readTasks.Count, string.Join(", ", extensions), directoryPath);

            documents.AddRange(await Task.WhenAll(readTasks));

            logger.LogInformation("Successfully collected {DocumentCount} files with extensions [{Extensions}] from: {DirectoryPath}",
                documents.Count, string.Join(", ", extensions), directoryPath);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Access denied to directory: {directoryPath}", ex);
        }

        return documents;
    }

    private async Task<MarkdownDocument> ReadFileWithSemaphoreAsync(string filePath, string directoryPath, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            logger.LogDebug("Reading file: {FilePath}", filePath);

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var relativePath = Path.GetRelativePath(directoryPath, filePath);

            return new MarkdownDocument
            {
                FileName = relativePath,
                FilePath = relativePath,
                Content = content
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading file: {FilePath}", filePath);
            // Return document with empty content on error
            var relativePath = Path.GetRelativePath(directoryPath, filePath);
            return new MarkdownDocument
            {
                FileName = relativePath,
                FilePath = relativePath,
                Content = string.Empty
            };
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static void ValidateDirectoryPath(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));

        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
    }
}
using System.Text.RegularExpressions;
using Business.Models;
using Microsoft.Extensions.Logging;

namespace Business.Services;

/// <summary>
/// Service for building documentation by processing template files with insert directives
/// </summary>
public class MarkdownCombinationService(ILogger<MarkdownCombinationService> logger) : IMarkdownCombinationService
{
    private static readonly Regex InsertDirectiveRegex = new(@"<insert\s+([^>]+)>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public IEnumerable<MarkdownDocument> BuildDocumentation(
        IEnumerable<MarkdownDocument> templateDocuments,
        IEnumerable<MarkdownDocument> sourceDocuments)
    {
        if (templateDocuments == null)
            throw new ArgumentNullException(nameof(templateDocuments));
        
        if (sourceDocuments == null)
            throw new ArgumentNullException(nameof(sourceDocuments));

        var templateList = templateDocuments.ToList();
        var sourceDictionary = sourceDocuments.ToDictionary(
            doc => doc.FileName, 
            doc => doc.Content, 
            StringComparer.OrdinalIgnoreCase);

        logger.LogInformation("Building documentation for {TemplateCount} templates using {SourceCount} source documents", 
            templateList.Count, sourceDictionary.Count);

        // Log all source documents for debugging
        if (sourceDictionary.Count > 0)
        {
            logger.LogDebug("Available source documents: {SourceDocuments}", 
                string.Join(", ", sourceDictionary.Keys));
        }
        else
        {
            logger.LogDebug("No source documents provided");
        }

        var results = new List<MarkdownDocument>();

        foreach (var template in templateList)
        {
            try
            {
                logger.LogDebug("Processing template: {TemplateFileName}", template.FileName);
                
                var processedContent = ProcessTemplate(template.Content, sourceDictionary, template.FileName);
                var outputFileName = Path.ChangeExtension(template.FileName, ".md");
                var resultDocument = new MarkdownDocument(outputFileName, processedContent);
                
                results.Add(resultDocument);
                
                logger.LogDebug("Successfully processed template: {TemplateFileName}", template.FileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing template: {TemplateFileName}", template.FileName);
                // Add the template with original content on error, but with .md extension
                var outputFileName = Path.ChangeExtension(template.FileName, ".md");
                results.Add(new MarkdownDocument(outputFileName, template.Content));
            }
        }

        logger.LogInformation("Documentation building completed. Processed {ProcessedCount} templates", results.Count);
        
        return results;
    }

    private string ProcessTemplate(string templateContent, Dictionary<string, string> sourceDictionary, string templateFileName)
    {
        if (string.IsNullOrEmpty(templateContent))
            return templateContent;

        var processedContent = templateContent;
        var processedDirectives = new HashSet<string>();

        // Process insert directives - continue until no more directives are found (handles nested inserts)
        int maxIterations = 10; // Prevent infinite loops
        int iteration = 0;
        
        while (iteration < maxIterations)
        {
            var matches = InsertDirectiveRegex.Matches(processedContent);
            if (matches.Count == 0)
                break; // No more directives to process

            bool anyReplaced = false;

            foreach (Match match in matches)
            {
                var fullDirective = match.Value; // e.g., "<insert common-features.md>"
                var fileName = match.Groups[1].Value.Trim(); // e.g., "common-features.md"

                // Skip if we've already processed this directive in this iteration
                if (processedDirectives.Contains(fullDirective))
                    continue;

                if (sourceDictionary.TryGetValue(fileName, out var sourceContent))
                {
                    logger.LogDebug("Inserting content from {SourceFileName} into {TemplateFileName}", 
                        fileName, templateFileName);
                    
                    processedContent = processedContent.Replace(fullDirective, sourceContent ?? string.Empty);
                    processedDirectives.Add(fullDirective);
                    anyReplaced = true;
                }
                else
                {
                    logger.LogWarning("Source document not found for insert directive: {FileName} in template {TemplateFileName}", 
                        fileName, templateFileName);
                    
                    // Replace with a comment indicating missing source
                    var replacementComment = $"<!-- Missing source: {fileName} -->";
                    processedContent = processedContent.Replace(fullDirective, replacementComment);
                    processedDirectives.Add(fullDirective);
                    anyReplaced = true;
                }
            }

            if (!anyReplaced)
                break; // No directives were replaced, avoid infinite loop

            iteration++;
        }

        if (iteration >= maxIterations)
        {
            logger.LogWarning("Maximum iterations reached while processing template {TemplateFileName}. " +
                             "This might indicate circular references in insert directives.", templateFileName);
        }

        return processedContent;
    }
}
using System.Text.RegularExpressions;
using Business.Models;
using Microsoft.Extensions.Logging;

namespace Business.Services;

/// <summary>
/// Service for building documentation by processing template files with insert directives
/// </summary>
public class MarkdownCombinationService(ILogger<MarkdownCombinationService> logger) : IMarkdownCombinationService
{
    private static readonly Regex InsertDirectiveRegex = new(@"<MarkDownExtension\s+operation=""insert""\s+file=""([^""]*)""\s*/>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
                var fullDirective = match.Value; // e.g., "<MarkDownExtension operation="insert" file="common-features.mdsrc" />"
                var fileName = match.Groups[1].Value.Trim(); // e.g., "common-features.mdsrc"

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

    public ValidationResult Validate(MarkdownDocument templateDocument, IEnumerable<MarkdownDocument> sourceDocuments)
    {
        if (templateDocument == null)
            throw new ArgumentNullException(nameof(templateDocument));
        
        if (sourceDocuments == null)
            throw new ArgumentNullException(nameof(sourceDocuments));

        var result = new ValidationResult();
        var sourceDictionary = sourceDocuments.ToDictionary(
            doc => doc.FileName, 
            doc => doc.Content, 
            StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(templateDocument.Content))
        {
            return result; // Empty content is valid
        }

        ValidateInsertDirectives(templateDocument, sourceDictionary, result);
        
        return result;
    }

    private void ValidateInsertDirectives(MarkdownDocument templateDocument, Dictionary<string, string> sourceDictionary, ValidationResult result)
    {
        var content = templateDocument.Content;
        var lines = content.Split('\n');
        var processedDirectives = new HashSet<string>();
        var currentDirectives = new HashSet<string>();
        
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var matches = InsertDirectiveRegex.Matches(line);
            
            foreach (Match match in matches)
            {
                var fullDirective = match.Value;
                var fileName = match.Groups[1].Value.Trim();
                var lineNumber = lineIndex + 1;
                
                // Check for malformed directive
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    result.Errors.Add(new ValidationIssue
                    {
                        Message = "MarkDownExtension directive is missing filename",
                        DirectivePath = fullDirective,
                        LineNumber = lineNumber,
                        SourceContext = line.Trim()
                    });
                    continue;
                }

                // Check for invalid characters in path (allow forward slash for paths)
                var invalidChars = Path.GetInvalidFileNameChars().Where(c => c != '/').ToArray();
                if (fileName.IndexOfAny(invalidChars) >= 0)
                {
                    result.Errors.Add(new ValidationIssue
                    {
                        Message = $"MarkDownExtension directive contains invalid filename characters: '{fileName}'",
                        DirectivePath = fileName,
                        LineNumber = lineNumber,
                        SourceContext = line.Trim()
                    });
                    continue;
                }

                // Check if source file exists
                if (!sourceDictionary.ContainsKey(fileName))
                {
                    result.Errors.Add(new ValidationIssue
                    {
                        Message = $"Source document not found: '{fileName}'",
                        DirectivePath = fileName,
                        LineNumber = lineNumber,
                        SourceContext = line.Trim()
                    });
                    continue;
                }

                // Check for duplicate directives in the same template
                if (processedDirectives.Contains(fullDirective))
                {
                    result.Warnings.Add(new ValidationIssue
                    {
                        Message = $"Duplicate MarkDownExtension directive found: '{fullDirective}'",
                        DirectivePath = fileName,
                        LineNumber = lineNumber,
                        SourceContext = line.Trim()
                    });
                }
                else
                {
                    processedDirectives.Add(fullDirective);
                }

                // Track directives for potential circular reference detection
                currentDirectives.Add(fileName);
            }
        }

        // Check for potential circular references by validating nested directives
        ValidateCircularReferences(templateDocument.FileName, currentDirectives, sourceDictionary, result, new HashSet<string>());
    }

    private void ValidateCircularReferences(string currentFileName, HashSet<string> currentDirectives, 
        Dictionary<string, string> sourceDictionary, ValidationResult result, HashSet<string> visitedFiles)
    {
        if (visitedFiles.Contains(currentFileName))
        {
            result.Warnings.Add(new ValidationIssue
            {
                Message = $"Potential circular reference detected involving file: '{currentFileName}'",
                DirectivePath = currentFileName
            });
            return;
        }

        visitedFiles.Add(currentFileName);

        foreach (var directive in currentDirectives)
        {
            if (sourceDictionary.TryGetValue(directive, out var sourceContent))
            {
                var nestedMatches = InsertDirectiveRegex.Matches(sourceContent);
                var nestedDirectives = new HashSet<string>();
                
                foreach (Match match in nestedMatches)
                {
                    var nestedFileName = match.Groups[1].Value.Trim();
                    nestedDirectives.Add(nestedFileName);
                }

                if (nestedDirectives.Count > 0)
                {
                    ValidateCircularReferences(directive, nestedDirectives, sourceDictionary, result, new HashSet<string>(visitedFiles));
                }
            }
        }
    }
}
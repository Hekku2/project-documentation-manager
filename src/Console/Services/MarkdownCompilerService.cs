using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MarkdownCompiler.Console.Models;

namespace MarkdownCompiler.Console.Services;

/// <summary>
/// Service for compiling markdown templates and sources into documentation
/// </summary>
public class MarkdownCompilerService(ILogger<MarkdownCompilerService> logger) : IMarkdownCompilerService
{
    private static readonly Regex InsertDirectiveRegex = new(@"<MarkDownExtension\s+operation=""insert""\s+file=""([^""]*)""\s*/>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AnyMarkdownExtensionRegex = new(@"<MarkDownExtension[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public IEnumerable<MarkdownDocument> CompileDocuments(IEnumerable<MarkdownDocument> documents)
    {
        if (documents == null)
            throw new ArgumentNullException(nameof(documents));

        var documentList = documents.ToList();
        var templateDocuments = documentList.Where(doc => MarkdownFileExtensions.HasExtension(doc.FileName, MarkdownFileExtensions.Template));
        var sourceDocuments = documentList.Where(doc =>
            MarkdownFileExtensions.HasExtension(doc.FileName, MarkdownFileExtensions.Source) ||
            MarkdownFileExtensions.HasExtension(doc.FileName, MarkdownFileExtensions.Markdown) ||
            MarkdownFileExtensions.HasExtension(doc.FileName, MarkdownFileExtensions.Template));

        var templateList = templateDocuments.ToList();
        var sourceDictionary = sourceDocuments.ToDictionary(
            doc => PathUtilities.NormalizePathKey(doc.FilePath),
             doc => doc.Content,
             PathUtilities.FilePathComparer);

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

        // Add regular markdown files that are not source files
        var regularMarkdownFiles = documentList.Where(doc =>
            MarkdownFileExtensions.HasExtension(doc.FileName, MarkdownFileExtensions.Markdown) &&
            !MarkdownFileExtensions.HasExtension(doc.FileName, MarkdownFileExtensions.Source) &&
            !MarkdownFileExtensions.HasExtension(doc.FileName, MarkdownFileExtensions.Template));

        foreach (var markdownFile in regularMarkdownFiles)
        {
            results.Add(markdownFile);
            logger.LogDebug("Including regular markdown file: {FileName}", markdownFile.FileName);
        }

        // Process template files
        foreach (var template in templateList)
        {
            try
            {
                logger.LogDebug("Processing template: {TemplateFilePath}", template.FilePath);

                var processedContent = ProcessTemplate(template.Content, sourceDictionary, template.FilePath);
                var outputFileName = Path.ChangeExtension(template.FileName, MarkdownFileExtensions.Markdown);
                var resultDocument = new MarkdownDocument
                {
                    FileName = outputFileName,
                    FilePath = Path.ChangeExtension(template.FilePath, MarkdownFileExtensions.Markdown),
                    Content = processedContent
                };

                results.Add(resultDocument);

                logger.LogDebug("Successfully processed template: {TemplateFileName}", template.FileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing template: {TemplateFileName} at {TemplateFilePath}", template.FileName, template.FilePath);
                // Add the template with original content on error, but with .md extension
                var outputFileName = Path.ChangeExtension(template.FileName, MarkdownFileExtensions.Markdown);
                results.Add(new MarkdownDocument
                {
                    FileName = outputFileName,
                    FilePath = Path.ChangeExtension(template.FilePath, MarkdownFileExtensions.Markdown),
                    Content = template.Content
                });
            }
        }

        logger.LogInformation("Documentation building completed. Processed {ProcessedCount} templates", results.Count);

        return results;
    }

    private string ProcessTemplate(string templateContent, Dictionary<string, string> sourceDictionary, string templateFilePath)
    {
        if (string.IsNullOrEmpty(templateContent))
            return templateContent;

        var processedContent = templateContent;
        var processedDirectives = new HashSet<string>();
        const int MaxIterations = 10;
        int iteration = 0;

        var directiveProcesses = true;
        while (iteration < MaxIterations && directiveProcesses)
        {
            var matches = FindInsertDirectives(processedContent);
            if (matches.Count == 0)
                break;

            directiveProcesses = ProcessDirectiveMatches(matches, ref processedContent, sourceDictionary, processedDirectives, templateFilePath);
            iteration++;
        }

        LogMaxIterationsWarningIfNeeded(iteration, MaxIterations, templateFilePath);
        return processedContent;
    }

    private static MatchCollection FindInsertDirectives(string content)
    {
        return InsertDirectiveRegex.Matches(content);
    }

    private bool ProcessDirectiveMatches(MatchCollection matches, ref string processedContent,
        Dictionary<string, string> sourceDictionary, HashSet<string> processedDirectives, string templateFilePath)
    {
        var anyReplaced = false;

        foreach (Match match in matches)
        {
            if (ProcessSingleDirective(match, ref processedContent, sourceDictionary, processedDirectives, templateFilePath))
            {
                anyReplaced = true;
            }
        }

        return anyReplaced;
    }

    private bool ProcessSingleDirective(Match match, ref string processedContent,
        Dictionary<string, string> sourceDictionary, HashSet<string> processedDirectives, string templateFilePath)
    {
        var fullDirective = match.Value;
        var fileName = match.Groups[1].Value.Trim();

        if (processedDirectives.Contains(fullDirective))
        {
            // Skip already processed directives
            return false;
        }

        // Resolve relative path based on template location
        var templateDir = Path.GetDirectoryName(templateFilePath) ?? string.Empty;
        var resolvedPath = string.IsNullOrEmpty(templateDir)
            ? fileName
            : Path.GetRelativePath(".", Path.Combine(templateDir, fileName));

        var normalizedFileName = PathUtilities.NormalizePathKey(resolvedPath);
        string replacementContent = GetReplacementContent(normalizedFileName, sourceDictionary, templateFilePath);

        processedContent = processedContent.Replace(fullDirective, replacementContent);
        processedDirectives.Add(fullDirective);
        return true;
    }

    private string GetReplacementContent(string normalizedFileName, Dictionary<string, string> sourceDictionary, string templateFileName)
    {
        if (sourceDictionary.TryGetValue(normalizedFileName, out var sourceContent))
        {
            logger.LogDebug("Inserting content from {SourceFileName} into {TemplateFileName}",
                normalizedFileName, templateFileName);
            return sourceContent;
        }
        else
        {
            logger.LogWarning("Source document not found for insert directive: {FileName} in template {TemplateFileName}",
                normalizedFileName, templateFileName);
            return $"<!-- Missing source: {normalizedFileName} -->";
        }
    }

    private void LogMaxIterationsWarningIfNeeded(int iteration, int maxIterations, string templateFileName)
    {
        if (iteration >= maxIterations)
        {
            logger.LogWarning("Maximum iterations reached while processing template {TemplateFileName}. " +
                             "This might indicate circular references in insert directives.", templateFileName);
        }
    }

    private void ValidateInsertDirectives(MarkdownDocument templateDocument, Dictionary<string, string> sourceDictionary, ValidationResult result)
    {
        var content = templateDocument.Content;
        var lines = content.Split('\n');
        var processedDirectives = new HashSet<string>();
        var currentDirectives = new HashSet<string>();

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var lineNumber = lineIndex + 1;

            // Check for malformed directives
            ValidateMalformedDirectives(line, lineNumber, templateDocument, result);

            // Process valid directives
            var validDirectives = InsertDirectiveRegex.Matches(line);
            foreach (Match match in validDirectives)
            {
                var fullDirective = match.Value;
                var fileName = match.Groups[1].Value.Trim();

                if (!ValidateDirectiveFileName(fileName, fullDirective, line, lineNumber, templateDocument, result))
                    continue;

                if (!ValidateFileNameCharacters(fileName, line, lineNumber, templateDocument, result))
                    continue;

                if (!ValidateSourceFileExists(fileName, line, lineNumber, templateDocument, sourceDictionary, result))
                    continue;

                ValidateDuplicateDirective(fullDirective, fileName, line, lineNumber, templateDocument, processedDirectives, result);

                // Track directives for potential circular reference detection
                var templateDir = Path.GetDirectoryName(templateDocument.FilePath) ?? string.Empty;
                var resolvedPath = string.IsNullOrEmpty(templateDir)
                    ? fileName
                    : Path.GetRelativePath(".", Path.Combine(templateDir, fileName));
                currentDirectives.Add(resolvedPath);
            }
        }
    }

    private void ValidateMalformedDirectives(string line, int lineNumber, MarkdownDocument templateDocument, ValidationResult result)
    {
        var allDirectives = AnyMarkdownExtensionRegex.Matches(line);
        var validDirectives = InsertDirectiveRegex.Matches(line);

        if (allDirectives.Count <= validDirectives.Count) return;

        foreach (var directive in allDirectives.Select(match => match.Value))
        {
            var isValid = validDirectives.Cast<Match>().Any(validMatch => validMatch.Value == directive);

            if (!isValid)
            {
                var errorMessage = GetMalformedDirectiveErrorMessage(directive);
                result.Errors.Add(new ValidationIssue
                {
                    Message = errorMessage,
                    DirectivePath = directive,
                    SourceFile = templateDocument.FilePath,
                    LineNumber = lineNumber,
                    SourceContext = line.Trim()
                });
            }
        }
    }

    private static string GetMalformedDirectiveErrorMessage(string directive)
    {
        if (!directive.Contains("operation="))
            return "MarkDownExtension directive is missing 'operation' attribute";
        if (!directive.Contains("operation=\"insert\""))
            return "MarkDownExtension directive has invalid operation. Only 'insert' is supported";
        if (!directive.Contains("file="))
            return "MarkDownExtension directive is missing 'file' attribute";
        return "MarkDownExtension directive is malformed";
    }

    private static bool ValidateDirectiveFileName(string fileName, string fullDirective, string line, int lineNumber, MarkdownDocument templateDocument, ValidationResult result)
    {
        if (!string.IsNullOrWhiteSpace(fileName)) return true;

        result.Errors.Add(new ValidationIssue
        {
            Message = "MarkDownExtension directive is missing filename",
            DirectivePath = fullDirective,
            SourceFile = templateDocument.FilePath,
            LineNumber = lineNumber,
            SourceContext = line.Trim()
        });
        return false;
    }

    private static bool ValidateFileNameCharacters(string fileName, string line, int lineNumber, MarkdownDocument templateDocument, ValidationResult result)
    {
        var invalidChars = Path.GetInvalidPathChars()
                            .Except([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar])
                            .ToArray();

        if (fileName.IndexOfAny(invalidChars) < 0) return true;

        result.Errors.Add(new ValidationIssue
        {
            Message = $"MarkDownExtension directive contains invalid filename characters: '{fileName}'",
            DirectivePath = fileName,
            SourceFile = templateDocument.FilePath,
            LineNumber = lineNumber,
            SourceContext = line.Trim()
        });
        return false;
    }

    private static bool ValidateSourceFileExists(string fileName, string line, int lineNumber, MarkdownDocument templateDocument, Dictionary<string, string> sourceDictionary, ValidationResult result)
    {
        // Resolve relative path based on template location
        var templateDir = Path.GetDirectoryName(templateDocument.FilePath) ?? string.Empty;
        var resolvedPath = string.IsNullOrEmpty(templateDir)
            ? fileName
            : Path.GetRelativePath(".", Path.Combine(templateDir, fileName));

        var normalizedFileName = PathUtilities.NormalizePathKey(resolvedPath);
        if (sourceDictionary.ContainsKey(normalizedFileName)) return true;

        result.Errors.Add(new ValidationIssue
        {
            Message = $"Source document not found: '{fileName}' (resolved to: '{normalizedFileName}')",
            DirectivePath = fileName,
            SourceFile = templateDocument.FilePath,
            LineNumber = lineNumber,
            SourceContext = line.Trim()
        });
        return false;
    }

    private static void ValidateDuplicateDirective(string fullDirective, string fileName, string line, int lineNumber, MarkdownDocument templateDocument, HashSet<string> processedDirectives, ValidationResult result)
    {
        if (!processedDirectives.Add(fullDirective))
        {
            result.Warnings.Add(new ValidationIssue
            {
                Message = $"Duplicate MarkDownExtension directive found: '{fullDirective}'",
                DirectivePath = fileName,
                SourceFile = templateDocument.FilePath,
                LineNumber = lineNumber,
                SourceContext = line.Trim()
            });
        }
    }

    public ValidationResult Validate(IEnumerable<MarkdownDocument> documents)
    {
        if (documents == null)
            throw new ArgumentNullException(nameof(documents));

        var documentList = documents.ToList();
        var templateDocuments = documentList.Where(doc =>
            MarkdownFileExtensions.HasExtension(doc.FileName, MarkdownFileExtensions.Template) ||
            MarkdownFileExtensions.HasExtension(doc.FileName, MarkdownFileExtensions.Source));
        var sourceDocuments = documentList.Where(doc =>
            MarkdownFileExtensions.HasExtension(doc.FileName, MarkdownFileExtensions.Source) ||
            MarkdownFileExtensions.HasExtension(doc.FileName, MarkdownFileExtensions.Markdown) ||
            MarkdownFileExtensions.HasExtension(doc.FileName, MarkdownFileExtensions.Template));

        var templateList = templateDocuments.ToList();
        var sourceList = sourceDocuments.ToList();
        var combinedResult = new ValidationResult();

        logger.LogInformation("Validating {TemplateCount} template documents", templateList.Count);

        foreach (var template in templateList)
        {
            logger.LogDebug("Validating template: {TemplateFileName}", template.FilePath);

            var result = new ValidationResult();
            var sourceDictionary = sourceList.ToDictionary(
                doc => PathUtilities.NormalizePathKey(doc.FilePath),
                doc => doc.Content,
                PathUtilities.FilePathComparer);

            if (!string.IsNullOrEmpty(template.Content))
            {
                ValidateInsertDirectives(template, sourceDictionary, result);
            }
            var validationResult = result;
            if (validationResult.IsValid)
            {
                combinedResult.ValidFilesCount++;
                logger.LogDebug("Template {TemplateFileName} is valid", template.FilePath);
            }

            // Add template filename context to errors and warnings
            foreach (var error in validationResult.Errors)
            {
                combinedResult.Errors.Add(new ValidationIssue
                {
                    Message = $"[{template.FileName}] {error.Message}",
                    DirectivePath = error.DirectivePath,
                    SourceFile = error.SourceFile,
                    LineNumber = error.LineNumber,
                    SourceContext = error.SourceContext
                });
            }

            foreach (var warning in validationResult.Warnings)
            {
                combinedResult.Warnings.Add(new ValidationIssue
                {
                    Message = $"[{template.FileName}] {warning.Message}",
                    DirectivePath = warning.DirectivePath,
                    SourceFile = warning.SourceFile,
                    LineNumber = warning.LineNumber,
                    SourceContext = warning.SourceContext
                });
            }
        }

        logger.LogInformation("Validation completed for all templates. Found {ErrorCount} errors and {WarningCount} warnings",
            combinedResult.Errors.Count, combinedResult.Warnings.Count);

        return combinedResult;
    }

}
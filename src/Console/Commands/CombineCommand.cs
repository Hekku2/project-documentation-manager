using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;
using ProjectDocumentationManager.Business.Services;
using ProjectDocumentationManager.Console.Services;

namespace ProjectDocumentationManager.Console.Commands;

public class CombineCommand(
    IMarkdownFileCollectorService collector,
    IMarkdownCombinationService combiner,
    IMarkdownDocumentFileWriterService writer,
    IAnsiConsole ansiConsole,
    IFileSystemService fileSystemService) : AsyncCommand<CombineCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<INPUT_FOLDER>")]
        [Description("Input folder containing markdown templates")]
        public required string InputFolder { get; set; }

        [CommandArgument(1, "<OUTPUT_FOLDER>")]
        [Description("Output folder for combined markdown files")]
        public required string OutputFolder { get; set; }
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        try
        {
            if (!fileSystemService.DirectoryExists(settings.InputFolder))
            {
                ansiConsole.MarkupLine($"[red]Error: Input folder '{settings.InputFolder}' does not exist[/]");
                return CommandConstants.CommandError;
            }

            ansiConsole.MarkupLine($"[green]Collecting markdown files from:[/] {settings.InputFolder}");
            var allDocuments = await collector.CollectAllMarkdownFilesAsync(settings.InputFolder);

            var templateFiles = allDocuments.Where(doc => doc.FileName.EndsWith(".mdext"));
            var sourceFiles = allDocuments.Where(doc => doc.FileName.EndsWith(".mdsrc"));
            var markdownFiles = allDocuments.Where(doc => doc.FileName.EndsWith(".md"));

            if (!templateFiles.Any())
            {
                ansiConsole.MarkupLine("[yellow]Warning: No markdown template files found[/]");
                return CommandConstants.CommandError;
            }

            ansiConsole.MarkupLine($"Found {templateFiles.Count()} template files, {sourceFiles.Count()} source files, and {markdownFiles.Count()} markdown files");
            var validationResult = combiner.Validate(allDocuments);

            if (!validationResult.IsValid)
            {
                ansiConsole.MarkupLine("[red]Validation errors found:[/]");
                foreach (var error in validationResult.Errors)
                {
                    ansiConsole.MarkupLine($"  [red]- {error.Message}[/]");
                }
                return CommandConstants.CommandError;
            }

            var processedDocuments = combiner.BuildDocumentation(allDocuments);
            fileSystemService.EnsureDirectoryExists(settings.OutputFolder);
            await writer.WriteDocumentsToFolderAsync(processedDocuments, settings.OutputFolder);

            ansiConsole.MarkupLine($"[green]✓ Markdown combination completed![/]");
            ansiConsole.MarkupLine($"Output location: [blue]{fileSystemService.GetFullPath(settings.OutputFolder)}[/]");

            return CommandConstants.CommandOk;
        }
        catch (Exception ex)
        {
            ansiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return CommandConstants.CommandError;
        }
    }
}
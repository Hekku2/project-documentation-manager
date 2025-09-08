using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;
using ProjectDocumentationManager.Business.Services;

namespace ProjectDocumentationManager.Console.Commands;

public class CombineCommand(
    IMarkdownFileCollectorService collector,
    IMarkdownCombinationService combiner,
    IMarkdownDocumentFileWriterService writer) : AsyncCommand<CombineCommand.Settings>
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
            if (!Directory.Exists(settings.InputFolder))
            {
                AnsiConsole.MarkupLine($"[red]Error: Input folder '{settings.InputFolder}' does not exist[/]");
                return CommandConstants.CommandError;
            }

            AnsiConsole.MarkupLine($"[green]Collecting markdown files from:[/] {settings.InputFolder}");
            var (templateFiles, sourceFiles) = await collector.CollectAllMarkdownFilesAsync(settings.InputFolder);

            if (!templateFiles.Any())
            {
                AnsiConsole.MarkupLine("[yellow]Warning: No markdown template files found[/]");
                return CommandConstants.CommandError;
            }

            AnsiConsole.MarkupLine($"Found {templateFiles.Count()} template files and {sourceFiles.Count()} source files");

            var validationResult = combiner.Validate(templateFiles, sourceFiles);

            if (!validationResult.IsValid)
            {
                AnsiConsole.MarkupLine("[red]Validation errors found:[/]");
                foreach (var error in validationResult.Errors)
                {
                    AnsiConsole.MarkupLine($"  [red]- {error.Message}[/]");
                }
                return CommandConstants.CommandError;
            }

            var processedDocuments = combiner.BuildDocumentation(templateFiles, sourceFiles);
            Directory.CreateDirectory(settings.OutputFolder);
            await writer.WriteDocumentsToFolderAsync(processedDocuments, settings.OutputFolder);

            AnsiConsole.MarkupLine($"[green]✓ Markdown combination completed![/]");
            AnsiConsole.MarkupLine($"Output location: [blue]{Path.GetFullPath(settings.OutputFolder)}[/]");

            return CommandConstants.CommandOk;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return CommandConstants.CommandError;
        }
    }
}
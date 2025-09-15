using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;
using ProjectDocumentationManager.Business;
using ProjectDocumentationManager.Business.Services;
using ProjectDocumentationManager.Console.Services;

namespace ProjectDocumentationManager.Console.Commands;

public class ValidateCommand(
    IAnsiConsole ansiConsole,
    IMarkdownFileCollectorService collector,
    IMarkdownCombinationService combiner,
    IFileSystemService fileSystemService) : AsyncCommand<ValidateCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<INPUT_FOLDER>")]
        [Description("Input folder containing markdown templates to validate")]
        public required string InputFolder { get; set; }
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

            ansiConsole.MarkupLine($"[green]Validating markdown files in:[/] {settings.InputFolder}");
            var allDocuments = await collector.CollectAllMarkdownFilesAsync(settings.InputFolder);
            var docList = allDocuments.ToList();
            var templateFiles = docList.Where(doc => doc.FileName.EndsWith(MarkdownFileExtensions.Template, System.StringComparison.OrdinalIgnoreCase)).ToList();
            var sourceFiles = docList.Where(doc => doc.FileName.EndsWith(MarkdownFileExtensions.Source, System.StringComparison.OrdinalIgnoreCase)).ToList();
            var markdownFiles = docList.Where(doc => doc.FileName.EndsWith(MarkdownFileExtensions.Markdown, System.StringComparison.OrdinalIgnoreCase)).ToList();
            if (!templateFiles.Any())
            {
                ansiConsole.MarkupLine("[yellow]Warning: No markdown template files found[/]");
                return CommandConstants.CommandOk;
            }

            ansiConsole.MarkupLine($"Found {templateFiles.Count()} template files, {sourceFiles.Count()} source files, and {markdownFiles.Count()} markdown files");

            var validationResult = combiner.Validate(allDocuments);
            var totalFiles = templateFiles.Count();

            var table = CreateSummaryTable(validationResult, totalFiles);

            ansiConsole.Write(table);

            if (!validationResult.IsValid)
            {
                ansiConsole.MarkupLine($"\n[red]Validation completed with errors[/]");

                if (validationResult.Errors.Any())
                {
                    ansiConsole.MarkupLine("\n[yellow]Issues found:[/]");
                    foreach (var error in validationResult.Errors)
                    {
                        ansiConsole.MarkupLine($"[red]- {error.Message}[/]");
                    }
                }

                return CommandConstants.CommandError;
            }

            ansiConsole.MarkupLine($"\n[green]✓ All {totalFiles} files validated successfully![/]");
            return CommandConstants.CommandOk;
        }
        catch (Exception ex)
        {
            ansiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return CommandConstants.CommandError;
        }
    }

    private static Table CreateSummaryTable(Business.Models.ValidationResult validationResult, int totalFiles)
    {
        // Summary
        var table = new Table();
        table.AddColumn("Status");
        table.AddColumn("Count");

        table.AddRow("[green]Valid files[/]", validationResult.ValidFilesCount.ToString());
        table.AddRow("[yellow]Files with warnings[/]", validationResult.WarningFilesCount.ToString());
        table.AddRow("[red]Invalid files[/]", validationResult.InvalidFilesCount.ToString());

        table.AddRow("Total files", totalFiles.ToString());
        return table;
    }
}
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;
using ProjectDocumentationManager.Business.Services;

namespace ProjectDocumentationManager.Console.Commands;

public class ValidateCommand(
    IMarkdownFileCollectorService collector,
    IMarkdownCombinationService combiner) : AsyncCommand<ValidateCommand.Settings>
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

            if (!Directory.Exists(settings.InputFolder))
            {
                AnsiConsole.MarkupLine($"[red]Error: Input folder '{settings.InputFolder}' does not exist[/]");
                return CommandConstants.CommandError;
            }

            AnsiConsole.MarkupLine($"[green]Validating markdown files in:[/] {settings.InputFolder}");
            var (templateFiles, sourceFiles) = await collector.CollectAllMarkdownFilesAsync(settings.InputFolder);

            if (!templateFiles.Any())
            {
                AnsiConsole.MarkupLine("[yellow]Warning: No markdown template files found[/]");
                return CommandConstants.CommandOk;
            }

            AnsiConsole.MarkupLine($"Found {templateFiles.Count()} template files and {sourceFiles.Count()} source files");

            var validationResult = combiner.Validate(templateFiles, sourceFiles);
            var totalFiles = templateFiles.Count();

            var table = CreateSummaryTable(validationResult, totalFiles);

            AnsiConsole.Write(table);

            if (!validationResult.IsValid)
            {
                AnsiConsole.MarkupLine($"\n[red]Validation completed with errors[/]");

                if (validationResult.Errors.Any())
                {
                    AnsiConsole.MarkupLine("\n[yellow]Issues found:[/]");
                    foreach (var error in validationResult.Errors)
                    {
                        AnsiConsole.MarkupLine($"[red]- {error.Message}[/]");
                    }
                }

                return CommandConstants.CommandError;
            }

            AnsiConsole.MarkupLine($"\n[green]✓ All {totalFiles} files validated successfully![/]");
            return CommandConstants.CommandOk;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return CommandConstants.CommandError;
        }
    }

    private static Table CreateSummaryTable(Business.Models.ValidationResult validationResult, int totalFiles)
    {
        // Summary
        var table = new Table();
        table.AddColumn("Status");
        table.AddColumn("Count");

        if (validationResult.IsValid)
        {
            table.AddRow("[green]Valid files[/]", totalFiles.ToString());
            table.AddRow("[red]Invalid files[/]", "0");
        }
        else
        {
            table.AddRow("[green]Valid files[/]", "0");
            table.AddRow("[red]Invalid files[/]", totalFiles.ToString());
        }
        table.AddRow("Total files", totalFiles.ToString());
        return table;
    }
}
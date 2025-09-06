using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using Business.Services;

namespace Console.Commands;

public class CombineCommand : AsyncCommand<CombineCommand.Settings>
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
        var host = CreateHost();
        
        try
        {
            var collector = host.Services.GetRequiredService<IMarkdownFileCollectorService>();
            var combiner = host.Services.GetRequiredService<IMarkdownCombinationService>();
            var writer = host.Services.GetRequiredService<IMarkdownDocumentFileWriterService>();
            var logger = host.Services.GetRequiredService<ILogger<CombineCommand>>();

            if (!Directory.Exists(settings.InputFolder))
            {
                AnsiConsole.MarkupLine($"[red]Error: Input folder '{settings.InputFolder}' does not exist[/]");
                return 1;
            }

            Directory.CreateDirectory(settings.OutputFolder);

            AnsiConsole.MarkupLine($"[green]Collecting markdown files from:[/] {settings.InputFolder}");
            var (templateFiles, sourceFiles) = await collector.CollectAllMarkdownFilesAsync(settings.InputFolder);

            if (!templateFiles.Any())
            {
                AnsiConsole.MarkupLine("[yellow]Warning: No markdown template files found[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"Found {templateFiles.Count()} template files and {sourceFiles.Count()} source files");

            var processedDocuments = combiner.BuildDocumentation(templateFiles, sourceFiles);
            var validationResult = combiner.Validate(templateFiles, sourceFiles);

            if (!validationResult.IsValid)
            {
                AnsiConsole.MarkupLine("[red]Validation errors found:[/]");
                foreach (var error in validationResult.Errors)
                {
                    AnsiConsole.MarkupLine($"  [red]- {error.Message}[/]");
                }
                return 1;
            }

            await writer.WriteDocumentsToFolderAsync(processedDocuments, settings.OutputFolder);

            AnsiConsole.MarkupLine($"[green]✓ Markdown combination completed![/]");
            AnsiConsole.MarkupLine($"Output location: [blue]{Path.GetFullPath(settings.OutputFolder)}[/]");
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddTransient<IMarkdownFileCollectorService, MarkdownFileCollectorService>();
                services.AddTransient<IMarkdownCombinationService, MarkdownCombinationService>();
                services.AddTransient<IMarkdownDocumentFileWriterService, MarkdownDocumentFileWriterService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .Build();
    }
}
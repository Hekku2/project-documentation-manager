using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using MarkdownCompiler.Console.Commands;
using MarkdownCompiler.Console.Services;

namespace MarkdownCompiler.Console;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            services.AddSingleton<IMarkdownFileCollectorService, MarkdownFileCollectorService>();
            services.AddSingleton<IMarkdownCombinationService, MarkdownCombinationService>();
            services.AddSingleton<IMarkdownDocumentFileWriterService, MarkdownDocumentFileWriterService>();
            services.AddTransient<IFileSystemService, FileSystemService>();
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        var app = new CommandApp(new TypeRegistrar(hostBuilder));

        app.Configure(config =>
        {
            config.AddCommand<CombineCommand>("combine")
                .WithDescription("Combine markdown files from templates");

            config.AddCommand<ValidateCommand>("validate")
                .WithDescription("Validate markdown templates and sources");
        });


        var code = await app.RunAsync(args);
        return code;
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using ProjectDocumentationManager.Business.Services;
using ProjectDocumentationManager.Console.Commands;
using ProjectDocumentationManager.Console.Services;

namespace ProjectDocumentationManager.Console;

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
            services.AddTransient<CombineCommand>();
            services.AddTransient<ValidateCommand>();
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
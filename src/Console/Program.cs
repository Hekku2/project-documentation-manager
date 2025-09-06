using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using Console.Commands;
using Business.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddTransient<IMarkdownFileCollectorService, MarkdownFileCollectorService>();
        services.AddTransient<IMarkdownCombinationService, MarkdownCombinationService>();
        services.AddTransient<IMarkdownDocumentFileWriterService, MarkdownDocumentFileWriterService>();
        services.AddTransient<CombineCommand>();
        services.AddTransient<ValidateCommand>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

var app = new CommandApp(new Console.TypeRegistrar(host.Services));

app.Configure(config =>
{
    config.AddCommand<CombineCommand>("combine")
        .WithDescription("Combine markdown files from templates");

    config.AddCommand<ValidateCommand>("validate")
        .WithDescription("Validate markdown templates and sources");
});

return await app.RunAsync(args);
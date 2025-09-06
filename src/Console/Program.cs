using Spectre.Console.Cli;
using Console.Commands;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<CombineCommand>("combine")
        .WithDescription("Combine markdown files from templates");

    config.AddCommand<ValidateCommand>("validate")
        .WithDescription("Validate markdown templates and sources");
});

return await app.RunAsync(args);
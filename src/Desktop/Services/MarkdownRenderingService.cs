using Markdig;

namespace Desktop.Services;

public class MarkdownRenderingService : IMarkdownRenderingService
{
    private readonly MarkdownPipeline _pipeline;
    
    public MarkdownRenderingService()
    {
        // Configure Markdig with commonly useful extensions
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // Includes tables, task lists, auto-identifiers, etc.
            .Build();
    }
    
    public string ConvertToHtml(string markdownText)
    {
        if (string.IsNullOrEmpty(markdownText))
            return string.Empty;
            
        return Markdown.ToHtml(markdownText, _pipeline);
    }
}
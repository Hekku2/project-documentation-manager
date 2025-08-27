namespace Desktop.Services;

public interface IMarkdownRenderingService
{
    /// <summary>
    /// Converts markdown text to HTML using Markdig
    /// </summary>
    /// <param name="markdownText">The markdown text to convert</param>
    /// <returns>HTML representation of the markdown</returns>
    string ConvertToHtml(string markdownText);
}
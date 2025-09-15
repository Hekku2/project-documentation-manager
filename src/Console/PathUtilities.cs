namespace MarkdownCompiler.Console;

/// <summary>
/// Utilities for cross-platform path handling and comparison
/// </summary>
public static class PathUtilities
{
    /// <summary>
    /// Gets the appropriate string comparer for file paths.
    /// For this application, we use case-insensitive comparison on all platforms for better user experience
    /// with markdown file references, even though Linux filesystems are case-sensitive
    /// </summary>
    public static StringComparer FilePathComparer => StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Gets the appropriate string comparison for file paths.
    /// For this application, we use case-insensitive comparison on all platforms for better user experience
    /// with markdown file references, even though Linux filesystems are case-sensitive
    /// </summary>
    public static StringComparison FilePathComparison => StringComparison.OrdinalIgnoreCase;

    /// <summary>
    /// Normalizes a path by converting all directory separators to the platform-specific separator
    /// for consistent cross-platform path comparison
    /// </summary>
    /// <param name="path">The path to normalize</param>
    /// <returns>The normalized path for use as a dictionary key</returns>
    public static string NormalizePathKey(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        // Normalize all separators to the platform-specific separator
        // Handle both Windows and Unix style separators regardless of platform
        return path.Replace('\\', Path.DirectorySeparatorChar)
                  .Replace('/', Path.DirectorySeparatorChar);
    }
}
using System;
using System.IO;
using System.Linq;
using Desktop.Factories;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace Desktop.Services;

public class FileSystemChangeHandler(ILogger<FileSystemChangeHandler> logger, IFileService fileService) : IFileSystemChangeHandler
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    public void HandleFileSystemChange(FileSystemChangedEventArgs eventArguments, FileSystemItemViewModel rootViewModel, IFileSystemItemViewModelFactory viewModelFactory)
    {
        var parentViewModel = FindParentForPath(eventArguments.Path, rootViewModel);
        if (parentViewModel == null)
        {
            logger.LogWarning("Parent view model not found for path: {Path}", eventArguments.Path);
            return;
        }

        switch (eventArguments.ChangeType)
        {
            case FileSystemChangeType.Created:
                HandleItemCreated(parentViewModel, eventArguments.Path, eventArguments.IsDirectory, viewModelFactory);
                break;
            case FileSystemChangeType.Deleted:
                HandleItemDeleted(parentViewModel, eventArguments.Path);
                break;
            case FileSystemChangeType.Renamed:
                if (!string.IsNullOrEmpty(eventArguments.OldPath))
                {
                    HandleItemDeleted(parentViewModel, eventArguments.OldPath);
                    HandleItemCreated(parentViewModel, eventArguments.Path, eventArguments.IsDirectory, viewModelFactory);
                }
                break;
            case FileSystemChangeType.Changed:
                HandleItemChanged(eventArguments.Path, rootViewModel);
                break;
        }
    }

    private static FileSystemItemViewModel? FindParentForPath(string filePath, FileSystemItemViewModel rootViewModel)
    {
        var parentPath = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(parentPath))
            return null;

        return FindViewModelByPath(parentPath, rootViewModel);
    }

    private static FileSystemItemViewModel? FindViewModelByPath(string path, FileSystemItemViewModel rootViewModel)
    {
        // Normalize both paths and trim any trailing separators
        var rootFull = Path.GetFullPath(rootViewModel.Item.FullPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var targetFull = Path.GetFullPath(path);

        // Exact match
        if (string.Equals(rootFull, targetFull, PathComparison))
            return rootViewModel;

        // Only treat as descendant if it begins with root + separator
        var rootWithSep = rootFull + Path.DirectorySeparatorChar;
        if (!targetFull.StartsWith(rootWithSep, PathComparison))
            return null;

        // If children are loaded, search through them
        if (rootViewModel.HasChildrenLoaded)
            …
        {
            foreach (var child in rootViewModel.Children)
            {
                var result = FindViewModelByPath(path, child);
                if (result != null)
                    return result;
            }
        }

        return null;
    }

    private void HandleItemCreated(FileSystemItemViewModel parentViewModel, string itemPath, bool isDirectory, IFileSystemItemViewModelFactory viewModelFactory)
    {
        var fileSystemItem = fileService.CreateFileSystemItem(itemPath, isDirectory);

        // Always update the underlying data structure first
        if (parentViewModel.Item.IsDirectory)
        {
            var childFileName = Path.GetFileName(itemPath);
            var existingItem = parentViewModel.Item.Children.FirstOrDefault(c =>
                string.Equals(c.Name, childFileName, PathComparison));

            if (existingItem == null && fileSystemItem != null)
            {
                parentViewModel.Item.Children.Add(fileSystemItem);
            }
        }

        // If parent's UI children are not loaded, no need to update UI immediately
        if (!parentViewModel.HasChildrenLoaded)
        {
            return;
        }

        // Check if item already exists
        var fileName = Path.GetFileName(itemPath);
        if (parentViewModel.Children.Any(c => string.Equals(c.Name, fileName, PathComparison)))
            return;

        // Create the new file system item
        if (fileSystemItem == null)
            return;

        var newViewModel = viewModelFactory.CreateWithChildren(fileSystemItem);

        // Mark the new item as visible since its parent is expanded and loaded
        if (fileSystemItem.IsDirectory)
        {
            newViewModel.IsVisible = true;
        }

        // Find the correct position to insert (directories first, then alphabetical)
        var insertIndex = 0;
        for (var i = 0; i < parentViewModel.Children.Count; i++)
        {
            var existingChild = parentViewModel.Children[i];

            // Directories come before files
            if (isDirectory && !existingChild.IsDirectory)
                break;
            if (!isDirectory && existingChild.IsDirectory)
            {
                insertIndex = i + 1;
                continue;
            }

            // Within the same type, sort alphabetically
            if (string.Compare(fileName, existingChild.Name, PathComparison) < 0)
                break;

            insertIndex = i + 1;
        }

        parentViewModel.Children.Insert(insertIndex, newViewModel);
    }

    private static void HandleItemDeleted(FileSystemItemViewModel parentViewModel, string itemPath)
    {
        var fileName = Path.GetFileName(itemPath);

        // Remove from UI children if loaded
        if (parentViewModel.HasChildrenLoaded)
        {
            var itemToRemove = parentViewModel.Children.FirstOrDefault(c =>
                string.Equals(c.Name, fileName, PathComparison));

            if (itemToRemove != null)
            {
                parentViewModel.Children.Remove(itemToRemove);
            }
        }

        // Also remove from underlying data structure
        if (parentViewModel.Item.IsDirectory)
        {
            var dataItemToRemove = parentViewModel.Item.Children.FirstOrDefault(c =>
                string.Equals(c.Name, fileName, PathComparison));

            if (dataItemToRemove != null)
            {
                parentViewModel.Item.Children.Remove(dataItemToRemove);
            }
        }
    }

    private void HandleItemChanged(string itemPath, FileSystemItemViewModel rootViewModel)
    {
        var itemViewModel = FindViewModelByPath(itemPath, rootViewModel);
        if (itemViewModel != null && !itemViewModel.IsDirectory)
        {
            // Update last modified time if needed
            try
            {
                var lastModified = File.GetLastWriteTime(itemPath);
                if (itemViewModel.Item.LastModified != lastModified)
                {
                    itemViewModel.Item.LastModified = lastModified;
                    itemViewModel.Item.Size = new FileInfo(itemPath).Length;
                    // Trigger property change notifications if needed
                }
            }
            catch
            {
                // Ignore errors when updating file info
            }
        }
    }
}
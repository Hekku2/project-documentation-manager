using System;
using System.IO;
using System.Linq;
using Desktop.Factories;
using Desktop.Models;
using Desktop.ViewModels;

namespace Desktop.Services;

public class FileSystemChangeHandler(IFileService fileService) : IFileSystemChangeHandler
{
    public void HandleFileSystemChange(FileSystemChangedEventArgs eventArguments, FileSystemItemViewModel rootViewModel, IFileSystemItemViewModelFactory viewModelFactory)
    {
        var parentViewModel = FindParentForPath(eventArguments.Path, rootViewModel);
        if (parentViewModel == null)
            return;

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
        if (string.Equals(rootViewModel.Item.FullPath, path, StringComparison.OrdinalIgnoreCase))
            return rootViewModel;

        // Check if the requested path is a child of this item
        if (!path.StartsWith(rootViewModel.Item.FullPath, StringComparison.OrdinalIgnoreCase))
            return null;

        // If children are loaded, search through them
        if (rootViewModel.HasChildrenLoaded)
        {
            foreach (var child in rootViewModel.Children)
            {
                var result = FindViewModelByPath(path, child);
                if (result != null)
                    return result;
            }
        }

        // If this is a directory and the path could be a child, return this as potential parent
        if (rootViewModel.Item.IsDirectory && path.StartsWith(rootViewModel.Item.FullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return rootViewModel; // Return this directory as the closest parent found
        }

        return null;
    }

    private void HandleItemCreated(FileSystemItemViewModel parentViewModel, string itemPath, bool isDirectory, IFileSystemItemViewModelFactory viewModelFactory)
    {
        // Always update the underlying data structure first
        if (parentViewModel.Item.IsDirectory)
        {
            var childFileName = Path.GetFileName(itemPath);
            var existingItem = parentViewModel.Item.Children.FirstOrDefault(c => 
                string.Equals(c.Name, childFileName, StringComparison.OrdinalIgnoreCase));
            
            if (existingItem == null)
            {
                var childItem = fileService.CreateFileSystemItem(itemPath, isDirectory);
                if (childItem != null)
                {
                    parentViewModel.Item.Children.Add(childItem);
                }
            }
        }

        // If parent's UI children are not loaded, no need to update UI immediately
        if (!parentViewModel.HasChildrenLoaded)
        {
            return;
        }

        // Check if item already exists
        var fileName = Path.GetFileName(itemPath);
        if (parentViewModel.Children.Any(c => string.Equals(c.Name, fileName, StringComparison.OrdinalIgnoreCase)))
            return;

        // Create the new file system item
        var newItem = fileService.CreateFileSystemItem(itemPath, isDirectory);
        if (newItem == null)
            return;

        var newViewModel = viewModelFactory.CreateChild(newItem, true);

        // Mark the new item as visible since its parent is expanded and loaded
        if (newItem.IsDirectory)
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
            if (string.Compare(fileName, existingChild.Name, StringComparison.OrdinalIgnoreCase) < 0)
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
                string.Equals(c.Name, fileName, StringComparison.OrdinalIgnoreCase));
            
            if (itemToRemove != null)
            {
                parentViewModel.Children.Remove(itemToRemove);
            }
        }
        
        // Also remove from underlying data structure
        if (parentViewModel.Item.IsDirectory)
        {
            var dataItemToRemove = parentViewModel.Item.Children.FirstOrDefault(c => 
                string.Equals(c.Name, fileName, StringComparison.OrdinalIgnoreCase));
            
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
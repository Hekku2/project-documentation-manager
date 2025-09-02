using System;
using Desktop.Models;
using Desktop.Services;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace Desktop.Factories;

public sealed class FileSystemItemViewModelFactory(
    ILoggerFactory loggerFactory,
    IFileSystemExplorerService fileSystemExplorerService,
    IFileSystemChangeHandler fileSystemChangeHandler,
    Action<string> onItemSelected,
    Action<string> onItemPreview) : IFileSystemItemViewModelFactory
{
    public FileSystemItemViewModel CreateChild(FileSystemItem item, bool loadChildren)
    {
        return Create(item, loadChildren: loadChildren);
    }

    public FileSystemItemViewModel CreateRoot(FileSystemItem item)
    {
        return Create(item, loadChildren: true);
    }

    private FileSystemItemViewModel Create(
        FileSystemItem item,
        bool loadChildren = false)
    {
        return new FileSystemItemViewModel(
            loggerFactory.CreateLogger<FileSystemItemViewModel>(),
            this,
            fileSystemExplorerService,
            fileSystemChangeHandler,
            item,
            loadChildren,
            onItemSelected,
            onItemPreview);
    }
}

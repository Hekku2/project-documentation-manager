using System;
using Desktop.Models;
using Desktop.Services;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace Desktop.Factories;

public sealed class FileSystemItemViewModelFactory(
    ILoggerFactory loggerFactory,
    IFileSystemExplorerService fileSystemExplorerService,
    Action<string> onItemSelected,
    Action<string> onItemPreview) : IFileSystemItemViewModelFactory
{
    public FileSystemItemViewModel CreateWithChildren(FileSystemItem item)
    {
        return Create(item, loadChildren: true);
    }

    public FileSystemItemViewModel Create(FileSystemItem item)
    {
        return Create(item, loadChildren: false);
    }

    private FileSystemItemViewModel Create(
        FileSystemItem item,
        bool loadChildren)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(fileSystemExplorerService);
        ArgumentNullException.ThrowIfNull(onItemSelected);
        ArgumentNullException.ThrowIfNull(onItemPreview);
        ArgumentNullException.ThrowIfNull(item);

        return new FileSystemItemViewModel(
            loggerFactory.CreateLogger<FileSystemItemViewModel>(),
            this,
            fileSystemExplorerService,
            item,
            loadChildren,
            onItemSelected,
            onItemPreview);
    }
}

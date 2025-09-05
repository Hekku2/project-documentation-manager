using System;
using Desktop.Models;
using Desktop.Services;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace Desktop.Factories;

public sealed class FileSystemItemViewModelFactory(
    ILoggerFactory loggerFactory,
    IFileSystemExplorerService fileSystemExplorerService,
    IFileService fileService,
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
        ArgumentNullException.ThrowIfNull(fileService);
        ArgumentNullException.ThrowIfNull(onItemSelected);
        ArgumentNullException.ThrowIfNull(onItemPreview);
        ArgumentNullException.ThrowIfNull(item);

        return new FileSystemItemViewModel(
            loggerFactory.CreateLogger<FileSystemItemViewModel>(),
            this,
            fileSystemExplorerService,
            fileService,
            item,
            loadChildren,
            onItemSelected,
            onItemPreview);
    }
}

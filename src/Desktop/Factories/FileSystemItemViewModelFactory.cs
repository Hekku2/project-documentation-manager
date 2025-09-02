using System;
using Desktop.Models;
using Desktop.Services;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace Desktop.Factories;

public class FileSystemItemViewModelFactory(
    ILoggerFactory loggerFactory,
    IFileService fileService,
    IFileSystemExplorerService fileSystemExplorerService,
    IFileSystemChangeHandler fileSystemChangeHandler,
    Action<string> onItemSelected,
    Action<string> onItemPreview) : IFileSystemItemViewModelFactory
{
    public FileSystemItemViewModel CreateChild(FileSystemItem item)
    {
        return Create(item, isRoot: false);
    }

    public FileSystemItemViewModel CreateRoot(FileSystemItem item)
    {
        return Create(item, isRoot: true);
    }

    private FileSystemItemViewModel Create(
        FileSystemItem item,
        bool isRoot = false)
    {
        return new FileSystemItemViewModel(
            loggerFactory.CreateLogger<FileSystemItemViewModel>(),
            this,
            fileSystemExplorerService,
            fileSystemChangeHandler,
            item,
            isRoot,
            fileService,
            onItemSelected,
            onItemPreview);
    }
}

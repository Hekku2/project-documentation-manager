using System;
using Desktop.Models;
using Desktop.Services;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace Desktop.Factories;

public interface IFileSystemItemViewModelFactory
{
    FileSystemItemViewModel Create(
        FileSystemItem item,
        bool isRoot = false,
        Action<string>? onFileSelected = null,
        Action<string>? onFilePreview = null);
}

public class FileSystemItemViewModelFactory(
    ILoggerFactory loggerFactory,
    IFileService fileService,
    IFileSystemExplorerService fileSystemExplorerService) : IFileSystemItemViewModelFactory
{
    public FileSystemItemViewModel Create(
        FileSystemItem item,
        bool isRoot = false,
        Action<string>? onFileSelected = null,
        Action<string>? onFilePreview = null)
    {
        return new FileSystemItemViewModel(
            loggerFactory.CreateLogger<FileSystemItemViewModel>(),
            this,
            fileSystemExplorerService,
            item,
            isRoot,
            fileService,
            onFileSelected,
            onFilePreview);
    }
}

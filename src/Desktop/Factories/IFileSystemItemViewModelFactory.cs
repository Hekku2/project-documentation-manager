using Desktop.Models;
using Desktop.ViewModels;

namespace Desktop.Factories;

public interface IFileSystemItemViewModelFactory
{
    FileSystemItemViewModel CreateWithChildren(
        FileSystemItem item);

    FileSystemItemViewModel Create(
        FileSystemItem item);
}

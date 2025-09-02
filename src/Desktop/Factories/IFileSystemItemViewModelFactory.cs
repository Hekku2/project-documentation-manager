using Desktop.Models;
using Desktop.ViewModels;

namespace Desktop.Factories;

public interface IFileSystemItemViewModelFactory
{
    FileSystemItemViewModel CreateChild(
        FileSystemItem item, bool loadChildren);

    FileSystemItemViewModel CreateRoot(
        FileSystemItem item);
}

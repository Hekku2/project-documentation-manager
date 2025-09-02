using Desktop.Factories;
using Desktop.ViewModels;

namespace Desktop.Services;

public interface IFileSystemChangeHandler
{
    void HandleFileSystemChange(FileSystemChangedEventArgs e, FileSystemItemViewModel rootViewModel, IFileSystemItemViewModelFactory viewModelFactory);
}
using Desktop.Factories;
using Desktop.ViewModels;

namespace Desktop.Services;

public interface IFileSystemChangeHandler
{
    void HandleFileSystemChange(FileSystemChangedEventArgs eventArguments, FileSystemItemViewModel rootViewModel, IFileSystemItemViewModelFactory viewModelFactory);
}
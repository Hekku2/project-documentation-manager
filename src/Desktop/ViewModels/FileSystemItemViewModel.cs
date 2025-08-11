using System.Collections.ObjectModel;
using System.Linq;
using Desktop.Models;

namespace Desktop.ViewModels;

public class FileSystemItemViewModel : ViewModelBase
{
    private bool _isExpanded;
    private bool _isSelected;

    public FileSystemItemViewModel(FileSystemItem item)
    {
        Item = item;
        Children = new ObservableCollection<FileSystemItemViewModel>();
        
        // Convert child items to ViewModels
        foreach (var child in item.Children.OrderBy(c => !c.IsDirectory).ThenBy(c => c.Name))
        {
            Children.Add(new FileSystemItemViewModel(child));
        }
        
        // Auto-expand root directories
        IsExpanded = item.IsDirectory && IsRoot(item);
    }

    public FileSystemItem Item { get; }
    
    public ObservableCollection<FileSystemItemViewModel> Children { get; }

    public string Name => Item.DisplayName;
    public string FullPath => Item.FullPath;
    public bool IsDirectory => Item.IsDirectory;
    public bool HasChildren => Children.Count > 0;

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private static bool IsRoot(FileSystemItem item)
    {
        // Consider it root if it's a directory and parent path suggests it's a project root
        return item.IsDirectory && (
            item.Name.Contains("project") ||
            item.FullPath.Contains("projects") ||
            item.Children.Any(c => c.Name == "src" || c.Name == "README.md")
        );
    }
}
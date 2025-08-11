
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desktop.Configuration;
using Desktop.Services;
using Desktop.Models;
using System.Threading.Tasks;
using System.Linq;

namespace Desktop.Views;

public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly ApplicationOptions _applicationOptions;
    private readonly IFileService _fileService;

    public MainWindow(ILogger<MainWindow> logger, IOptions<ApplicationOptions> applicationOptions, IFileService fileService)
    {
        _logger = logger;
        _applicationOptions = applicationOptions.Value;
        _fileService = fileService;
        InitializeComponent();
        _logger.LogInformation("MainWindow initialized with dependency injection");
        _logger.LogInformation("Default theme: {Theme}", _applicationOptions.DefaultTheme);
        _logger.LogInformation("Default project folder: {Folder}", _applicationOptions.DefaultProjectFolder);
        
        // Populate TreeView asynchronously
        _ = Task.Run(PopulateFileExplorerAsync);
    }

    private async Task PopulateFileExplorerAsync()
    {
        try
        {
            _logger.LogInformation("Loading file structure...");
            var fileStructure = await _fileService.GetFileStructureAsync();
            
            if (fileStructure != null)
            {
                // Update UI on the UI thread
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    PopulateTreeView(fileStructure);
                });
                _logger.LogInformation("File structure loaded successfully");
            }
            else
            {
                _logger.LogWarning("Failed to load file structure");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error loading file structure");
        }
    }

    private void PopulateTreeView(FileSystemItem rootItem)
    {
        var fileExplorer = this.FindControl<TreeView>("FileExplorer");
        if (fileExplorer == null)
        {
            _logger.LogWarning("FileExplorer TreeView not found");
            return;
        }

        fileExplorer.Items.Clear();
        fileExplorer.Items.Add(CreateTreeViewItem(rootItem));
    }

    private TreeViewItem CreateTreeViewItem(FileSystemItem item)
    {
        var treeViewItem = new TreeViewItem
        {
            Header = item.DisplayName,
            IsExpanded = item.IsDirectory && item.Name == System.IO.Path.GetFileName(_applicationOptions.DefaultProjectFolder)
        };

        // Add children if it's a directory
        if (item.IsDirectory && item.HasChildren)
        {
            foreach (var child in item.Children.OrderBy(c => !c.IsDirectory).ThenBy(c => c.Name))
            {
                treeViewItem.Items.Add(CreateTreeViewItem(child));
            }
        }

        return treeViewItem;
    }
}
using Avalonia.Controls;
using Desktop.Views;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desktop.Configuration;
using Desktop.Services;
using Desktop.Models;
using NSubstitute;
using Business.Services;

namespace Desktop.UITests;

[NonParallelizable]
public abstract class MainWindowTestBase
{
    protected ILogger<MainWindowViewModel> _vmLogger = null!;
    protected ILogger<EditorTabBarViewModel> _tabBarLogger = null!;
    protected ILogger<EditorContentViewModel> _contentLogger = null!;
    protected ILogger<EditorStateService> _stateLogger = null!;
    protected IOptions<ApplicationOptions> _options = null!;
    protected IFileService _fileService = null!;
    protected IServiceProvider serviceProvider = null!;
    protected IMarkdownCombinationService _markdownCombinationService = null!;
    protected IMarkdownFileCollectorService _markdownFileCollectorService = null!;
    protected Desktop.Services.IMarkdownRenderingService _markdownRenderingService = null!;
    protected EditorStateService _editorStateService = null!;
    protected Logging.ILogTransitionService _logTransitionService = null!;

    [SetUp]
    public void Setup()
    {
        _vmLogger = Substitute.For<ILogger<MainWindowViewModel>>();
        _tabBarLogger = Substitute.For<ILogger<EditorTabBarViewModel>>();
        _contentLogger = Substitute.For<ILogger<EditorContentViewModel>>();
        _stateLogger = Substitute.For<ILogger<EditorStateService>>();
        _options = Options.Create(new ApplicationOptions());
        _fileService = Substitute.For<IFileService>();
        serviceProvider = Substitute.For<IServiceProvider>();
        _markdownCombinationService = Substitute.For<IMarkdownCombinationService>();
        _markdownFileCollectorService = Substitute.For<IMarkdownFileCollectorService>();
        _markdownRenderingService = Substitute.For<Desktop.Services.IMarkdownRenderingService>();
        _logTransitionService = Substitute.For<Logging.ILogTransitionService>();
        _editorStateService = new EditorStateService(_stateLogger);
        
        // Set up common fileService behavior
        _fileService.IsValidFolder(Arg.Any<string>()).Returns(true);
        _fileService.ReadFileContentAsync(Arg.Any<string>()).Returns("Mock file content");
    }

    protected static async Task WaitForConditionAsync(Func<bool> condition, int timeoutMs = 2000, int intervalMs = 10)
    {
        var maxWait = timeoutMs / intervalMs;
        var waitCount = 0;
        while (!condition() && waitCount < maxWait)
        {
            await Task.Delay(intervalMs);
            waitCount++;
        }
    }

    protected static async Task<(MainWindowViewModel, FileExplorerViewModel)> SetupWindowAndWaitForLoadAsync(MainWindow window)
    {
        window.Show();
        
        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null);
        
        // Get the FileExplorerViewModel from the window's FileExplorer control
        var fileExplorerBorder = window.FindControl<Border>("FileExplorerBorder");
        Assert.That(fileExplorerBorder, Is.Not.Null);
        
        var fileExplorerControl = fileExplorerBorder.Child as FileExplorerUserControl;
        Assert.That(fileExplorerControl, Is.Not.Null);
        
        var fileExplorerViewModel = fileExplorerControl.DataContext as FileExplorerViewModel;
        Assert.That(fileExplorerViewModel, Is.Not.Null);

        // Wait for file structure to load and root to be expanded
        await WaitForConditionAsync(() => fileExplorerViewModel!.RootItem != null, 2000);
        Assert.That(fileExplorerViewModel.RootItem, Is.Not.Null, "Root item should be loaded");
        
        // Wait for root to be auto-expanded with children
        await WaitForConditionAsync(() => 
            fileExplorerViewModel.RootItem!.IsExpanded && 
            fileExplorerViewModel.RootItem.Children.Any(c => c.Name != "Loading..."), 3000);
        
        return (viewModel!, fileExplorerViewModel);
    }

    protected static async Task ExpandFolderAndWaitAsync(FileSystemItemViewModel? folder)
    {
        if (folder == null) throw new ArgumentNullException(nameof(folder));

        folder.IsExpanded = true;
        // Wait for children to be loaded (either already loaded or loading to complete)
        await WaitForConditionAsync(() => 
            folder.Children.Any() && 
            (folder.Children.All(c => c.Name != "Loading...") || folder.Children.Count > 1), 2000);
    }

    protected static async Task SelectFileAndWaitForTabAsync(FileSystemItemViewModel? file, MainWindowViewModel viewModel)
    {
        if (file == null) throw new ArgumentNullException(nameof(file));

        var initialTabCount = viewModel.EditorTabBar.EditorTabs.Count;
        file.IsSelected = true;
        await WaitForConditionAsync(() => viewModel.EditorTabBar.EditorTabs.Count > initialTabCount, 1000);
    }

    protected static FileSystemItem CreateSimpleTestStructure() => new()
    {
        Name = "test-project",
        FullPath = "/test/path",
        IsDirectory = true,
        Children = 
        [
            new() { Name = "src", FullPath = "/test/src", IsDirectory = true },
            new() { Name = "test", FullPath = "/test/test", IsDirectory = true },
            new() { Name = "README.md", FullPath = "/test/README.md", IsDirectory = false }
        ]
    };

    protected static FileSystemItem CreateNestedTestStructure() => new()
    {
        Name = "test-project",
        FullPath = "/test/path",
        IsDirectory = true,
        Children = 
        [
            new() 
            { 
                Name = "src", 
                FullPath = "/test/path/src",
                IsDirectory = true,
                Children = 
                [
                    new() { Name = "components", FullPath = "/test/path/src/components", IsDirectory = true },
                    new() { Name = "utils", FullPath = "/test/path/src/utils", IsDirectory = true },
                    new() { Name = "main.cs", FullPath = "/test/path/src/main.cs", IsDirectory = false }
                ]
            },
            new() 
            { 
                Name = "test", 
                FullPath = "/test/path/test",
                IsDirectory = true,
                Children = 
                [
                    new() { Name = "unit", FullPath = "/test/path/test/unit", IsDirectory = true },
                    new() { Name = "integration", FullPath = "/test/path/test/integration", IsDirectory = true }
                ]
            },
            new() { Name = "README.md", FullPath = "/test/path/README.md", IsDirectory = false }
        ]
    };

    protected EditorViewModel CreateEditorViewModel(
        EditorTabBarViewModel? editorTabBarViewModel = null, 
        EditorContentViewModel? editorContentViewModel = null,
        IEditorStateService? editorStateService = null)
    {
        editorStateService ??= _editorStateService;
        editorTabBarViewModel ??= new EditorTabBarViewModel(_tabBarLogger, _fileService, editorStateService);
        editorContentViewModel ??= new EditorContentViewModel(_contentLogger, editorStateService, _options, serviceProvider, _markdownCombinationService, _markdownFileCollectorService, _markdownRenderingService);
        
        var hotkeyService = Substitute.For<Desktop.Services.IHotkeyService>();
        var editorLogger = Substitute.For<ILogger<Desktop.ViewModels.EditorViewModel>>();
        return new Desktop.ViewModels.EditorViewModel(editorLogger, _options, editorTabBarViewModel, editorContentViewModel, hotkeyService);
    }

    protected MainWindow CreateMainWindow()
    {
        _fileService.GetFileStructureAsync().Returns(Task.FromResult<FileSystemItem?>(CreateSimpleTestStructure()));
        _fileService.GetFileStructureAsync(Arg.Any<string>()).Returns(Task.FromResult<FileSystemItem?>(CreateSimpleTestStructure()));
        
        _editorStateService = new EditorStateService(_stateLogger);
        var editorViewModel = CreateEditorViewModel();
        
        var hotkeyService = Substitute.For<Desktop.Services.IHotkeyService>();
        var fileExplorerViewModel = new FileExplorerViewModel(Substitute.For<ILogger<FileExplorerViewModel>>(), _fileService);
        var viewModel = new MainWindowViewModel(_vmLogger, _options, _editorStateService, editorViewModel, _logTransitionService, hotkeyService);
        return new MainWindow(viewModel, fileExplorerViewModel);
    }

    protected MainWindow CreateMainWindowWithNestedStructure()
    {
        _fileService.GetFileStructureAsync().Returns(Task.FromResult<FileSystemItem?>(CreateNestedTestStructure()));
        _fileService.GetFileStructureAsync(Arg.Any<string>()).Returns(Task.FromResult<FileSystemItem?>(CreateNestedTestStructure()));
        
        var editorStateService = new EditorStateService(_stateLogger);
        var editorViewModel = CreateEditorViewModel(editorStateService: editorStateService);
        
        var hotkeyService = Substitute.For<Desktop.Services.IHotkeyService>();
        var fileExplorerViewModel = new FileExplorerViewModel(Substitute.For<ILogger<FileExplorerViewModel>>(), _fileService);
        var viewModel = new MainWindowViewModel(_vmLogger, _options, editorStateService, editorViewModel, _logTransitionService, hotkeyService);
        return new MainWindow(viewModel, fileExplorerViewModel);
    }
    
    protected FileExplorerUserControl CreateFileExplorerWithNestedStructure()
    {
        _fileService.GetFileStructureAsync().Returns(Task.FromResult<FileSystemItem?>(CreateNestedTestStructure()));
        _fileService.GetFileStructureAsync(Arg.Any<string>()).Returns(Task.FromResult<FileSystemItem?>(CreateNestedTestStructure()));
        
        var fileExplorerViewModel = new FileExplorerViewModel(Substitute.For<ILogger<FileExplorerViewModel>>(), _fileService);
        return new FileExplorerUserControl(fileExplorerViewModel);
    }
    
    protected FileExplorerUserControl CreateFileExplorerWithSimpleStructure()
    {
        _fileService.GetFileStructureAsync().Returns(Task.FromResult<FileSystemItem?>(CreateSimpleTestStructure()));
        _fileService.GetFileStructureAsync(Arg.Any<string>()).Returns(Task.FromResult<FileSystemItem?>(CreateSimpleTestStructure()));
        
        var fileExplorerViewModel = new FileExplorerViewModel(Substitute.For<ILogger<FileExplorerViewModel>>(), _fileService);
        return new FileExplorerUserControl(fileExplorerViewModel);
    }
    
    protected static async Task<FileExplorerViewModel> SetupFileExplorerAndWaitForLoadAsync(FileExplorerUserControl fileExplorer)
    {
        var viewModel = fileExplorer.DataContext as FileExplorerViewModel;
        Assert.That(viewModel, Is.Not.Null);

        await viewModel!.InitializeAsync();
        
        // Wait for file structure to load and root to be expanded
        await WaitForConditionAsync(() => viewModel.RootItem != null, 2000);
        Assert.That(viewModel.RootItem, Is.Not.Null, "Root item should be loaded");
        
        // Wait for root to be auto-expanded with children
        await WaitForConditionAsync(() =>
            viewModel.RootItem!.IsExpanded &&
            viewModel.RootItem.Children.Any(c => c.Name != "Loading...") &&
            viewModel.RootItem.Children.Any(), 3000);
        
        return viewModel;
    }
}
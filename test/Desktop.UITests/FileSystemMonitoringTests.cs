using Avalonia.Headless.NUnit;
using Desktop.Views;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desktop.Configuration;
using Desktop.Services;
using Desktop.Models;
using NSubstitute;
using Business.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Desktop.UITests;

public class FileSystemMonitoringTests
{
    private static FileSystemItem CreateTestStructure() => new()
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
                    new()
                    {
                        Name = "controllers",
                        FullPath = "/test/path/src/controllers",
                        IsDirectory = true,
                        Children =
                        [
                            new() { Name = "HomeController.cs", FullPath = "/test/path/src/controllers/HomeController.cs", IsDirectory = false }
                        ]
                    },
                    new() { Name = "main.cs", FullPath = "/test/path/src/main.cs", IsDirectory = false }
                ]
            },
            new()
            {
                Name = "tests",
                FullPath = "/test/path/tests",
                IsDirectory = true,
                Children =
                [
                    new() { Name = "unit", FullPath = "/test/path/tests/unit", IsDirectory = true, Children = [] },
                    new() { Name = "integration", FullPath = "/test/path/tests/integration", IsDirectory = true, Children = [] }
                ]
            },
            new() { Name = "README.md", FullPath = "/test/path/README.md", IsDirectory = false }
        ]
    };

    private static async Task WaitForFileStructureLoadAsync(FileExplorerViewModel fileExplorerViewModel)
    {
        await Task.Delay(500);

        var maxWait = 50;
        var waitCount = 0;
        while (fileExplorerViewModel.RootItem == null && waitCount < maxWait)
        {
            await Task.Delay(100);
            waitCount++;
        }
    }

    private static (MainWindow window, IFileService fileService, IFileSystemMonitorService fileSystemMonitorService, MainWindowViewModel viewModel, FileExplorerViewModel fileExplorerViewModel) CreateMainWindowWithMonitoring()
    {
        var vmLogger = NullLoggerFactory.Instance.CreateLogger<MainWindowViewModel>();
        var tabBarLogger = NullLoggerFactory.Instance.CreateLogger<EditorTabBarViewModel>();
        var contentLogger = NullLoggerFactory.Instance.CreateLogger<EditorContentViewModel>();
        var stateLogger = NullLoggerFactory.Instance.CreateLogger<EditorStateService>();
        var options = Options.Create(new ApplicationOptions
        {
            DefaultProjectFolder = "/test/path"
        });
        var fileService = Substitute.For<IFileService>();
        var fileSystemMonitorService = Substitute.For<IFileSystemMonitorService>();
        var serviceProvider = Substitute.For<IServiceProvider>();

        fileService.GetFileStructureAsync().Returns(Task.FromResult<FileSystemItem?>(CreateTestStructure()));
        fileService.GetFileStructureAsync(Arg.Any<string>()).Returns(Task.FromResult<FileSystemItem?>(CreateTestStructure()));
        fileService.IsValidFolder(Arg.Any<string>()).Returns(true);
        fileService.ReadFileContentAsync(Arg.Any<string>()).Returns("Mock file content");
        fileSystemMonitorService.IsMonitoring.Returns(false);
        fileService.CreateFileSystemItem(Arg.Any<string>(), Arg.Any<bool>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            var isDirectory = callInfo.Arg<bool>();
            var fileName = System.IO.Path.GetFileName(path);
            return new FileSystemItem
            {
                Name = fileName,
                FullPath = path,
                IsDirectory = isDirectory,
                LastModified = DateTime.Now,
                Size = isDirectory ? 0 : 100
            };
        });

        var markdownCombinationService = Substitute.For<IMarkdownCombinationService>();
        var markdownFileCollectorService = Substitute.For<IMarkdownFileCollectorService>();
        var markdownRenderingService = Substitute.For<Desktop.Services.IMarkdownRenderingService>();

        var editorStateService = new EditorStateService(stateLogger);
        var editorTabBarViewModel = new EditorTabBarViewModel(tabBarLogger, fileService, editorStateService);
        var editorContentViewModel = new EditorContentViewModel(contentLogger, editorStateService, options, serviceProvider, markdownCombinationService, markdownFileCollectorService, markdownRenderingService, Substitute.For<Desktop.Factories.ISettingsContentViewModelFactory>());

        var logTransitionService = Substitute.For<Desktop.Logging.ILogTransitionService>();
        var hotkeyService = Substitute.For<Desktop.Services.IHotkeyService>();
        var editorLogger = NullLoggerFactory.Instance.CreateLogger<Desktop.ViewModels.EditorViewModel>();
        var editorViewModel = new Desktop.ViewModels.EditorViewModel(editorLogger, options, editorTabBarViewModel, editorContentViewModel, hotkeyService);
        var fileSystemExplorerService = Substitute.For<Desktop.Services.IFileSystemExplorerService>();
        var fileSystemChangeHandler = new Desktop.Services.FileSystemChangeHandler(NullLoggerFactory.Instance.CreateLogger<Desktop.Services.FileSystemChangeHandler>(), fileService);
        var fileExplorerViewModel = new FileExplorerViewModel(
            NullLoggerFactory.Instance.CreateLogger<FileExplorerViewModel>(),
            NullLoggerFactory.Instance,
            fileSystemExplorerService,
            fileSystemChangeHandler,
            fileService,
            fileSystemMonitorService,
            options);
        var viewModel = new MainWindowViewModel(vmLogger, options, editorStateService, editorViewModel, logTransitionService, hotkeyService);
        var window = new MainWindow(viewModel, fileExplorerViewModel);

        return (window, fileService, fileSystemMonitorService, viewModel, fileExplorerViewModel);
    }

    [AvaloniaTest]
    public async Task FileSystemMonitor_Should_Start_Monitoring_When_File_Structure_Loads()
    {
        var (window, fileService, fileSystemMonitorService, viewModel, fileExplorerViewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        await WaitForFileStructureLoadAsync(fileExplorerViewModel);

        // Verify that StartMonitoring was called
        fileSystemMonitorService.Received(1).StartMonitoring(Arg.Any<string>());
    }

    [AvaloniaTest]
    public async Task FileExplorerViewModel_Should_Handle_File_Created_Event()
    {
        var (window, fileService, fileSystemMonitorService, viewModel, fileExplorerViewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        await WaitForFileStructureLoadAsync(fileExplorerViewModel);

        Assert.That(fileExplorerViewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Get the src folder and expand it
        var srcFolder = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        Assert.That(srcFolder, Is.Not.Null, "src folder should exist");

        srcFolder!.IsExpanded = true;
        await Task.Delay(1000);

        // Initial state: src folder should have 2 children (controllers, main.cs)
        Assert.That(srcFolder.Children, Has.Count.EqualTo(2), "src should initially have 2 children");
        var mainCs = srcFolder.Children.FirstOrDefault(c => c.Name == "main.cs");
        Assert.That(mainCs, Is.Not.Null, "src should contain main.cs");

        // Simulate file created event
        var eventArgs = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Created,
            Path = "/test/path/src/newfile.cs",
            IsDirectory = false
        };

        fileSystemMonitorService.FileSystemChanged += Raise.EventWith(fileSystemMonitorService, eventArgs);

        // Wait for UI to update
        await Task.Delay(500);

        // Verify new file was added
        Assert.Multiple(() =>
        {
            Assert.That(srcFolder.Children, Has.Count.EqualTo(3), "src should now have 3 children");
            var newFile = srcFolder.Children.FirstOrDefault(c => c.Name == "newfile.cs");
            Assert.That(newFile, Is.Not.Null, "newfile.cs should be added");
            Assert.That(newFile!.IsDirectory, Is.False, "newfile.cs should be a file");
            Assert.That(newFile.FullPath, Is.EqualTo("/test/path/src/newfile.cs"), "newfile.cs should have correct path");
        });
    }

    [AvaloniaTest]
    public async Task FileExplorerViewModel_Should_Handle_Directory_Created_Event()
    {
        var (window, fileService, fileSystemMonitorService, viewModel, fileExplorerViewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        await WaitForFileStructureLoadAsync(fileExplorerViewModel);

        Assert.That(fileExplorerViewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Initial state: root should have 3 children (src, tests, README.md)
        Assert.That(fileExplorerViewModel.RootItem!.Children, Has.Count.EqualTo(3), "root should initially have 3 children");

        // Simulate directory created event
        var eventArgs = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Created,
            Path = "/test/path/docs",
            IsDirectory = true
        };

        fileSystemMonitorService.FileSystemChanged += Raise.EventWith(fileSystemMonitorService, eventArgs);

        // Wait for UI to update
        await Task.Delay(500);

        // Verify new directory was added
        Assert.Multiple(() =>
        {
            Assert.That(fileExplorerViewModel.RootItem.Children, Has.Count.EqualTo(4), "root should now have 4 children");
            var newDir = fileExplorerViewModel.RootItem.Children.FirstOrDefault(c => c.Name == "docs");
            Assert.That(newDir, Is.Not.Null, "docs directory should be added");
            Assert.That(newDir!.IsDirectory, Is.True, "docs should be a directory");
            Assert.That(newDir.FullPath, Is.EqualTo("/test/path/docs"), "docs should have correct path");

            // Verify sorting: directories should come first
            var firstChild = fileExplorerViewModel.RootItem.Children.First();
            Assert.That(firstChild.Name, Is.EqualTo("docs"), "docs should be sorted first (directories first, alphabetical)");
        });
    }

    [AvaloniaTest]
    public async Task FileExplorerViewModel_Should_Handle_File_Deleted_Event()
    {
        var (window, fileService, fileSystemMonitorService, viewModel, fileExplorerViewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        await WaitForFileStructureLoadAsync(fileExplorerViewModel);

        Assert.That(fileExplorerViewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Initial state: root should have README.md
        var readmeFile = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        Assert.Multiple(() =>
        {
            Assert.That(readmeFile, Is.Not.Null, "README.md should exist initially");
            Assert.That(fileExplorerViewModel.RootItem.Children, Has.Count.EqualTo(3), "root should initially have 3 children");
        });

        // Simulate file deleted event
        var eventArgs = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Deleted,
            Path = "/test/path/README.md",
            IsDirectory = false
        };

        fileSystemMonitorService.FileSystemChanged += Raise.EventWith(fileSystemMonitorService, eventArgs);

        // Wait for UI to update
        await Task.Delay(500);

        // Verify file was removed
        Assert.Multiple(() =>
        {
            Assert.That(fileExplorerViewModel.RootItem.Children, Has.Count.EqualTo(2), "root should now have 2 children");
            var remainingReadme = fileExplorerViewModel.RootItem.Children.FirstOrDefault(c => c.Name == "README.md");
            Assert.That(remainingReadme, Is.Null, "README.md should be removed");

            // Verify src folder still exists
            var srcFolder = fileExplorerViewModel.RootItem.Children.FirstOrDefault(c => c.Name == "src");
            Assert.That(srcFolder, Is.Not.Null, "src folder should still exist");
        });
    }

    [AvaloniaTest]
    public async Task FileExplorerViewModel_Should_Handle_File_Renamed_Event()
    {
        var (window, fileService, fileSystemMonitorService, viewModel, fileExplorerViewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        await WaitForFileStructureLoadAsync(fileExplorerViewModel);

        Assert.That(fileExplorerViewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Initial state: root should have README.md
        var readmeFile = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        Assert.Multiple(() =>
        {
            Assert.That(readmeFile, Is.Not.Null, "README.md should exist initially");
            Assert.That(fileExplorerViewModel.RootItem.Children, Has.Count.EqualTo(3), "root should initially have 3 children");
        });

        // Simulate file renamed event
        var eventArgs = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Renamed,
            Path = "/test/path/CHANGELOG.md",
            OldPath = "/test/path/README.md",
            IsDirectory = false
        };

        fileSystemMonitorService.FileSystemChanged += Raise.EventWith(fileSystemMonitorService, eventArgs);

        // Wait for UI to update
        await Task.Delay(500);

        // Verify file was renamed (old removed, new added)
        Assert.Multiple(() =>
        {
            Assert.That(fileExplorerViewModel.RootItem.Children, Has.Count.EqualTo(3), "root should still have 3 children");

            var oldFile = fileExplorerViewModel.RootItem.Children.FirstOrDefault(c => c.Name == "README.md");
            Assert.That(oldFile, Is.Null, "README.md should be removed");

            var newFile = fileExplorerViewModel.RootItem.Children.FirstOrDefault(c => c.Name == "CHANGELOG.md");
            Assert.That(newFile, Is.Not.Null, "CHANGELOG.md should be added");
            Assert.That(newFile!.IsDirectory, Is.False, "CHANGELOG.md should be a file");
            Assert.That(newFile.FullPath, Is.EqualTo("/test/path/CHANGELOG.md"), "CHANGELOG.md should have correct path");
        });
    }

    [AvaloniaTest]
    public async Task FileExplorerViewModel_Should_Only_Update_Expanded_Folders()
    {
        var (window, fileService, fileSystemMonitorService, viewModel, fileExplorerViewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        await WaitForFileStructureLoadAsync(fileExplorerViewModel);

        Assert.That(fileExplorerViewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Get src folder but DON'T expand it
        var srcFolder = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        Assert.That(srcFolder, Is.Not.Null, "src folder should exist");
        Assert.That(srcFolder!.IsExpanded, Is.False, "src folder should not be expanded initially");

        // Simulate file created in unexpanded folder
        var eventArgs = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Created,
            Path = "/test/path/src/newfile.cs",
            IsDirectory = false
        };

        fileSystemMonitorService.FileSystemChanged += Raise.EventWith(fileSystemMonitorService, eventArgs);

        // Wait for UI to update
        await Task.Delay(500);

        // With the new loading behavior, visible folders load their children immediately
        // even when not expanded, so children will be loaded but folder stays collapsed
        Assert.Multiple(() =>
        {
            Assert.That(srcFolder.IsExpanded, Is.False, "src folder should still not be expanded");
            Assert.That(srcFolder.Children, Is.Not.Empty, "src should have children loaded when visible");
            Assert.That(srcFolder.HasChildren, Is.True, "src should indicate it has children");
        });

        // Now expand the src folder
        srcFolder.IsExpanded = true;
        await Task.Delay(1000);

        // Now it should load the actual children (including the original main.cs and controllers)
        // And the newfile.cs should be there since the file system change was tracked in the underlying model
        Assert.Multiple(() =>
        {
            Assert.That(srcFolder.IsExpanded, Is.True, "src folder should be expanded");
            Assert.That(srcFolder.Children, Has.Count.EqualTo(3), "src should have actual children loaded including the new file");
            var childrenNames = srcFolder.Children.Select(c => c.Name).OrderBy(n => n).ToArray();
            Assert.That(childrenNames, Is.EqualTo(new[] { "controllers", "main.cs", "newfile.cs" }), "src should have controllers, main.cs and newfile.cs");
        });
    }

    [AvaloniaTest]
    public async Task FileExplorerViewModel_Should_Sort_New_Items_Correctly()
    {
        var (window, fileService, fileSystemMonitorService, viewModel, fileExplorerViewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        await WaitForFileStructureLoadAsync(fileExplorerViewModel);

        Assert.That(fileExplorerViewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        Assert.Multiple(() =>
        {
            // Initial state: root has [src (dir), tests (dir), README.md (file)]
            Assert.That(fileExplorerViewModel.RootItem!.Children, Has.Count.EqualTo(3), "root should initially have 3 children");
            Assert.That(fileExplorerViewModel.RootItem.Children[0].Name, Is.EqualTo("src"), "src should be first (directory, alphabetical)");
            Assert.That(fileExplorerViewModel.RootItem.Children[1].Name, Is.EqualTo("tests"), "tests should be second (directory, alphabetical)");
            Assert.That(fileExplorerViewModel.RootItem.Children[2].Name, Is.EqualTo("README.md"), "README.md should be third (file)");
        });

        // Add a new directory that should be sorted first alphabetically among directories
        var newDirEvent = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Created,
            Path = "/test/path/docs",
            IsDirectory = true
        };

        fileSystemMonitorService.FileSystemChanged += Raise.EventWith(fileSystemMonitorService, newDirEvent);
        await Task.Delay(500);

        // Add a new file that should be sorted among files
        var newFileEvent = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Created,
            Path = "/test/path/CHANGELOG.md",
            IsDirectory = false
        };

        fileSystemMonitorService.FileSystemChanged += Raise.EventWith(fileSystemMonitorService, newFileEvent);
        await Task.Delay(500);

        // Verify sorting: directories first (alphabetical), then files (alphabetical)
        Assert.Multiple(() =>
        {
            Assert.That(fileExplorerViewModel.RootItem.Children, Has.Count.EqualTo(5), "root should have 5 children");

            // Directories first, alphabetically
            Assert.That(fileExplorerViewModel.RootItem.Children[0].Name, Is.EqualTo("docs"), "docs should be first (dir, alphabetical)");
            Assert.That(fileExplorerViewModel.RootItem.Children[0].IsDirectory, Is.True, "first item should be directory");

            Assert.That(fileExplorerViewModel.RootItem.Children[1].Name, Is.EqualTo("src"), "src should be second (dir, alphabetical)");
            Assert.That(fileExplorerViewModel.RootItem.Children[1].IsDirectory, Is.True, "second item should be directory");

            Assert.That(fileExplorerViewModel.RootItem.Children[2].Name, Is.EqualTo("tests"), "tests should be third (dir, alphabetical)");
            Assert.That(fileExplorerViewModel.RootItem.Children[2].IsDirectory, Is.True, "third item should be directory");

            // Files second, alphabetically
            Assert.That(fileExplorerViewModel.RootItem.Children[3].Name, Is.EqualTo("CHANGELOG.md"), "CHANGELOG.md should be fourth (file, alphabetical)");
            Assert.That(fileExplorerViewModel.RootItem.Children[3].IsDirectory, Is.False, "fourth item should be file");

            Assert.That(fileExplorerViewModel.RootItem.Children[4].Name, Is.EqualTo("README.md"), "README.md should be fifth (file, alphabetical)");
            Assert.That(fileExplorerViewModel.RootItem.Children[4].IsDirectory, Is.False, "fifth item should be file");
        });
    }

    [AvaloniaTest]
    public async Task FileExplorerViewModel_Should_Ignore_Changes_Outside_Monitored_Path()
    {
        var (window, fileService, fileSystemMonitorService, viewModel, fileExplorerViewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        await WaitForFileStructureLoadAsync(fileExplorerViewModel);

        Assert.That(fileExplorerViewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Initial state: root should have 3 children
        var initialCount = fileExplorerViewModel.RootItem!.Children.Count;
        Assert.That(initialCount, Is.EqualTo(3), "root should initially have 3 children");

        // Simulate file created outside of the monitored path
        var eventArgs = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Created,
            Path = "/different/path/newfile.cs",  // Outside the /test/path root
            IsDirectory = false
        };

        fileSystemMonitorService.FileSystemChanged += Raise.EventWith(fileSystemMonitorService, eventArgs);

        // Wait for potential UI update
        await Task.Delay(500);

        // Verify no changes occurred
        Assert.That(fileExplorerViewModel.RootItem.Children, Has.Count.EqualTo(initialCount),
            "root should still have same number of children (change was outside monitored path)");
    }

    [AvaloniaTest]
    public async Task FileExplorerViewModel_Should_Preload_Next_Level_When_Expanded()
    {
        var (window, fileService, fileSystemMonitorService, viewModel, fileExplorerViewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        await WaitForFileStructureLoadAsync(fileExplorerViewModel);

        Assert.That(fileExplorerViewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Get the src folder and expand it
        var srcFolder = fileExplorerViewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        Assert.That(srcFolder, Is.Not.Null, "src folder should exist");

        // Expand the src folder to trigger loading and preloading
        srcFolder!.IsExpanded = true;
        await Task.Delay(1500); // Give extra time for preloading to complete

        // Verify src folder has loaded its direct children
        Assert.Multiple(() =>
        {
            Assert.That(srcFolder.Children, Has.Count.EqualTo(2), "src should have 2 direct children");

            var controllersFolder = srcFolder.Children.FirstOrDefault(c => c.Name == "controllers");
            Assert.That(controllersFolder, Is.Not.Null, "controllers folder should exist");
            Assert.That(controllersFolder!.IsDirectory, Is.True, "controllers should be a directory");

            var mainFile = srcFolder.Children.FirstOrDefault(c => c.Name == "main.cs");
            Assert.That(mainFile, Is.Not.Null, "main.cs file should exist");
            Assert.That(mainFile!.IsDirectory, Is.False, "main.cs should be a file");
        });

        // Verify that controllers folder has been preloaded (next level)
        var controllersFolder = srcFolder.Children.FirstOrDefault(c => c.Name == "controllers");
        Assert.That(controllersFolder, Is.Not.Null, "controllers folder should exist");

        // The controllers folder should have its children preloaded but not be expanded
        Assert.Multiple(() =>
        {
            Assert.That(controllersFolder!.IsExpanded, Is.False, "controllers folder should not be expanded yet");
            Assert.That(controllersFolder.Children, Has.Count.EqualTo(1), "controllers folder should have preloaded its children");

            var homeController = controllersFolder.Children.FirstOrDefault(c => c.Name == "HomeController.cs");
            Assert.That(homeController, Is.Not.Null, "HomeController.cs should be preloaded");
            Assert.That(homeController!.IsDirectory, Is.False, "HomeController.cs should be a file");
        });

        // Also verify tests folder preloading
        var testsFolder = fileExplorerViewModel.RootItem.Children.FirstOrDefault(c => c.Name == "tests");
        Assert.That(testsFolder, Is.Not.Null, "tests folder should exist");

        // Expand tests folder to verify preloading works there too
        testsFolder!.IsExpanded = true;
        await Task.Delay(1500); // Give time for preloading

        Assert.Multiple(() =>
        {
            Assert.That(testsFolder.Children, Has.Count.EqualTo(2), "tests should have 2 direct children");

            var unitFolder = testsFolder.Children.FirstOrDefault(c => c.Name == "unit");
            var integrationFolder = testsFolder.Children.FirstOrDefault(c => c.Name == "integration");

            Assert.That(unitFolder, Is.Not.Null, "unit folder should exist");
            Assert.That(integrationFolder, Is.Not.Null, "integration folder should exist");

            // Both should be preloaded (even though empty)
            Assert.That(unitFolder!.IsExpanded, Is.False, "unit folder should not be expanded yet");
            Assert.That(integrationFolder!.IsExpanded, Is.False, "integration folder should not be expanded yet");
            Assert.That(unitFolder.Children, Is.Empty, "unit folder should be preloaded (empty)");
            Assert.That(integrationFolder.Children, Is.Empty, "integration folder should be preloaded (empty)");
        });
    }
}
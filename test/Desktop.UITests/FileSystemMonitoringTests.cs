using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Desktop.Views;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desktop.Configuration;
using Desktop.Services;
using Desktop.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using System;
using Microsoft.Extensions.DependencyInjection;
using Business.Services;

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
                    new() { Name = "main.cs", FullPath = "/test/path/src/main.cs", IsDirectory = false }
                ]
            },
            new() { Name = "README.md", FullPath = "/test/path/README.md", IsDirectory = false }
        ]
    };

    private static (MainWindow window, IFileService fileService, MainWindowViewModel viewModel) CreateMainWindowWithMonitoring()
    {
        var vmLogger = new LoggerFactory().CreateLogger<MainWindowViewModel>();
        var tabBarLogger = new LoggerFactory().CreateLogger<EditorTabBarViewModel>();
        var contentLogger = new LoggerFactory().CreateLogger<EditorContentViewModel>();
        var stateLogger = new LoggerFactory().CreateLogger<EditorStateService>();
        var options = Options.Create(new ApplicationOptions());
        var fileService = Substitute.For<IFileService>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        
        fileService.GetFileStructureAsync().Returns(Task.FromResult<FileSystemItem?>(CreateTestStructure()));
        fileService.GetFileStructureAsync(Arg.Any<string>()).Returns(Task.FromResult<FileSystemItem?>(CreateTestStructure()));
        fileService.IsValidFolder(Arg.Any<string>()).Returns(true);
        fileService.ReadFileContentAsync(Arg.Any<string>()).Returns("Mock file content");
        fileService.IsMonitoringFileSystem.Returns(false);
        
        var markdownCombinationService = Substitute.For<IMarkdownCombinationService>();
        var markdownFileCollectorService = Substitute.For<IMarkdownFileCollectorService>();
        
        var editorStateService = new EditorStateService(stateLogger);
        var editorTabBarViewModel = new EditorTabBarViewModel(tabBarLogger, fileService, editorStateService);
        var editorContentViewModel = new EditorContentViewModel(contentLogger, editorStateService, options, serviceProvider, markdownCombinationService, markdownFileCollectorService);
        
        var viewModel = new MainWindowViewModel(vmLogger, options, fileService, serviceProvider, editorStateService, editorTabBarViewModel, editorContentViewModel);
        var window = new MainWindow(viewModel);
        
        return (window, fileService, viewModel);
    }

    [AvaloniaTest]
    public async Task FileService_Should_Start_Monitoring_When_File_Structure_Loads()
    {
        var (window, fileService, viewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        // Wait for file structure to load
        await Task.Delay(500);
        
        var maxWait = 50;
        var waitCount = 0;
        while (viewModel.RootItem == null && waitCount < maxWait)
        {
            await Task.Delay(100);
            waitCount++;
        }

        // Verify that StartFileSystemMonitoring was called
        fileService.Received(1).StartFileSystemMonitoring();
    }

    [AvaloniaTest]
    public async Task FileSystemItemViewModel_Should_Handle_File_Created_Event()
    {
        var (window, fileService, viewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        // Wait for file structure to load
        await Task.Delay(500);
        
        var maxWait = 50;
        var waitCount = 0;
        while (viewModel.RootItem == null && waitCount < maxWait)
        {
            await Task.Delay(100);
            waitCount++;
        }

        Assert.That(viewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Get the src folder and expand it
        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        Assert.That(srcFolder, Is.Not.Null, "src folder should exist");
        
        srcFolder!.IsExpanded = true;
        await Task.Delay(1000);

        // Initial state: src folder should have 1 child (main.cs)
        Assert.That(srcFolder.Children.Count, Is.EqualTo(1), "src should initially have 1 child");
        Assert.That(srcFolder.Children[0].Name, Is.EqualTo("main.cs"), "src should contain main.cs");

        // Simulate file created event
        var eventArgs = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Created,
            Path = "/test/path/src/newfile.cs",
            IsDirectory = false
        };

        fileService.FileSystemChanged += Raise.EventWith(fileService, eventArgs);

        // Wait for UI to update
        await Task.Delay(500);

        // Verify new file was added
        Assert.Multiple(() =>
        {
            Assert.That(srcFolder.Children.Count, Is.EqualTo(2), "src should now have 2 children");
            var newFile = srcFolder.Children.FirstOrDefault(c => c.Name == "newfile.cs");
            Assert.That(newFile, Is.Not.Null, "newfile.cs should be added");
            Assert.That(newFile!.IsDirectory, Is.False, "newfile.cs should be a file");
            Assert.That(newFile.FullPath, Is.EqualTo("/test/path/src/newfile.cs"), "newfile.cs should have correct path");
        });
    }

    [AvaloniaTest]
    public async Task FileSystemItemViewModel_Should_Handle_Directory_Created_Event()
    {
        var (window, fileService, viewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        // Wait for file structure to load
        await Task.Delay(500);
        
        var maxWait = 50;
        var waitCount = 0;
        while (viewModel.RootItem == null && waitCount < maxWait)
        {
            await Task.Delay(100);
            waitCount++;
        }

        Assert.That(viewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Initial state: root should have 2 children (src, README.md)
        Assert.That(viewModel.RootItem!.Children.Count, Is.EqualTo(2), "root should initially have 2 children");

        // Simulate directory created event
        var eventArgs = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Created,
            Path = "/test/path/docs",
            IsDirectory = true
        };

        fileService.FileSystemChanged += Raise.EventWith(fileService, eventArgs);

        // Wait for UI to update
        await Task.Delay(500);

        // Verify new directory was added
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.RootItem.Children.Count, Is.EqualTo(3), "root should now have 3 children");
            var newDir = viewModel.RootItem.Children.FirstOrDefault(c => c.Name == "docs");
            Assert.That(newDir, Is.Not.Null, "docs directory should be added");
            Assert.That(newDir!.IsDirectory, Is.True, "docs should be a directory");
            Assert.That(newDir.FullPath, Is.EqualTo("/test/path/docs"), "docs should have correct path");
            
            // Verify sorting: directories should come first
            var firstChild = viewModel.RootItem.Children.First();
            Assert.That(firstChild.Name, Is.EqualTo("docs"), "docs should be sorted first (directories first)");
        });
    }

    [AvaloniaTest]
    public async Task FileSystemItemViewModel_Should_Handle_File_Deleted_Event()
    {
        var (window, fileService, viewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        // Wait for file structure to load
        await Task.Delay(500);
        
        var maxWait = 50;
        var waitCount = 0;
        while (viewModel.RootItem == null && waitCount < maxWait)
        {
            await Task.Delay(100);
            waitCount++;
        }

        Assert.That(viewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Initial state: root should have README.md
        var readmeFile = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        Assert.That(readmeFile, Is.Not.Null, "README.md should exist initially");
        Assert.That(viewModel.RootItem.Children.Count, Is.EqualTo(2), "root should initially have 2 children");

        // Simulate file deleted event
        var eventArgs = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Deleted,
            Path = "/test/path/README.md",
            IsDirectory = false
        };

        fileService.FileSystemChanged += Raise.EventWith(fileService, eventArgs);

        // Wait for UI to update
        await Task.Delay(500);

        // Verify file was removed
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.RootItem.Children.Count, Is.EqualTo(1), "root should now have 1 child");
            var remainingReadme = viewModel.RootItem.Children.FirstOrDefault(c => c.Name == "README.md");
            Assert.That(remainingReadme, Is.Null, "README.md should be removed");
            
            // Verify src folder still exists
            var srcFolder = viewModel.RootItem.Children.FirstOrDefault(c => c.Name == "src");
            Assert.That(srcFolder, Is.Not.Null, "src folder should still exist");
        });
    }

    [AvaloniaTest]
    public async Task FileSystemItemViewModel_Should_Handle_File_Renamed_Event()
    {
        var (window, fileService, viewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        // Wait for file structure to load
        await Task.Delay(500);
        
        var maxWait = 50;
        var waitCount = 0;
        while (viewModel.RootItem == null && waitCount < maxWait)
        {
            await Task.Delay(100);
            waitCount++;
        }

        Assert.That(viewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Initial state: root should have README.md
        var readmeFile = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "README.md");
        Assert.That(readmeFile, Is.Not.Null, "README.md should exist initially");
        Assert.That(viewModel.RootItem.Children.Count, Is.EqualTo(2), "root should initially have 2 children");

        // Simulate file renamed event
        var eventArgs = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Renamed,
            Path = "/test/path/CHANGELOG.md",
            OldPath = "/test/path/README.md",
            IsDirectory = false
        };

        fileService.FileSystemChanged += Raise.EventWith(fileService, eventArgs);

        // Wait for UI to update
        await Task.Delay(500);

        // Verify file was renamed (old removed, new added)
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.RootItem.Children.Count, Is.EqualTo(2), "root should still have 2 children");
            
            var oldFile = viewModel.RootItem.Children.FirstOrDefault(c => c.Name == "README.md");
            Assert.That(oldFile, Is.Null, "README.md should be removed");
            
            var newFile = viewModel.RootItem.Children.FirstOrDefault(c => c.Name == "CHANGELOG.md");
            Assert.That(newFile, Is.Not.Null, "CHANGELOG.md should be added");
            Assert.That(newFile!.IsDirectory, Is.False, "CHANGELOG.md should be a file");
            Assert.That(newFile.FullPath, Is.EqualTo("/test/path/CHANGELOG.md"), "CHANGELOG.md should have correct path");
        });
    }

    [AvaloniaTest]
    public async Task FileSystemItemViewModel_Should_Only_Update_Expanded_Folders()
    {
        var (window, fileService, viewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        // Wait for file structure to load
        await Task.Delay(500);
        
        var maxWait = 50;
        var waitCount = 0;
        while (viewModel.RootItem == null && waitCount < maxWait)
        {
            await Task.Delay(100);
            waitCount++;
        }

        Assert.That(viewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Get src folder but DON'T expand it
        var srcFolder = viewModel.RootItem!.Children.FirstOrDefault(c => c.Name == "src");
        Assert.That(srcFolder, Is.Not.Null, "src folder should exist");
        Assert.That(srcFolder!.IsExpanded, Is.False, "src folder should not be expanded initially");

        // Simulate file created in unexpanded folder
        var eventArgs = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Created,
            Path = "/test/path/src/newfile.cs",
            IsDirectory = false
        };

        fileService.FileSystemChanged += Raise.EventWith(fileService, eventArgs);

        // Wait for UI to update
        await Task.Delay(500);

        // Since src folder is not expanded, its children should not be updated
        // It should still have the placeholder "Loading..." child
        Assert.Multiple(() =>
        {
            Assert.That(srcFolder.IsExpanded, Is.False, "src folder should still not be expanded");
            Assert.That(srcFolder.Children.Count, Is.EqualTo(1), "src should still have placeholder child");
            Assert.That(srcFolder.Children[0].Name, Is.EqualTo("Loading..."), "src should still have loading placeholder");
        });

        // Now expand the src folder
        srcFolder.IsExpanded = true;
        await Task.Delay(1000);

        // Now it should load the actual children (including the original main.cs)
        // But the newfile.cs won't be there since it wasn't added to the original model
        Assert.Multiple(() =>
        {
            Assert.That(srcFolder.IsExpanded, Is.True, "src folder should be expanded");
            Assert.That(srcFolder.Children.Count, Is.EqualTo(1), "src should have actual children loaded");
            Assert.That(srcFolder.Children[0].Name, Is.EqualTo("main.cs"), "src should have main.cs");
        });
    }

    [AvaloniaTest]
    public async Task FileSystemItemViewModel_Should_Sort_New_Items_Correctly()
    {
        var (window, fileService, viewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        // Wait for file structure to load
        await Task.Delay(500);
        
        var maxWait = 50;
        var waitCount = 0;
        while (viewModel.RootItem == null && waitCount < maxWait)
        {
            await Task.Delay(100);
            waitCount++;
        }

        Assert.That(viewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Initial state: root has [src (dir), README.md (file)]
        Assert.That(viewModel.RootItem!.Children.Count, Is.EqualTo(2), "root should initially have 2 children");
        Assert.That(viewModel.RootItem.Children[0].Name, Is.EqualTo("src"), "src should be first (directory)");
        Assert.That(viewModel.RootItem.Children[1].Name, Is.EqualTo("README.md"), "README.md should be second (file)");

        // Add a new directory that should be sorted first alphabetically among directories
        var newDirEvent = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Created,
            Path = "/test/path/docs",
            IsDirectory = true
        };

        fileService.FileSystemChanged += Raise.EventWith(fileService, newDirEvent);
        await Task.Delay(500);

        // Add a new file that should be sorted among files
        var newFileEvent = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Created,
            Path = "/test/path/CHANGELOG.md",
            IsDirectory = false
        };

        fileService.FileSystemChanged += Raise.EventWith(fileService, newFileEvent);
        await Task.Delay(500);

        // Verify sorting: directories first (alphabetical), then files (alphabetical)
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.RootItem.Children.Count, Is.EqualTo(4), "root should have 4 children");
            
            // Directories first, alphabetically
            Assert.That(viewModel.RootItem.Children[0].Name, Is.EqualTo("docs"), "docs should be first (dir, alphabetical)");
            Assert.That(viewModel.RootItem.Children[0].IsDirectory, Is.True, "first item should be directory");
            
            Assert.That(viewModel.RootItem.Children[1].Name, Is.EqualTo("src"), "src should be second (dir, alphabetical)");
            Assert.That(viewModel.RootItem.Children[1].IsDirectory, Is.True, "second item should be directory");
            
            // Files second, alphabetically
            Assert.That(viewModel.RootItem.Children[2].Name, Is.EqualTo("CHANGELOG.md"), "CHANGELOG.md should be third (file, alphabetical)");
            Assert.That(viewModel.RootItem.Children[2].IsDirectory, Is.False, "third item should be file");
            
            Assert.That(viewModel.RootItem.Children[3].Name, Is.EqualTo("README.md"), "README.md should be fourth (file, alphabetical)");
            Assert.That(viewModel.RootItem.Children[3].IsDirectory, Is.False, "fourth item should be file");
        });
    }

    [AvaloniaTest]
    public async Task FileSystemItemViewModel_Should_Ignore_Changes_Outside_Monitored_Path()
    {
        var (window, fileService, viewModel) = CreateMainWindowWithMonitoring();
        window.Show();

        // Wait for file structure to load
        await Task.Delay(500);
        
        var maxWait = 50;
        var waitCount = 0;
        while (viewModel.RootItem == null && waitCount < maxWait)
        {
            await Task.Delay(100);
            waitCount++;
        }

        Assert.That(viewModel.RootItem, Is.Not.Null, "Root item should be loaded");

        // Initial state: root should have 2 children
        var initialCount = viewModel.RootItem!.Children.Count;
        Assert.That(initialCount, Is.EqualTo(2), "root should initially have 2 children");

        // Simulate file created outside of the monitored path
        var eventArgs = new FileSystemChangedEventArgs
        {
            ChangeType = FileSystemChangeType.Created,
            Path = "/different/path/newfile.cs",  // Outside the /test/path root
            IsDirectory = false
        };

        fileService.FileSystemChanged += Raise.EventWith(fileService, eventArgs);

        // Wait for potential UI update
        await Task.Delay(500);

        // Verify no changes occurred
        Assert.That(viewModel.RootItem.Children.Count, Is.EqualTo(initialCount), 
            "root should still have same number of children (change was outside monitored path)");
    }
}
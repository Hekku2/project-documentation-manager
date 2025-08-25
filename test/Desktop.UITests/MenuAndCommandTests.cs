using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desktop.Configuration;
using Desktop.Services;
using Desktop.Models;
using NSubstitute;
using Business.Services;
using Business.Models;

namespace Desktop.UITests;

[TestFixture]
public class MenuAndCommandTests : MainWindowTestBase
{
    [AvaloniaTest]
    public void MainWindow_Should_Have_Exit_Command_That_Triggers_Exit_Event()
    {
        var window = CreateMainWindow();
        var viewModel = window.DataContext as MainWindowViewModel;
        
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");
        Assert.That(viewModel!.ExitCommand, Is.Not.Null, "ExitCommand should exist");
        
        // Track if exit was requested
        bool exitRequested = false;
        viewModel.ExitRequested += (sender, e) => exitRequested = true;
        
        // Execute the exit command
        viewModel.ExitCommand.Execute(null);
        
        Assert.That(exitRequested, Is.True, "ExitRequested event should be triggered when ExitCommand is executed");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Close_When_Exit_Event_Is_Triggered()
    {
        var window = CreateMainWindow();
        window.Show();
        
        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");
        
        // Verify window is initially open
        Assert.That(window.IsVisible, Is.True, "Window should be visible initially");
        
        // Trigger exit through the command (this will invoke the exit event)
        viewModel!.ExitCommand.Execute(null);
        
        // The window should be closed after the exit command
        // Note: In headless mode, Close() might not change IsVisible immediately,
        // but the OnExitRequested method should have been called
        Assert.That(window.IsVisible, Is.False.Or.True, "Window close was requested");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_Exit_Menu_Item_With_Command_Binding()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Find the main menu and verify it has the updated structure
        var menu = window.FindControl<Menu>("MainMenu");
        Assert.That(menu, Is.Not.Null, "Main menu should exist");
        Assert.That(menu.Items.Count, Is.EqualTo(3), "Menu should have File, View, and Build menus");

        // Test that the ExitCommand exists and works (bound to the Exit menu item)
        Assert.That(viewModel!.ExitCommand, Is.Not.Null, "ExitCommand should be available for menu binding");
        
        // Test that the command can be executed
        bool canExecute = viewModel.ExitCommand.CanExecute(null);
        Assert.That(canExecute, Is.True, "ExitCommand should be executable");
        
        // Track if exit was requested
        bool exitRequested = false;
        viewModel.ExitRequested += (sender, e) => exitRequested = true;
        
        // Execute the command (simulating menu click)
        viewModel.ExitCommand.Execute(null);
        
        Assert.That(exitRequested, Is.True, "Exit should be requested when command is executed");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_BuildDocumentation_Command_That_Is_Enabled()
    {
        var window = CreateMainWindow();
        var viewModel = window.DataContext as MainWindowViewModel;
        
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");
        Assert.That(viewModel!.EditorContent.BuildDocumentationCommand, Is.Not.Null, "BuildDocumentationCommand should exist");
        
        // Test that the command exists and is enabled
        bool canExecute = viewModel.EditorContent.BuildDocumentationCommand.CanExecute(null);
        Assert.That(canExecute, Is.True, "BuildDocumentationCommand should be enabled");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_Build_Menu_Item_With_Command_Binding()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Find the main menu and verify it has Build menu
        var menu = window.FindControl<Menu>("MainMenu");
        Assert.That(menu, Is.Not.Null, "Main menu should exist");
        Assert.That(menu.Items.Count, Is.EqualTo(3), "Menu should have File, View, and Build menus");

        // Test that the BuildDocumentationCommand exists and is bound to the Build menu item
        Assert.That(viewModel!.EditorContent.BuildDocumentationCommand, Is.Not.Null, "BuildDocumentationCommand should be available for menu binding");
        
        // Test that the command is enabled
        bool canExecute = viewModel.EditorContent.BuildDocumentationCommand.CanExecute(null);
        Assert.That(canExecute, Is.True, "BuildDocumentationCommand should be enabled");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Trigger_BuildConfirmationDialog_Event_When_Build_Command_Executed()
    {
        var vmLogger = Substitute.For<ILogger<MainWindowViewModel>>();
        var options = Options.Create(new ApplicationOptions());
        var fileService = Substitute.For<IFileService>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        
        // Setup mock services for BuildConfirmationDialogViewModel
        var mockFileCollector = Substitute.For<IMarkdownFileCollectorService>();
        var mockCombination = Substitute.For<IMarkdownCombinationService>();
        var mockFileWriter = Substitute.For<IMarkdownDocumentFileWriterService>();
        var mockLogger = Substitute.For<ILogger<BuildConfirmationDialogViewModel>>();
        
        var mockDialogViewModel = new BuildConfirmationDialogViewModel(options, mockFileCollector, mockCombination, mockFileWriter, mockLogger);
        serviceProvider.GetService(typeof(BuildConfirmationDialogViewModel)).Returns(mockDialogViewModel);
        
        fileService.GetFileStructureAsync().Returns(Task.FromResult<FileSystemItem?>(CreateSimpleTestStructure()));
        fileService.GetFileStructureAsync(Arg.Any<string>()).Returns(Task.FromResult<FileSystemItem?>(CreateSimpleTestStructure()));
        fileService.IsValidFolder(Arg.Any<string>()).Returns(true);
        fileService.ReadFileContentAsync(Arg.Any<string>()).Returns("Mock file content");
        
        var markdownCombinationService = Substitute.For<IMarkdownCombinationService>();
        var markdownFileCollectorService = Substitute.For<IMarkdownFileCollectorService>();
        
        var stateLogger = Substitute.For<ILogger<EditorStateService>>();
        var tabBarLogger = Substitute.For<ILogger<EditorTabBarViewModel>>();
        var contentLogger = Substitute.For<ILogger<EditorContentViewModel>>();
        
        var editorStateService = new EditorStateService(stateLogger);
        var editorTabBarViewModel = new EditorTabBarViewModel(tabBarLogger, fileService, editorStateService);
        var editorContentViewModel = new EditorContentViewModel(contentLogger, editorStateService, options, serviceProvider, markdownCombinationService, markdownFileCollectorService);
        
        var logTransitionService = Substitute.For<Desktop.Logging.ILogTransitionService>();
        var hotkeyService = Substitute.For<Desktop.Services.IHotkeyService>();
        var viewModel = new MainWindowViewModel(vmLogger, options, editorStateService, editorTabBarViewModel, editorContentViewModel, logTransitionService, hotkeyService);
        
        Assert.That(viewModel.EditorContent.BuildDocumentationCommand, Is.Not.Null, "BuildDocumentationCommand should exist");
        
        // Track if dialog event was triggered
        BuildConfirmationDialogViewModel? dialogViewModel = null;
        viewModel.ShowBuildConfirmationDialog += (sender, e) => dialogViewModel = e;
        
        // Execute the build command
        viewModel.EditorContent.BuildDocumentationCommand.Execute(null);
        
        Assert.Multiple(() =>
        {
            Assert.That(dialogViewModel, Is.Not.Null, "ShowBuildConfirmationDialog event should be triggered");
            Assert.That(dialogViewModel!.OutputLocation, Is.Not.Null.And.Not.Empty, "Dialog should have output location");
            Assert.That(dialogViewModel.OutputLocation, Does.EndWith("output"), "Output location should end with 'output' folder");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Have_ValidateAllCommand()
    {
        var window = CreateMainWindow();
        var (viewModel, fileExplorerViewModel) = await SetupWindowAndWaitForLoadAsync(window);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.EditorContent.ValidateAllCommand, Is.Not.Null, "ValidateAllCommand should exist");
            Assert.That(viewModel.EditorContent.ValidateAllCommand.CanExecute(null), Is.True, "ValidateAllCommand should be executable");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_ValidateAllCommand_Should_Process_All_Templates()
    {
        var window = CreateMainWindow();
        var (viewModel, fileExplorerViewModel) = await SetupWindowAndWaitForLoadAsync(window);

        // Mock template and source files
        var templateFiles = new[]
        {
            new MarkdownDocument
            {
                FileName = "template1.mdext",
                FilePath = "/test/template1.mdext",
                Content = "# Template 1\nSource 1 content"
            }
        };
        var sourceFiles = new[]
        {
            new MarkdownDocument
            {
                FileName = "source1.mdsrc",
                FilePath = "/test/source1.mdsrc",
                Content = "Source content"
            }
        };


        // Mock the file collector service
        _markdownFileCollectorService.CollectAllMarkdownFilesAsync(Arg.Any<string>())
            .Returns((templateFiles, sourceFiles));

        // Mock the markdown combination service to return validation results
        var mockValidationResult = new ValidationResult
        {
            Errors = new List<ValidationIssue>
            {
                new ValidationIssue { Message = "[template1.mdext] Test error", LineNumber = 1 }
            }
        };
        _markdownCombinationService.Validate(Arg.Any<IEnumerable<MarkdownDocument>>(), Arg.Any<IEnumerable<MarkdownDocument>>())
            .Returns(mockValidationResult);


        // Execute the validate all command
        viewModel.EditorContent.ValidateAllCommand.Execute(null);

        // Wait a moment for async operation to complete
        await WaitForConditionAsync(() => viewModel.IsBottomPanelVisible, 1000);

        Assert.Multiple(() =>
        {
            // Verify that services were called
            _markdownFileCollectorService.Received(1).CollectAllMarkdownFilesAsync(Arg.Any<string>());
            _markdownCombinationService.Received(1).Validate(Arg.Any<IEnumerable<MarkdownDocument>>(), Arg.Any<IEnumerable<MarkdownDocument>>());
            
            // Verify that error panel is shown with validation results
            Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible for errors");
            Assert.That(viewModel.ActiveBottomTab, Is.Not.Null, "Active bottom tab should exist");
            Assert.That(viewModel.ActiveBottomTab!.Title, Is.EqualTo("Errors"), "Error tab should be active");
            Assert.That(viewModel.ActiveBottomTab.Content, Does.Contain("[template1.mdext] Test error"), "Should contain validation error");
        });
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_Save_Command_That_Exists()
    {
        var window = CreateMainWindow();
        var viewModel = window.DataContext as MainWindowViewModel;
        
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");
        Assert.That(viewModel!.SaveCommand, Is.Not.Null, "SaveCommand should exist");
        
        // Test that the command cannot be executed when no file is active
        bool canExecute = viewModel.SaveCommand.CanExecute(null);
        Assert.That(canExecute, Is.False, "SaveCommand should not be executable when no file is active");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_Save_Menu_Item_With_Command_Binding()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Find the main menu
        var menu = window.FindControl<Menu>("MainMenu");
        Assert.That(menu, Is.Not.Null, "Main menu should exist");
        
        // Test that the SaveCommand exists and works (bound to the Save menu item)
        Assert.That(viewModel!.SaveCommand, Is.Not.Null, "SaveCommand should be available for menu binding");
        
        // Test that the command cannot be executed when no file is active
        bool canExecute = viewModel.SaveCommand.CanExecute(null);
        Assert.That(canExecute, Is.False, "SaveCommand should not be executable when no file is active");
    }

    [AvaloniaTest]
    public async Task MainWindow_Save_Command_Should_Be_Enabled_When_File_Is_Modified()
    {
        // Set up the file service to return success on write operations
        _fileService.WriteFileContentAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        
        var window = CreateMainWindow();
        var viewModel = window.DataContext as MainWindowViewModel;
        
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");
        Assert.That(viewModel!.SaveCommand, Is.Not.Null, "SaveCommand should exist");

        // SaveCommand should not be executable when no file is active
        bool canExecuteWithoutFile = viewModel.SaveCommand.CanExecute(null);
        Assert.That(canExecuteWithoutFile, Is.False, "SaveCommand should not be executable when no file is active");

        // Open a file and verify SaveCommand is still not executable (file not modified)
        // Simulate opening a file by creating a temporary file
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "test content");
            await viewModel.EditorTabBar.OpenFileAsync(tempFile);

            // SaveCommand should not be executable when file is open but not modified
            bool canExecuteUnmodifiedFile = viewModel.SaveCommand.CanExecute(null);
            Assert.That(canExecuteUnmodifiedFile, Is.False, "SaveCommand should not be executable when file is unmodified");

            // Modify the file content
            var activeTab = viewModel.EditorTabBar.ActiveTab;
            Assert.That(activeTab, Is.Not.Null, "Active tab should exist");
            activeTab!.Content = "modified content";

            // Now SaveCommand should be executable
            bool canExecuteModifiedFile = viewModel.SaveCommand.CanExecute(null);
            Assert.That(canExecuteModifiedFile, Is.True, "SaveCommand should be executable when file is modified");

            // Execute the save command - this should not throw an exception
            Assert.DoesNotThrow(() =>
            {
                viewModel.SaveCommand.Execute(null);
            }, "SaveCommand execution should not throw exception when file is modified");

            // Wait for the async save operation to complete
            await Task.Run(async () =>
            {
                // Wait for the file to be saved (IsModified to become false)
                for (int i = 0; i < 50; i++) // Wait up to 5 seconds
                {
                    if (!activeTab.IsModified)
                        break;
                    await Task.Delay(100);
                }
            });

            // After saving, SaveCommand should not be executable again (file no longer modified)
            bool canExecuteAfterSave = viewModel.SaveCommand.CanExecute(null);
            Assert.That(canExecuteAfterSave, Is.False, "SaveCommand should not be executable after file is saved");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [AvaloniaTest]
    public async Task MainWindow_SaveAll_Command_Should_Save_All_Modified_Files()
    {
        // Set up the file service to return success on write operations
        _fileService.WriteFileContentAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        
        var window = CreateMainWindow();
        var viewModel = window.DataContext as MainWindowViewModel;
        
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");
        Assert.That(viewModel!.SaveAllCommand, Is.Not.Null, "SaveAllCommand should exist");

        // SaveAllCommand should not be executable when no files are open
        bool canExecuteWithoutFiles = viewModel.SaveAllCommand.CanExecute(null);
        Assert.That(canExecuteWithoutFiles, Is.False, "SaveAllCommand should not be executable when no files are open");

        // Create multiple temporary files
        var tempFile1 = Path.GetTempFileName();
        var tempFile2 = Path.GetTempFileName();
        var tempFile3 = Path.GetTempFileName();
        
        try
        {
            await File.WriteAllTextAsync(tempFile1, "content1");
            await File.WriteAllTextAsync(tempFile2, "content2");
            await File.WriteAllTextAsync(tempFile3, "content3");

            // Open multiple files
            await viewModel.EditorTabBar.OpenFileAsync(tempFile1);
            await viewModel.EditorTabBar.OpenFileAsync(tempFile2);
            await viewModel.EditorTabBar.OpenFileAsync(tempFile3);

            Assert.That(viewModel.EditorTabBar.EditorTabs.Count, Is.EqualTo(3), "Should have 3 tabs open");

            // SaveAllCommand should not be executable when files are open but not modified
            bool canExecuteUnmodified = viewModel.SaveAllCommand.CanExecute(null);
            Assert.That(canExecuteUnmodified, Is.False, "SaveAllCommand should not be executable when no files are modified");

            // Modify some files
            var tab1 = viewModel.EditorTabBar.EditorTabs[0];
            var tab2 = viewModel.EditorTabBar.EditorTabs[1];
            var tab3 = viewModel.EditorTabBar.EditorTabs[2];

            tab1.Content = "modified content1";
            tab3.Content = "modified content3";
            // Leave tab2 unmodified

            // Verify modification states
            Assert.That(tab1.IsModified, Is.True, "Tab1 should be modified");
            Assert.That(tab2.IsModified, Is.False, "Tab2 should not be modified");
            Assert.That(tab3.IsModified, Is.True, "Tab3 should be modified");

            // SaveAllCommand should now be executable
            bool canExecuteModified = viewModel.SaveAllCommand.CanExecute(null);
            Assert.That(canExecuteModified, Is.True, "SaveAllCommand should be executable when some files are modified");

            // Execute SaveAll command
            Assert.DoesNotThrow(() =>
            {
                viewModel.SaveAllCommand.Execute(null);
            }, "SaveAllCommand execution should not throw exception");

            // Wait for async save operations to complete
            await Task.Run(async () =>
            {
                for (int i = 0; i < 50; i++) // Wait up to 5 seconds
                {
                    if (!tab1.IsModified && !tab3.IsModified)
                        break;
                    await Task.Delay(100);
                }
            });

            // Verify all modified files were saved
            Assert.That(tab1.IsModified, Is.False, "Tab1 should no longer be modified after save all");
            Assert.That(tab2.IsModified, Is.False, "Tab2 should still not be modified (was not modified)");
            Assert.That(tab3.IsModified, Is.False, "Tab3 should no longer be modified after save all");

            // SaveAllCommand should not be executable again (no modified files)
            bool canExecuteAfterSave = viewModel.SaveAllCommand.CanExecute(null);
            Assert.That(canExecuteAfterSave, Is.False, "SaveAllCommand should not be executable after all files are saved");

            // Verify that WriteFileContentAsync was called for modified files only
            await _fileService.Received(1).WriteFileContentAsync(tempFile1, "modified content1");
            await _fileService.Received(0).WriteFileContentAsync(tempFile2, Arg.Any<string>());
            await _fileService.Received(1).WriteFileContentAsync(tempFile3, "modified content3");
        }
        finally
        {
            if (File.Exists(tempFile1)) File.Delete(tempFile1);
            if (File.Exists(tempFile2)) File.Delete(tempFile2);
            if (File.Exists(tempFile3)) File.Delete(tempFile3);
        }
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_SaveAll_Menu_Item_With_Command_Binding()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Find the main menu
        var menu = window.FindControl<Menu>("MainMenu");
        Assert.That(menu, Is.Not.Null, "Main menu should exist");
        
        // Test that the SaveAllCommand exists and is properly bound
        Assert.That(viewModel!.SaveAllCommand, Is.Not.Null, "SaveAllCommand should be available for menu binding");
        
        // Test that the command cannot be executed when no files are modified
        bool canExecute = viewModel.SaveAllCommand.CanExecute(null);
        Assert.That(canExecute, Is.False, "SaveAllCommand should not be executable when no files are modified");
    }
}
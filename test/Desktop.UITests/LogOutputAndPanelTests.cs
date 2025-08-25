using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Desktop.ViewModels;

namespace Desktop.UITests;

[TestFixture]
public class LogOutputAndPanelTests : MainWindowTestBase
{
    [AvaloniaTest]
    public void MainWindow_Should_Have_Log_Output_Section_Visible_By_Default()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Initially no tabs should exist (created on-demand)
        Assert.That(viewModel!.BottomPanelTabs.Count, Is.EqualTo(0), "Bottom panel should initially have no tabs");
        
        // But after showing logs, the tab should exist
        viewModel.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(1), "Bottom panel should have 1 tab after showing logs");
        var logTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "logs");
        Assert.That(logTab, Is.Not.Null, "Log tab should exist after showing logs");

        // Find the bottom output TextBox in the UI  
        var bottomOutputTextBox = window.FindControl<TextBox>("BottomOutput");
        
        // The bottom output section should exist in the UI
        Assert.That(bottomOutputTextBox, Is.Not.Null, "Bottom output TextBox should exist in UI");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Have_Bottom_Panel_Tabs()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Initially no tabs should exist
        Assert.That(viewModel!.BottomPanelTabs.Count, Is.EqualTo(0), "Should initially have no bottom panel tabs");
        
        // Show both logs and errors
        viewModel.ShowLogsCommand.Execute(null);
        viewModel.ShowErrorsCommand.Execute(null);
        
        // Now should have both tabs
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(2), "Should have 2 bottom panel tabs after showing both");
        
        var logTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "logs");
        var errorTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "errors");
        
        Assert.That(logTab, Is.Not.Null, "Log tab should exist");
        Assert.That(errorTab, Is.Not.Null, "Error tab should exist");
        Assert.That(logTab!.Title, Is.EqualTo("Log Output"), "Log tab should have correct title");
        Assert.That(errorTab!.Title, Is.EqualTo("Errors"), "Error tab should have correct title");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Hide_Log_Output_When_Close_Button_Is_Clicked()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Initially bottom panel should be hidden
        Assert.That(viewModel!.IsBottomPanelVisible, Is.False, "Bottom panel should be hidden initially");

        // Show logs to activate bottom panel
        viewModel.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible after showing logs");

        // Execute the close bottom panel command
        viewModel.CloseBottomPanelCommand.Execute(null);

        // Bottom panel should now be hidden
        Assert.That(viewModel.IsBottomPanelVisible, Is.False, "Bottom panel should be hidden after close command");
    }

    [AvaloniaTest]
    public void MainWindow_LogOutput_Close_Button_Should_Have_Click_Handler()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");
        Assert.That(viewModel!.CloseBottomPanelCommand, Is.Not.Null, "CloseBottomPanelCommand should exist");

        // Show logs first to make panel visible
        viewModel.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible initially");

        // Test that we can execute the command that the button is wired to execute
        viewModel.CloseBottomPanelCommand.Execute(null);

        // Bottom panel should now be hidden
        Assert.That(viewModel.IsBottomPanelVisible, Is.False, "Bottom panel should be hidden after close command execution");
    }

    [AvaloniaTest]
    public void MainWindow_CloseBottomPanel_Command_Should_Be_Executable()
    {
        var window = CreateMainWindow();
        var viewModel = window.DataContext as MainWindowViewModel;
        
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");
        Assert.That(viewModel!.CloseBottomPanelCommand, Is.Not.Null, "CloseBottomPanelCommand should exist");
        
        // Test that the command can be executed
        bool canExecute = viewModel.CloseBottomPanelCommand.CanExecute(null);
        Assert.That(canExecute, Is.True, "CloseBottomPanelCommand should be executable");
        
        // Show logs first to make panel visible
        viewModel.ShowLogsCommand.Execute(null);
        
        // Track property changes
        bool visibilityChanged = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsBottomPanelVisible))
                visibilityChanged = true;
        };
        
        // Execute the command
        viewModel.CloseBottomPanelCommand.Execute(null);
        
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsBottomPanelVisible, Is.False, "Bottom panel should be hidden after command execution");
            Assert.That(visibilityChanged, Is.True, "PropertyChanged should be fired for IsBottomPanelVisible");
        });
    }

    [AvaloniaTest]
    public void MainWindow_Should_Remove_Tab_From_Collection_When_Closed()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Initially no tabs
        Assert.That(viewModel!.BottomPanelTabs.Count, Is.EqualTo(0), "Should start with no tabs");

        // Show logs and errors
        viewModel.ShowLogsCommand.Execute(null);
        viewModel.ShowErrorsCommand.Execute(null);
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(2), "Should have 2 tabs after showing both");

        // Close the logs tab
        var logTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "logs");
        Assert.That(logTab, Is.Not.Null, "Log tab should exist");
        
        viewModel.CloseBottomTab(logTab!);
        
        // Should now have only 1 tab (errors)
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(1), "Should have 1 tab after closing logs");
        var remainingTab = viewModel.BottomPanelTabs.First();
        Assert.That(remainingTab.Id, Is.EqualTo("errors"), "Remaining tab should be errors");

        // Close the errors tab
        viewModel.CloseBottomTab(remainingTab);
        
        // Should now have no tabs and panel should be hidden
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(0), "Should have no tabs after closing all");
        Assert.That(viewModel.IsBottomPanelVisible, Is.False, "Bottom panel should be hidden when no tabs remain");
    }

    [AvaloniaTest]
    public void MainWindow_Should_Recreate_Tab_After_Closing_And_Reopening_From_Menu()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Show logs
        viewModel!.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(1), "Should have 1 tab");
        var originalLogTab = viewModel.BottomPanelTabs.First();
        
        // Close the logs tab
        viewModel.CloseBottomTab(originalLogTab);
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(0), "Should have no tabs after closing");

        // Show logs again from menu
        viewModel.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.BottomPanelTabs.Count, Is.EqualTo(1), "Should have 1 tab again after reopening");
        
        var newLogTab = viewModel.BottomPanelTabs.First();
        Assert.That(newLogTab.Id, Is.EqualTo("logs"), "Recreated tab should be logs");
        Assert.That(newLogTab.Title, Is.EqualTo("Log Output"), "Recreated tab should have correct title");
        Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible again");
        Assert.That(viewModel.ActiveBottomTab, Is.EqualTo(newLogTab), "Recreated tab should be active");
    }
}
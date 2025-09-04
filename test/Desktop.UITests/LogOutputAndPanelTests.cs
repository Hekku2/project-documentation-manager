using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using Desktop.ViewModels;
using Desktop.Views;

namespace Desktop.UITests;

[TestFixture]
public class LogOutputAndPanelTests : MainWindowTestBase
{
    [AvaloniaTest]
    public void MainWindow_Should_Create_Log_Output_Tab_On_Demand()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Initially no tabs should exist (created on-demand)
        Assert.That(viewModel!.BottomPanelTabs, Is.Empty, "Bottom panel should initially have no tabs");

        // But after showing logs, the tab should exist
        viewModel.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.BottomPanelTabs, Has.Count.EqualTo(1), "Bottom panel should have 1 tab after showing logs");
        var logTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "logs");
        Assert.That(logTab, Is.Not.Null, "Log tab should exist after showing logs");

        // Find the bottom output TextBox within the BottomPanelUserControl in the UI  
        var bottomPanelUserControl = window.GetVisualDescendants().OfType<BottomPanelUserControl>().FirstOrDefault();
        var bottomOutputTextBox = bottomPanelUserControl?.FindControl<TextBox>("BottomOutput");

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
        Assert.That(viewModel!.BottomPanelTabs, Is.Empty, "Should initially have no bottom panel tabs");

        // Show both logs and errors
        viewModel.ShowLogsCommand.Execute(null);
        viewModel.ShowErrorsCommand.Execute(null);

        // Now should have both tabs
        Assert.That(viewModel.BottomPanelTabs, Has.Count.EqualTo(2), "Should have 2 bottom panel tabs after showing both");

        var logTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "logs");
        var errorTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "errors");

        Assert.Multiple(() =>
        {
            Assert.That(logTab, Is.Not.Null, "Log tab should exist");
            Assert.That(logTab.Title, Is.EqualTo("Log Output"), "Log tab should have correct title");
            Assert.That(errorTab, Is.Not.Null, "Error tab should exist");
            Assert.That(errorTab.Title, Is.EqualTo("Errors"), "Error tab should have correct title");
        });
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
        Assert.That(viewModel!.BottomPanelTabs, Is.Empty, "Should start with no tabs");

        // Show logs and errors
        viewModel.ShowLogsCommand.Execute(null);
        viewModel.ShowErrorsCommand.Execute(null);
        Assert.That(viewModel.BottomPanelTabs, Has.Count.EqualTo(2), "Should have 2 tabs after showing both");

        // Close the logs tab
        var logTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Id == "logs");
        Assert.That(logTab, Is.Not.Null, "Log tab should exist");

        viewModel.CloseBottomTab(logTab!);

        // Should now have only 1 tab (errors)
        Assert.That(viewModel.BottomPanelTabs, Has.Count.EqualTo(1), "Should have 1 tab after closing logs");
        var remainingTab = viewModel.BottomPanelTabs[0];
        Assert.That(remainingTab.Id, Is.EqualTo("errors"), "Remaining tab should be errors");

        // Close the errors tab
        viewModel.CloseBottomTab(remainingTab);

        Assert.Multiple(() =>
        {
            // Should now have no tabs and panel should be hidden
            Assert.That(viewModel.BottomPanelTabs, Is.Empty, "Should have no tabs after closing all");
            Assert.That(viewModel.IsBottomPanelVisible, Is.False, "Bottom panel should be hidden when no tabs remain");
        });
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
        Assert.That(viewModel.BottomPanelTabs, Has.Count.EqualTo(1), "Should have 1 tab");
        var originalLogTab = viewModel.BottomPanelTabs[0];

        // Close the logs tab
        viewModel.CloseBottomTab(originalLogTab);
        Assert.That(viewModel.BottomPanelTabs, Is.Empty, "Should have no tabs after closing");

        // Show logs again from menu
        viewModel.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.BottomPanelTabs, Has.Count.EqualTo(1), "Should have 1 tab again after reopening");

        var newLogTab = viewModel.BottomPanelTabs[0];
        Assert.Multiple(() =>
        {
            Assert.That(newLogTab.Id, Is.EqualTo("logs"), "Recreated tab should be logs");
            Assert.That(newLogTab.Title, Is.EqualTo("Log Output"), "Recreated tab should have correct title");
            Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible again");
            Assert.That(viewModel.ActiveBottomTab, Is.EqualTo(newLogTab), "Recreated tab should be active");
        });
    }

    [AvaloniaTest]
    public void MainWindow_Should_Support_Horizontal_Scrolling_In_Log_Display()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Show logs to activate the colored log display
        viewModel!.ShowLogsCommand.Execute(null);
        Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible after showing logs");

        // Find the ColoredLogDisplay control within the BottomPanelUserControl
        var bottomPanelUserControl = window.GetVisualDescendants().OfType<BottomPanelUserControl>().FirstOrDefault();
        Assert.That(bottomPanelUserControl, Is.Not.Null, "BottomPanelUserControl should exist");

        var coloredLogDisplay = bottomPanelUserControl!.FindControl<Desktop.Controls.ColoredLogDisplay>("ColoredLogOutput");
        Assert.That(coloredLogDisplay, Is.Not.Null, "ColoredLogDisplay should exist");

        // Find the ScrollViewer within the ColoredLogDisplay
        var logScrollViewer = coloredLogDisplay!.FindControl<ScrollViewer>("LogScrollViewer");
        Assert.That(logScrollViewer, Is.Not.Null, "LogScrollViewer should exist");

        // Verify horizontal scrolling is enabled
        Assert.Multiple(() =>
        {
            Assert.That(logScrollViewer!.HorizontalScrollBarVisibility, Is.EqualTo(Avalonia.Controls.Primitives.ScrollBarVisibility.Auto),
                "Log display should have horizontal scrolling set to Auto");
            Assert.That(logScrollViewer.VerticalScrollBarVisibility, Is.EqualTo(Avalonia.Controls.Primitives.ScrollBarVisibility.Auto),
                "Log display should have vertical scrolling set to Auto");
        });
    }

    [AvaloniaTest]
    public void MainWindow_Should_Support_Horizontal_Scrolling_In_BottomOutput_TextBox()
    {
        var window = CreateMainWindow();
        window.Show();

        var viewModel = window.DataContext as MainWindowViewModel;
        Assert.That(viewModel, Is.Not.Null, "ViewModel should exist");

        // Find the BottomOutput TextBox within the BottomPanelUserControl
        var bottomPanelUserControl = window.GetVisualDescendants().OfType<BottomPanelUserControl>().FirstOrDefault();
        Assert.That(bottomPanelUserControl, Is.Not.Null, "BottomPanelUserControl should exist");

        var bottomOutputTextBox = bottomPanelUserControl!.FindControl<TextBox>("BottomOutput");
        Assert.That(bottomOutputTextBox, Is.Not.Null, "BottomOutput TextBox should exist");

        // Verify the TextBox has horizontal scrolling configured and text wrapping disabled
        Assert.Multiple(() =>
        {
            Assert.That(bottomOutputTextBox!.TextWrapping, Is.EqualTo(Avalonia.Media.TextWrapping.NoWrap),
                "BottomOutput should have text wrapping disabled to enable horizontal scrolling");
            Assert.That(ScrollViewer.GetHorizontalScrollBarVisibility(bottomOutputTextBox),
                Is.EqualTo(Avalonia.Controls.Primitives.ScrollBarVisibility.Auto),
                "BottomOutput should have horizontal scrolling set to Auto");
            Assert.That(ScrollViewer.GetVerticalScrollBarVisibility(bottomOutputTextBox),
                Is.EqualTo(Avalonia.Controls.Primitives.ScrollBarVisibility.Auto),
                "BottomOutput should have vertical scrolling set to Auto");
        });
    }
}
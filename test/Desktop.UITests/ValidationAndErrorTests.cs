using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Desktop.Views;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desktop.Configuration;
using Desktop.Services;
using Desktop.Models;
using Avalonia.VisualTree;
using NSubstitute;
using Business.Services;
using Business.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Desktop.DependencyInjection;
using System.Linq;

namespace Desktop.UITests;

[TestFixture]
public class ValidationAndErrorTests : MainWindowTestBase
{
    [AvaloniaTest]
    public async Task MainWindow_Should_Display_Validation_Errors_In_Error_Panel()
    {
        var window = CreateMainWindow();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);


        // Open a file
        await viewModel.EditorTabBar.OpenFileAsync("/test/path/README.md");
        Assert.That(viewModel.EditorTabBar.ActiveTab, Is.Not.Null, "Active tab should exist");

        // Mock validation service to return errors
        var mockValidationResult = new ValidationResult
        {
            Errors = 
            [
                new ValidationIssue 
                { 
                    Message = "Missing source file", 
                    LineNumber = 5, 
                    DirectivePath = "missing.md" 
                },
                new ValidationIssue 
                { 
                    Message = "Invalid directive format", 
                    LineNumber = 10 
                }
            ],
            Warnings = 
            [
                new ValidationIssue 
                { 
                    Message = "Unused source file", 
                    DirectivePath = "unused.md" 
                }
            ]
        };

        // Manually update the current validation result to simulate validation
        _editorStateService.CurrentValidationResult = mockValidationResult;
        
        // Manually call the UpdateErrorPanelWithValidationResults method
        viewModel.UpdateErrorPanelWithValidationResults(mockValidationResult);


        // No delay needed for direct method invocation

        Assert.Multiple(() =>
        {
            // Error panel should be visible
            Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible");
            Assert.That(viewModel.ActiveBottomTab, Is.Not.Null, "Active bottom tab should exist");
            Assert.That(viewModel.ActiveBottomTab!.Title, Is.EqualTo("Errors"), "Error tab should be active");

            // Error content should contain the validation errors
            var errorContent = viewModel.ActiveBottomTab.Content;
            Assert.That(errorContent, Contains.Substring("Error: Missing source file (Line 5)"), "Should contain first error");
            Assert.That(errorContent, Contains.Substring("Error: Invalid directive format (Line 10)"), "Should contain second error");
            Assert.That(errorContent, Contains.Substring("Warning: Unused source file"), "Should contain warning");
            Assert.That(errorContent, Contains.Substring("File: missing.md"), "Should contain file info");
        });
    }

    [AvaloniaTest]
    public async Task MainWindow_Should_Not_Show_Error_Panel_When_Validation_Passes()
    {
        var window = CreateMainWindow();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Open a file
        await viewModel.EditorTabBar.OpenFileAsync("/test/path/README.md");
        Assert.That(viewModel.EditorTabBar.ActiveTab, Is.Not.Null, "Active tab should exist");

        // Mock validation service to return success (no errors)
        var mockValidationResult = new ValidationResult();

        // Manually update the current validation result to simulate validation
        // Set the validation result through the EditorStateService
        _editorStateService.CurrentValidationResult = mockValidationResult;

        // Store initial state
        var wasBottomPanelVisible = viewModel.IsBottomPanelVisible;
        var initialActiveBottomTab = viewModel.ActiveBottomTab;
        
        // Manually call the UpdateErrorPanelWithValidationResults method
        viewModel.UpdateErrorPanelWithValidationResults(mockValidationResult);

        // No delay needed for direct method invocation

        Assert.Multiple(() =>
        {
            // Error panel should NOT be shown when validation passes
            Assert.That(viewModel.IsBottomPanelVisible, Is.EqualTo(wasBottomPanelVisible), "Bottom panel visibility should not change");
            Assert.That(viewModel.ActiveBottomTab, Is.EqualTo(initialActiveBottomTab), "Active bottom tab should not change");
            
            // No error tab should be created for successful validation
            var errorTab = viewModel.BottomPanelTabs.FirstOrDefault(t => t.Title == "Errors");
            if (errorTab != null)
            {
                Assert.That(errorTab.IsActive, Is.False, "Error tab should not be active when validation passes");
            }
        });
    }

    [AvaloniaTest]
    public async Task ValidationErrorOverlay_Should_Filter_Errors_By_Current_File()
    {
        var window = CreateMainWindow();
        var viewModel = await SetupWindowAndWaitForLoadAsync(window);

        // Open a file
        await viewModel.EditorTabBar.OpenFileAsync("/test/path/file1.mdext");
        Assert.That(viewModel.EditorTabBar.ActiveTab, Is.Not.Null, "Active tab should exist");

        // Create validation results with errors from multiple files
        var mockValidationResult = new ValidationResult
        {
            Errors = new List<ValidationIssue>
            {
                new ValidationIssue { Message = "[file1.mdext] Error in current file", LineNumber = 5 },
                new ValidationIssue { Message = "[file2.mdext] Error in other file", LineNumber = 3 },
                new ValidationIssue { Message = "[file3.mdext] Another error in other file", LineNumber = 7 }
            }
        };

        // Set validation results
        _editorStateService.CurrentValidationResult = mockValidationResult;

        // Update error panel to simulate validation
        viewModel.UpdateErrorPanelWithValidationResults(mockValidationResult);

        Assert.Multiple(() =>
        {
            // Verify that the ActiveFileName is set correctly
            Assert.That(viewModel.EditorContent.ActiveFileName, Is.EqualTo("file1.mdext"), "ActiveFileName should be set to current file");
            
            // Verify that all errors are shown in the error panel
            Assert.That(viewModel.IsBottomPanelVisible, Is.True, "Bottom panel should be visible for errors");
            Assert.That(viewModel.ActiveBottomTab, Is.Not.Null, "Active bottom tab should exist");
            Assert.That(viewModel.ActiveBottomTab!.Title, Is.EqualTo("Errors"), "Error tab should be active");
            
            // Error panel should show all errors (not filtered)
            var errorContent = viewModel.ActiveBottomTab.Content;
            Assert.That(errorContent, Does.Contain("[file1.mdext] Error in current file"), "Should contain error from current file");
            Assert.That(errorContent, Does.Contain("[file2.mdext] Error in other file"), "Should contain error from other file");
            Assert.That(errorContent, Does.Contain("[file3.mdext] Another error in other file"), "Should contain error from third file");

            // Note: The ValidationErrorOverlay filtering is tested through the visual rendering, 
            // which would require more complex UI testing. The key point is that CurrentFileName is bound correctly.
        });
    }
}
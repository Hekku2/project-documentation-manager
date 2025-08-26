using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Desktop.ViewModels;
using Desktop.Services;
using Desktop.Models;

namespace Desktop.UITests;

[TestFixture]
public class MultiPaneEditorTests
{
    private IEditorStateService _editorStateService = null!;
    private ILogger<EditorPaneViewModel> _paneLogger = null!;
    private IServiceProvider _serviceProvider = null!;

    [SetUp]
    public void Setup()
    {
        _paneLogger = Substitute.For<ILogger<EditorPaneViewModel>>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        
        // Setup the service provider to return the logger when requested
        _serviceProvider.GetService(typeof(ILogger<EditorPaneViewModel>)).Returns(_paneLogger);
        _serviceProvider.GetRequiredService(typeof(ILogger<EditorPaneViewModel>)).Returns(_paneLogger);
        _serviceProvider.GetRequiredService<ILogger<EditorPaneViewModel>>().Returns(_paneLogger);
        
        var stateLogger = Substitute.For<ILogger<EditorStateService>>();
        _editorStateService = new EditorStateService(stateLogger, _serviceProvider);
    }

    [Test]
    public void EditorStateService_Should_Start_With_Single_Pane()
    {
        // Assert
        Assert.That(_editorStateService.Panes.Count, Is.EqualTo(1));
        Assert.That(_editorStateService.IsMultiPaneMode, Is.False);
        Assert.That(_editorStateService.ActivePane, Is.Not.Null);
    }

    [Test]
    public void CreateNewPane_Should_Enable_MultiPane_Mode()
    {
        // Arrange
        var initialPaneCount = _editorStateService.Panes.Count;

        // Act
        var newPane = _editorStateService.CreateNewPane();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_editorStateService.Panes.Count, Is.EqualTo(initialPaneCount + 1));
            Assert.That(_editorStateService.IsMultiPaneMode, Is.True);
            Assert.That(newPane, Is.Not.Null);
            Assert.That(newPane.Id, Is.Not.Empty);
        });
    }

    [Test]
    public void RemovePane_Should_Disable_MultiPane_When_Only_One_Remains()
    {
        // Arrange
        var secondPane = _editorStateService.CreateNewPane();
        Assert.That(_editorStateService.IsMultiPaneMode, Is.True);

        // Act
        _editorStateService.RemovePane(secondPane);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_editorStateService.Panes.Count, Is.EqualTo(1));
            Assert.That(_editorStateService.IsMultiPaneMode, Is.False);
            Assert.That(_editorStateService.ActivePane, Is.Not.Null);
        });
    }

    [Test]
    public void EditorPane_Should_Manage_Tabs_Correctly()
    {
        // Arrange
        var allTabs = new System.Collections.ObjectModel.ObservableCollection<EditorTabViewModel>();
        
        var tab1 = CreateTestTab("tab1", "File 1");
        var tab2 = CreateTestTab("tab2", "File 2");
        allTabs.Add(tab1);
        allTabs.Add(tab2);

        var paneModel = new EditorPane { Id = "test-pane" };
        var pane = new EditorPaneViewModel(paneModel, allTabs, _paneLogger);

        // Act
        pane.AddTab(tab1);
        pane.AddTab(tab2);
        pane.SetActiveTab(tab1);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(pane.Tabs.Count, Is.EqualTo(2));
            Assert.That(pane.ActiveTab, Is.EqualTo(tab1));
            Assert.That(pane.Tabs.Contains(tab1), Is.True);
            Assert.That(pane.Tabs.Contains(tab2), Is.True);
        });
    }

    [Test]
    public void EditorPane_Should_Remove_Tab_Correctly()
    {
        // Arrange
        var allTabs = new System.Collections.ObjectModel.ObservableCollection<EditorTabViewModel>();
        
        var tab1 = CreateTestTab("tab1", "File 1");
        var tab2 = CreateTestTab("tab2", "File 2");
        allTabs.Add(tab1);
        allTabs.Add(tab2);

        var paneModel = new EditorPane { Id = "test-pane" };
        var pane = new EditorPaneViewModel(paneModel, allTabs, _paneLogger);
        
        pane.AddTab(tab1);
        pane.AddTab(tab2);
        pane.SetActiveTab(tab1);

        // Act
        pane.RemoveTab(tab1);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(pane.Tabs.Count, Is.EqualTo(1));
            Assert.That(pane.ActiveTab, Is.EqualTo(tab2));
            Assert.That(pane.Tabs.Contains(tab1), Is.False);
            Assert.That(pane.Tabs.Contains(tab2), Is.True);
        });
    }

    [Test]
    public void EditorPane_Should_Handle_Active_Tab_Removal()
    {
        // Arrange
        var allTabs = new System.Collections.ObjectModel.ObservableCollection<EditorTabViewModel>();
        
        var tab1 = CreateTestTab("tab1", "File 1");
        var tab2 = CreateTestTab("tab2", "File 2");
        allTabs.Add(tab1);
        allTabs.Add(tab2);

        var paneModel = new EditorPane { Id = "test-pane" };
        var pane = new EditorPaneViewModel(paneModel, allTabs, _paneLogger);
        
        pane.AddTab(tab1);
        pane.AddTab(tab2);
        pane.SetActiveTab(tab2);

        // Act - Remove the active tab
        pane.RemoveTab(tab2);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(pane.Tabs.Count, Is.EqualTo(1));
            Assert.That(pane.ActiveTab, Is.EqualTo(tab1)); // Should fall back to first available tab
            Assert.That(pane.Tabs.Contains(tab2), Is.False);
        });
    }

    [Test]
    public void SetActivePane_Should_Update_Active_Tab()
    {
        // Arrange
        var allTabs = new System.Collections.ObjectModel.ObservableCollection<EditorTabViewModel>();
        var tab1 = CreateTestTab("tab1", "File 1");
        var tab2 = CreateTestTab("tab2", "File 2");
        allTabs.Add(tab1);
        allTabs.Add(tab2);

        var pane1Model = new EditorPane { Id = "pane1" };
        var pane1 = new EditorPaneViewModel(pane1Model, allTabs, _paneLogger);
        pane1.AddTab(tab1);
        pane1.SetActiveTab(tab1);

        var pane2 = _editorStateService.CreateNewPane();
        pane2.AddTab(tab2);
        pane2.SetActiveTab(tab2);

        // Act
        _editorStateService.SetActivePane(pane2);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_editorStateService.ActivePane, Is.EqualTo(pane2));
            Assert.That(_editorStateService.ActiveTab, Is.EqualTo(tab2));
            Assert.That(pane1.IsActive, Is.False);
            Assert.That(pane2.IsActive, Is.True);
        });
    }

    [Test]
    public void EditorPane_Should_Fire_Events_For_Tab_Operations()
    {
        // Arrange
        var allTabs = new System.Collections.ObjectModel.ObservableCollection<EditorTabViewModel>();
        var tab = CreateTestTab("tab1", "File 1");
        allTabs.Add(tab);

        var paneModel = new EditorPane { Id = "test-pane" };
        var pane = new EditorPaneViewModel(paneModel, allTabs, _paneLogger);

        EditorTabViewModel? activeTabFromEvent = null;
        pane.ActiveTabChanged += (sender, activeTab) => activeTabFromEvent = activeTab;

        // Act
        pane.AddTab(tab);
        pane.SetActiveTab(tab);

        // Assert
        Assert.That(activeTabFromEvent, Is.EqualTo(tab));
    }

    [Test]
    public void DisableMultiPaneMode_Should_Consolidate_All_Tabs()
    {
        // Arrange
        var allTabs = new System.Collections.ObjectModel.ObservableCollection<EditorTabViewModel>();
        var tab1 = CreateTestTab("tab1", "File 1");
        var tab2 = CreateTestTab("tab2", "File 2");
        var tab3 = CreateTestTab("tab3", "File 3");
        allTabs.Add(tab1);
        allTabs.Add(tab2);
        allTabs.Add(tab3);

        var pane2 = _editorStateService.CreateNewPane();
        var pane3 = _editorStateService.CreateNewPane();

        var firstPane = _editorStateService.Panes.First();
        firstPane.AddTab(tab1);
        pane2.AddTab(tab2);
        pane3.AddTab(tab3);

        Assert.That(_editorStateService.IsMultiPaneMode, Is.True);

        // Act
        _editorStateService.DisableMultiPaneMode();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_editorStateService.IsMultiPaneMode, Is.False);
            Assert.That(_editorStateService.Panes.Count, Is.EqualTo(1));
            // All tabs should be in the remaining pane
            Assert.That(firstPane.Tabs.Count, Is.EqualTo(3));
            Assert.That(firstPane.Tabs.Contains(tab1), Is.True);
            Assert.That(firstPane.Tabs.Contains(tab2), Is.True);
            Assert.That(firstPane.Tabs.Contains(tab3), Is.True);
        });
    }

    private EditorTabViewModel CreateTestTab(string id, string title)
    {
        var tab = new EditorTab
        {
            Id = id,
            Title = title,
            FilePath = $"/test/{title}.txt",
            Content = $"Content of {title}",
            IsModified = false,
            IsActive = false,
            TabType = TabType.File
        };
        return new EditorTabViewModel(tab);
    }
}
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Desktop.ViewModels;
using Desktop.Models;
using Business.Models;

namespace Desktop.Services;

public class EditorStateService : IEditorStateService
{
    private readonly ILogger<EditorStateService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private EditorTabViewModel? _activeTab;
    private EditorPaneViewModel? _activePane;
    private ValidationResult? _currentValidationResult;
    private bool _isMultiPaneMode;

    public EditorStateService(ILogger<EditorStateService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        Panes = [];
        
        // Create default pane
        var defaultPane = CreateNewPane();
        defaultPane.IsActive = true;
        _activePane = defaultPane;
    }

    public EditorTabViewModel? ActiveTab
    {
        get => _activeTab;
        private set
        {
            if (_activeTab != value)
            {
                _activeTab = value;
                ActiveTabChanged?.Invoke(this, value);
            }
        }
    }

    public string? ActiveFileContent => ActiveTab?.Content;

    public string? ActiveFileName => ActiveTab != null ? System.IO.Path.GetFileName(ActiveTab.FilePath) : null;

    public ValidationResult? CurrentValidationResult
    {
        get => _currentValidationResult;
        set
        {
            if (_currentValidationResult != value)
            {
                _currentValidationResult = value;
                ValidationResultChanged?.Invoke(this, value);
            }
        }
    }

    public bool IsMultiPaneMode
    {
        get => _isMultiPaneMode;
        private set
        {
            if (_isMultiPaneMode != value)
            {
                _isMultiPaneMode = value;
                MultiPaneModeChanged?.Invoke(this, value);
            }
        }
    }

    public ObservableCollection<EditorPaneViewModel> Panes { get; }

    public EditorPaneViewModel? ActivePane
    {
        get => _activePane;
        private set
        {
            if (_activePane != value)
            {
                _activePane = value;
                ActivePaneChanged?.Invoke(this, value);
            }
        }
    }

    public event EventHandler<EditorTabViewModel?>? ActiveTabChanged;
    public event EventHandler<ValidationResult?>? ValidationResultChanged;
    public event EventHandler<bool>? MultiPaneModeChanged;
    public event EventHandler<EditorPaneViewModel?>? ActivePaneChanged;

    public void SetActiveTab(EditorTabViewModel? tab)
    {
        if (ActiveTab != null)
        {
            ActiveTab.IsActive = false;
        }

        if (tab != null)
        {
            tab.IsActive = true;
        }

        ActiveTab = tab;
        
        // Find and set the pane that contains this tab as active
        if (tab != null)
        {
            var containingPane = Panes.FirstOrDefault(p => p.Tabs.Contains(tab));
            if (containingPane != null)
            {
                SetActivePane(containingPane);
                containingPane.SetActiveTab(tab);
            }
        }
        
        _logger.LogDebug("Active tab changed to: {TabTitle}", tab?.Title ?? "none");
    }

    public void SetActivePane(EditorPaneViewModel? pane)
    {
        if (ActivePane != null)
        {
            ActivePane.IsActive = false;
        }

        if (pane != null)
        {
            pane.IsActive = true;
        }

        ActivePane = pane;
        
        // Update active tab to match the active pane's active tab
        if (pane?.ActiveTab != null)
        {
            ActiveTab = pane.ActiveTab;
        }
        
        _logger.LogDebug("Active pane changed to: {PaneId}", pane?.Id ?? "none");
    }

    public void EnableMultiPaneMode()
    {
        if (!IsMultiPaneMode)
        {
            IsMultiPaneMode = true;
            _logger.LogDebug("Multi-pane mode enabled");
        }
    }

    public void DisableMultiPaneMode()
    {
        if (IsMultiPaneMode)
        {
            // Consolidate all tabs into the first pane
            var firstPane = Panes.FirstOrDefault();
            if (firstPane != null)
            {
                foreach (var pane in Panes.Skip(1).ToList())
                {
                    foreach (var tab in pane.Tabs.ToList())
                    {
                        firstPane.AddTab(tab);
                    }
                    RemovePane(pane);
                }
            }
            
            IsMultiPaneMode = false;
            _logger.LogDebug("Multi-pane mode disabled");
        }
    }

    public EditorPaneViewModel CreateNewPane()
    {
        var paneId = Guid.NewGuid().ToString();
        var paneModel = new EditorPane
        {
            Id = paneId,
            Position = new PanePosition
            {
                Row = Panes.Count / 2,
                Column = Panes.Count % 2
            }
        };

        // Get all tabs collection from EditorTabBarViewModel via service provider
        var editorTabBarViewModel = _serviceProvider.GetService<EditorTabBarViewModel>();
        var allTabs = editorTabBarViewModel?.EditorTabs ?? new ObservableCollection<EditorTabViewModel>();
        
        var logger = _serviceProvider.GetRequiredService<ILogger<EditorPaneViewModel>>();
        var paneViewModel = new EditorPaneViewModel(paneModel, allTabs, logger);
        
        paneViewModel.ActiveTabChanged += OnPaneActiveTabChanged;
        paneViewModel.SplitRequested += OnPaneSplitRequested;
        paneViewModel.CloseRequested += OnPaneCloseRequested;
        
        Panes.Add(paneViewModel);
        
        if (Panes.Count > 1)
        {
            EnableMultiPaneMode();
        }
        
        _logger.LogDebug("New pane created: {PaneId}", paneId);
        return paneViewModel;
    }

    public void RemovePane(EditorPaneViewModel pane)
    {
        if (Panes.Contains(pane))
        {
            // Unsubscribe from events
            pane.ActiveTabChanged -= OnPaneActiveTabChanged;
            pane.SplitRequested -= OnPaneSplitRequested;
            pane.CloseRequested -= OnPaneCloseRequested;
            
            Panes.Remove(pane);
            
            // If this was the active pane, set a new active pane
            if (ActivePane == pane)
            {
                var newActivePane = Panes.FirstOrDefault();
                SetActivePane(newActivePane);
            }
            
            // If only one pane remains, disable multi-pane mode
            if (Panes.Count <= 1)
            {
                DisableMultiPaneMode();
            }
            
            _logger.LogDebug("Pane removed: {PaneId}", pane.Id);
        }
    }

    private void OnPaneActiveTabChanged(object? sender, EditorTabViewModel? activeTab)
    {
        if (sender == ActivePane)
        {
            SetActiveTab(activeTab);
        }
    }

    private void OnPaneSplitRequested(object? sender, SplitPaneRequestEventArgs e)
    {
        // Create a new pane and position it relative to the source pane
        var newPane = CreateNewPane();
        
        // Position the new pane based on split direction
        var sourcePosition = e.SourcePane.Position;
        if (e.Direction == SplitDirection.Horizontal)
        {
            newPane.Position = new PanePosition
            {
                Row = sourcePosition.Row + 1,
                Column = sourcePosition.Column,
                RowSpan = sourcePosition.RowSpan,
                ColumnSpan = sourcePosition.ColumnSpan
            };
        }
        else // Vertical
        {
            newPane.Position = new PanePosition
            {
                Row = sourcePosition.Row,
                Column = sourcePosition.Column + 1,
                RowSpan = sourcePosition.RowSpan,
                ColumnSpan = sourcePosition.ColumnSpan
            };
        }
        
        // Add the active tab to the new pane if specified
        if (e.ActiveTab != null)
        {
            newPane.AddTab(e.ActiveTab);
            newPane.SetActiveTab(e.ActiveTab);
        }
        
        SetActivePane(newPane);
        
        _logger.LogDebug("Pane split: {Direction} from {SourcePaneId} to {NewPaneId}", 
            e.Direction, e.SourcePane.Id, newPane.Id);
    }

    private void OnPaneCloseRequested(object? sender, EditorPaneViewModel pane)
    {
        if (Panes.Count > 1) // Don't close the last pane
        {
            RemovePane(pane);
        }
    }
}
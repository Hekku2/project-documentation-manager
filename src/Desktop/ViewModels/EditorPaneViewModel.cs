using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Desktop.Models;

namespace Desktop.ViewModels;

public class EditorPaneViewModel : ViewModelBase
{
    private readonly ILogger<EditorPaneViewModel> _logger;
    private readonly EditorPane _pane;
    private readonly ObservableCollection<EditorTabViewModel> _allTabs;
    private EditorTabViewModel? _activeTab;
    private bool _isActive;

    public EditorPaneViewModel(
        EditorPane pane, 
        ObservableCollection<EditorTabViewModel> allTabs,
        ILogger<EditorPaneViewModel> logger)
    {
        _logger = logger;
        _pane = pane;
        _allTabs = allTabs;
        _isActive = pane.IsActive;

        Tabs = [];
        
        // Initialize with tabs that belong to this pane
        SyncTabsFromModel();
        
        // Subscribe to changes in the all tabs collection
        _allTabs.CollectionChanged += OnAllTabsCollectionChanged;
        
        // Commands
        SplitHorizontalCommand = new RelayCommand(SplitHorizontal);
        SplitVerticalCommand = new RelayCommand(SplitVertical);
        ClosePaneCommand = new RelayCommand(ClosePane, CanClosePane);
    }

    public string Id => _pane.Id;
    
    public ObservableCollection<EditorTabViewModel> Tabs { get; }
    
    public EditorTabViewModel? ActiveTab
    {
        get => _activeTab;
        set
        {
            if (SetProperty(ref _activeTab, value))
            {
                _pane.ActiveTabId = value?.Id;
                ActiveTabChanged?.Invoke(this, value);
            }
        }
    }
    
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (SetProperty(ref _isActive, value))
            {
                _pane.IsActive = value;
            }
        }
    }

    public PanePosition Position
    {
        get => _pane.Position;
        set
        {
            if (_pane.Position != value)
            {
                _pane.Position = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand SplitHorizontalCommand { get; }
    public ICommand SplitVerticalCommand { get; }
    public ICommand ClosePaneCommand { get; }

    public event EventHandler<EditorTabViewModel?>? ActiveTabChanged;
    public event EventHandler<SplitPaneRequestEventArgs>? SplitRequested;
    public event EventHandler<EditorPaneViewModel>? CloseRequested;

    public void AddTab(EditorTabViewModel tab)
    {
        if (!_pane.TabIds.Contains(tab.Id))
        {
            _pane.TabIds.Add(tab.Id);
            Tabs.Add(tab);
            
            if (ActiveTab == null)
            {
                SetActiveTab(tab);
            }
        }
    }

    public void RemoveTab(EditorTabViewModel tab)
    {
        if (_pane.TabIds.Contains(tab.Id))
        {
            _pane.TabIds.Remove(tab.Id);
            Tabs.Remove(tab);
            
            if (ActiveTab == tab)
            {
                ActiveTab = Tabs.FirstOrDefault();
            }
        }
    }

    public void SetActiveTab(EditorTabViewModel tab)
    {
        if (Tabs.Contains(tab))
        {
            ActiveTab = tab;
        }
    }

    private void SyncTabsFromModel()
    {
        Tabs.Clear();
        foreach (var tabId in _pane.TabIds)
        {
            var tab = _allTabs.FirstOrDefault(t => t.Id == tabId);
            if (tab != null)
            {
                Tabs.Add(tab);
            }
        }

        // Set active tab
        if (!string.IsNullOrEmpty(_pane.ActiveTabId))
        {
            var activeTab = Tabs.FirstOrDefault(t => t.Id == _pane.ActiveTabId);
            if (activeTab != null)
            {
                _activeTab = activeTab;
                OnPropertyChanged(nameof(ActiveTab));
            }
        }
    }

    private void OnAllTabsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Handle when tabs are removed from the global collection
        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (EditorTabViewModel removedTab in e.OldItems)
            {
                RemoveTab(removedTab);
            }
        }
    }

    private void SplitHorizontal()
    {
        var args = new SplitPaneRequestEventArgs
        {
            SourcePane = this,
            Direction = SplitDirection.Horizontal,
            ActiveTab = ActiveTab
        };
        SplitRequested?.Invoke(this, args);
    }

    private void SplitVertical()
    {
        var args = new SplitPaneRequestEventArgs
        {
            SourcePane = this,
            Direction = SplitDirection.Vertical,
            ActiveTab = ActiveTab
        };
        SplitRequested?.Invoke(this, args);
    }

    private void ClosePane()
    {
        CloseRequested?.Invoke(this, this);
    }

    private bool CanClosePane()
    {
        // Can close pane if there are other panes available
        return true; // Will be determined by the parent container
    }
}

public class SplitPaneRequestEventArgs : EventArgs
{
    public required EditorPaneViewModel SourcePane { get; set; }
    public required SplitDirection Direction { get; set; }
    public EditorTabViewModel? ActiveTab { get; set; }
}

public enum SplitDirection
{
    Horizontal,
    Vertical
}
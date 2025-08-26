using System;
using System.Collections.Specialized;
using Avalonia.Controls;
using Desktop.ViewModels;

namespace Desktop.Views;

public partial class EditorUserControl : UserControl
{
    private MainWindowViewModel? _viewModel;

    public EditorUserControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.EditorTabBar.Panes.CollectionChanged -= OnPanesCollectionChanged;
        }

        _viewModel = DataContext as MainWindowViewModel;
        
        if (_viewModel != null)
        {
            _viewModel.EditorTabBar.Panes.CollectionChanged += OnPanesCollectionChanged;
            UpdatePanesLayout();
        }
    }

    private void OnPanesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdatePanesLayout();
    }

    private void UpdatePanesLayout()
    {
        if (_viewModel == null) return;

        PanesGrid.Children.Clear();

        foreach (var pane in _viewModel.EditorTabBar.Panes)
        {
            var paneControl = new EditorPaneUserControl
            {
                DataContext = pane
            };
            PanesGrid.Children.Add(paneControl);
        }

        // Adjust grid size based on number of panes
        var paneCount = _viewModel.EditorTabBar.Panes.Count;
        if (paneCount <= 1)
        {
            PanesGrid.Rows = 1;
            PanesGrid.Columns = 1;
        }
        else if (paneCount <= 2)
        {
            PanesGrid.Rows = 1;
            PanesGrid.Columns = 2;
        }
        else if (paneCount <= 4)
        {
            PanesGrid.Rows = 2;
            PanesGrid.Columns = 2;
        }
        else
        {
            // For more panes, use a more complex layout
            var rows = (int)Math.Ceiling(Math.Sqrt(paneCount));
            var cols = (int)Math.Ceiling((double)paneCount / rows);
            PanesGrid.Rows = rows;
            PanesGrid.Columns = cols;
        }
    }
}
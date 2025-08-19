using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Desktop.Logging;

namespace Desktop.Controls;

public partial class ColoredLogDisplay : UserControl
{
    public static readonly StyledProperty<ObservableCollection<LogEntry>?> LogEntriesProperty =
        AvaloniaProperty.Register<ColoredLogDisplay, ObservableCollection<LogEntry>?>(nameof(LogEntries));

    public ObservableCollection<LogEntry>? LogEntries
    {
        get => GetValue(LogEntriesProperty);
        set => SetValue(LogEntriesProperty, value);
    }

    private ObservableCollection<LogDisplayEntry> _displayEntries = new();

    public ColoredLogDisplay()
    {
        InitializeComponent();
        LogItemsControl.ItemsSource = _displayEntries;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == LogEntriesProperty)
        {
            if (change.OldValue is ObservableCollection<LogEntry> oldCollection)
            {
                oldCollection.CollectionChanged -= OnLogEntriesChanged;
            }
            
            if (change.NewValue is ObservableCollection<LogEntry> newCollection)
            {
                newCollection.CollectionChanged += OnLogEntriesChanged;
                UpdateDisplayEntries(newCollection);
            }
        }
    }

    private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (LogEntries != null)
        {
            UpdateDisplayEntries(LogEntries);
        }
    }

    private void UpdateDisplayEntries(ObservableCollection<LogEntry> logEntries)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _displayEntries.Clear();
            foreach (var entry in logEntries)
            {
                _displayEntries.Add(new LogDisplayEntry(entry));
            }
            
            // Auto-scroll to bottom
            LogScrollViewer?.ScrollToEnd();
        });
    }
}
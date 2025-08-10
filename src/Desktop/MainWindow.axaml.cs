
using System.ComponentModel;
using Avalonia.Controls;

namespace Desktop;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private int _clickCount = 0;
    public string ClickCountText => $"button clicked {_clickCount} times";

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        DataContext = this;
        InitializeComponent();
    }

    private void ClickMeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _clickCount++;
        OnPropertyChanged(nameof(ClickCountText));
    }

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
using System;
using System.Windows.Input;
using Desktop.Models;

namespace Desktop.ViewModels;

public class EditorTabViewModel : ViewModelBase
{
    private string? _content;
    private bool _isModified;
    private bool _isActive;

    public EditorTabViewModel(EditorTab tab)
    {
        Tab = tab;
        _content = tab.Content;
        _isModified = tab.IsModified;
        _isActive = tab.IsActive;
        CloseCommand = new RelayCommand(() => CloseRequested?.Invoke(this));
        SelectCommand = new RelayCommand(() => SelectRequested?.Invoke(this));
    }

    public EditorTab Tab { get; }

    public string Id => Tab.Id;
    public string Title => Tab.Title;
    public string? FilePath => Tab.FilePath;
    public TabType TabType => Tab.TabType;

    public string? Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value))
            {
                Tab.Content = value;
                IsModified = true;
            }
        }
    }

    public bool IsModified
    {
        get => _isModified;
        set
        {
            if (SetProperty(ref _isModified, value))
            {
                Tab.IsModified = value;
                OnPropertyChanged(nameof(DisplayTitle));
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
                Tab.IsActive = value;
            }
        }
    }

    public string DisplayTitle => IsModified ? $"{Title} •" : Title;

    public ICommand CloseCommand { get; }
    public ICommand SelectCommand { get; }

    public event Action<EditorTabViewModel>? CloseRequested;
    public event Action<EditorTabViewModel>? SelectRequested;
}
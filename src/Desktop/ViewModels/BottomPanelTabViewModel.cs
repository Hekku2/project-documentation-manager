using System;
using System.Windows.Input;
using Desktop.Models;

namespace Desktop.ViewModels;

public class BottomPanelTabViewModel : ViewModelBase
{
    private string _content = string.Empty;
    private bool _isActive;

    public BottomPanelTabViewModel(BottomPanelTab tab)
    {
        Tab = tab;
        _content = tab.Content;
        _isActive = tab.IsActive;
        CloseCommand = new RelayCommand(() => CloseRequested?.Invoke(this), () => Tab.IsClosable);
        SelectCommand = new RelayCommand(() => SelectRequested?.Invoke(this));
    }

    public BottomPanelTab Tab { get; }

    public string Id => Tab.Id;
    public string Title => Tab.Title;
    public bool IsClosable => Tab.IsClosable;

    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value))
            {
                Tab.Content = value;
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

    public ICommand CloseCommand { get; }
    public ICommand SelectCommand { get; }

    public event Action<BottomPanelTabViewModel>? CloseRequested;
    public event Action<BottomPanelTabViewModel>? SelectRequested;
}
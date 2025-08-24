using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Desktop.Models;

namespace Desktop.Services;

public interface IHotkeyService
{
    void RegisterHotkeys(HotkeySettings settings, Dictionary<string, ICommand> commands);
    void UpdateHotkey(string actionName, string keyGesture, ICommand command);
    bool TryExecuteHotkey(Key key, KeyModifiers modifiers);
    void ApplyToWindow(Window window);
    event EventHandler<HotkeyExecutedEventArgs>? HotkeyExecuted;
}

public class HotkeyExecutedEventArgs : EventArgs
{
    public required string Action { get; set; }
    public required ICommand Command { get; set; }
}
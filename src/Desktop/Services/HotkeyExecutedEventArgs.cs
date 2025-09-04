using System;
using System.Windows.Input;

namespace Desktop.Services;

public class HotkeyExecutedEventArgs : EventArgs
{
    public required string Action { get; set; }
    public required ICommand Command { get; set; }
}

using System;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Desktop.Configuration;

namespace Desktop.ViewModels;

public class SettingsContentViewModel(ILogger<SettingsContentViewModel> logger) : ViewModelBase
{
    public required ApplicationOptions ApplicationOptions { get; init; }

    public ICommand ApplyHotkeyChangesCommand { get; } = new RelayCommand(() =>
    {
        logger.LogInformation("Apply hotkey changes requested from settings content");
        ApplyHotkeyChangesRequested?.Invoke(null, EventArgs.Empty);
    });

    public static event EventHandler? ApplyHotkeyChangesRequested;
}
using System;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desktop.Configuration;
using Desktop.Services;

namespace Desktop.ViewModels;

public class EditorViewModel(
    ILogger<EditorViewModel> logger,
    IOptions<ApplicationOptions> applicationOptions,
    EditorTabBarViewModel editorTabBarViewModel,
    EditorContentViewModel editorContentViewModel,
    IHotkeyService hotkeyService) : ViewModelBase
{
    private readonly ILogger<EditorViewModel> _logger = logger;
    private readonly ApplicationOptions _applicationOptions = applicationOptions.Value;
    private readonly IHotkeyService _hotkeyService = hotkeyService;

    public EditorTabBarViewModel EditorTabBar { get; } = editorTabBarViewModel;
    public EditorContentViewModel EditorContent { get; } = editorContentViewModel;
    public ApplicationOptions ApplicationOptions => _applicationOptions;

    public ICommand ApplyHotkeyChangesCommand { get; } = new RelayCommand(() =>
    {
        logger.LogInformation("Hotkey changes requested from editor");
        ApplyHotkeyChangesRequested?.Invoke(null, EventArgs.Empty);
    });

    public static event EventHandler? ApplyHotkeyChangesRequested;
}
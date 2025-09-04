using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows.Input;
using Desktop.Models;
using Microsoft.Extensions.Logging;

namespace Desktop.Services;

public class HotkeyService(ILogger<HotkeyService> logger) : IHotkeyService
{
    private readonly ConcurrentDictionary<(Key Key, KeyModifiers Modifiers), (string Action, ICommand Command)> _hotkeyMappings = new();
    private readonly ConcurrentDictionary<string, string> _actionToGesture = new();

    public event EventHandler<HotkeyExecutedEventArgs>? HotkeyExecuted;

    public void RegisterHotkeys(HotkeySettings settings, Dictionary<string, ICommand> commands)
    {
        _hotkeyMappings.Clear();
        _actionToGesture.Clear();

        RegisterHotkey("Save", settings.Save, commands.GetValueOrDefault("Save"));
        RegisterHotkey("SaveAll", settings.SaveAll, commands.GetValueOrDefault("SaveAll"));
        RegisterHotkey("BuildDocumentation", settings.BuildDocumentation, commands.GetValueOrDefault("BuildDocumentation"));
    }

    public void UpdateHotkey(string actionName, string keyGesture, ICommand command)
    {
        if (_actionToGesture.TryGetValue(actionName, out var oldGesture) && TryParseKeyGesture(oldGesture, out var oldKey, out var oldModifiers))
        {
            _hotkeyMappings.TryRemove((oldKey, oldModifiers), out _);

        }

        RegisterHotkey(actionName, keyGesture, command);
    }

    public bool TryExecuteHotkey(Key key, KeyModifiers modifiers)
    {
        if (_hotkeyMappings.TryGetValue((key, modifiers), out var mapping))
        {
            try
            {
                if (mapping.Command?.CanExecute(null) == true)
                {
                    mapping.Command.Execute(null);
                    HotkeyExecuted?.Invoke(this, new HotkeyExecutedEventArgs
                    {
                        Action = mapping.Action,
                        Command = mapping.Command
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing hotkey for action {Action}", mapping.Action);
            }
        }
        return false;
    }

    public void ApplyToWindow(Window window)
    {
        window.KeyBindings.Clear();

        foreach (var (keyData, commandData) in _hotkeyMappings)
        {
            var keyGesture = new KeyGesture(keyData.Key, keyData.Modifiers);
            window.KeyBindings.Add(new KeyBinding
            {
                Gesture = keyGesture,
                Command = commandData.Command
            });
        }
    }

    private void RegisterHotkey(string actionName, string keyGesture, ICommand? command)
    {
        if (command == null)
        {
            logger.LogWarning("No command provided for action {Action}", actionName);
            return;
        }

        if (!TryParseKeyGesture(keyGesture, out var key, out var modifiers))
        {
            logger.LogWarning("Invalid key gesture '{KeyGesture}' for action {Action}", keyGesture, actionName);
            return;
        }

        _hotkeyMappings[(key, modifiers)] = (actionName, command);
        _actionToGesture[actionName] = keyGesture;

        logger.LogDebug("Registered hotkey {KeyGesture} for action {Action}", keyGesture, actionName);
    }

    private static bool TryParseKeyGesture(string keyGesture, out Key key, out KeyModifiers modifiers)
    {
        key = Key.None;
        modifiers = KeyModifiers.None;

        if (string.IsNullOrEmpty(keyGesture))
            return false;

        var parts = keyGesture.Split('+');
        if (parts.Length == 0)
            return false;

        // Parse modifiers
        for (int i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].Trim().ToLowerInvariant())
            {
                case "ctrl":
                    modifiers |= KeyModifiers.Control;
                    break;
                case "shift":
                    modifiers |= KeyModifiers.Shift;
                    break;
                case "alt":
                    modifiers |= KeyModifiers.Alt;
                    break;
                case "meta":
                case "cmd":
                    modifiers |= KeyModifiers.Meta;
                    break;
                default:
                    return false;
            }
        }

        // Parse main key
        var keyString = parts[^1].Trim();
        return Enum.TryParse<Key>(keyString, true, out key);
    }
}
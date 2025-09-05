using NSubstitute;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Avalonia.Input;
using System.Windows.Input;
using Desktop.Services;
using Desktop.Models;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;

namespace Desktop.UITests.Services;

[TestFixture]
public class HotkeyServiceTests
{
    private HotkeyService _hotkeyService;
    private ILogger<HotkeyService> _logger;
    private ICommand _mockSaveCommand;
    private ICommand _mockSaveAllCommand;
    private ICommand _mockBuildCommand;

    [SetUp]
    public void Setup()
    {
        _logger = NullLoggerFactory.Instance.CreateLogger<HotkeyService>();
        _hotkeyService = new HotkeyService(_logger);
        _mockSaveCommand = Substitute.For<ICommand>();
        _mockSaveAllCommand = Substitute.For<ICommand>();
        _mockBuildCommand = Substitute.For<ICommand>();
    }

    [Test]
    public void RegisterHotkeys_WithValidSettings_RegistersAllHotkeys()
    {
        // Arrange
        var settings = new HotkeySettings
        {
            Save = "Ctrl+S",
            SaveAll = "Ctrl+Shift+S",
            BuildDocumentation = "Ctrl+B"
        };
        var commands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand,
            ["SaveAll"] = _mockSaveAllCommand,
            ["BuildDocumentation"] = _mockBuildCommand
        };

        // Act
        _hotkeyService.RegisterHotkeys(settings, commands);

        // Assert
        _mockSaveCommand.CanExecute(null).Returns(true);
        _mockSaveAllCommand.CanExecute(null).Returns(true);
        _mockBuildCommand.CanExecute(null).Returns(true);

        Assert.Multiple(() =>
        {
            Assert.That(_hotkeyService.TryExecuteHotkey(Key.S, KeyModifiers.Control), Is.True);
            Assert.That(_hotkeyService.TryExecuteHotkey(Key.S, KeyModifiers.Control | KeyModifiers.Shift), Is.True);
            Assert.That(_hotkeyService.TryExecuteHotkey(Key.B, KeyModifiers.Control), Is.True);
        });
    }

    [Test]
    public void RegisterHotkeys_WithMissingCommands_DoesNotRegisterMissingCommands()
    {
        // Arrange
        var settings = new HotkeySettings
        {
            Save = "Ctrl+S",
            SaveAll = "Ctrl+Shift+S",
            BuildDocumentation = "Ctrl+B"
        };
        var commands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand
            // Missing SaveAll and BuildDocumentation commands
        };

        // Act
        _hotkeyService.RegisterHotkeys(settings, commands);

        // Assert
        _mockSaveCommand.CanExecute(null).Returns(true);

        Assert.Multiple(() =>
        {
            Assert.That(_hotkeyService.TryExecuteHotkey(Key.S, KeyModifiers.Control), Is.True);
            Assert.That(_hotkeyService.TryExecuteHotkey(Key.S, KeyModifiers.Control | KeyModifiers.Shift), Is.False);
            Assert.That(_hotkeyService.TryExecuteHotkey(Key.B, KeyModifiers.Control), Is.False);
        });
    }

    [Test]
    public void TryExecuteHotkey_WithValidRegisteredHotkey_ExecutesCommandAndReturnsTrue()
    {
        // Arrange
        var settings = new HotkeySettings
        {
            Save = "Ctrl+S",
            SaveAll = "Ctrl+Shift+S",
            BuildDocumentation = "Ctrl+B"
        };
        var commands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand
        };
        _hotkeyService.RegisterHotkeys(settings, commands);
        _mockSaveCommand.CanExecute(null).Returns(true);

        // Act
        var result = _hotkeyService.TryExecuteHotkey(Key.S, KeyModifiers.Control);

        // Assert
        Assert.That(result, Is.True);
        _mockSaveCommand.Received(1).Execute(null);
    }

    [Test]
    public void TryExecuteHotkey_WithCommandThatCannotExecute_DoesNotExecuteAndReturnsFalse()
    {
        // Arrange
        var settings = new HotkeySettings
        {
            Save = "Ctrl+S",
            SaveAll = "Ctrl+Shift+S",
            BuildDocumentation = "Ctrl+B"
        };
        var commands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand
        };
        _hotkeyService.RegisterHotkeys(settings, commands);
        _mockSaveCommand.CanExecute(null).Returns(false);

        // Act
        var result = _hotkeyService.TryExecuteHotkey(Key.S, KeyModifiers.Control);

        // Assert
        Assert.That(result, Is.False);
        _mockSaveCommand.DidNotReceive().Execute(Arg.Any<object>());
    }

    [Test]
    public void TryExecuteHotkey_WithUnregisteredHotkey_ReturnsFalse()
    {
        // Arrange
        var settings = new HotkeySettings
        {
            Save = "Ctrl+S",
            SaveAll = "Ctrl+Shift+S",
            BuildDocumentation = "Ctrl+B"
        };
        var commands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand
        };
        _hotkeyService.RegisterHotkeys(settings, commands);

        // Act
        var result = _hotkeyService.TryExecuteHotkey(Key.A, KeyModifiers.Control);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryExecuteHotkey_WhenCommandThrowsException_ReturnsFalse()
    {
        // Arrange
        var settings = new HotkeySettings
        {
            Save = "Ctrl+S",
            SaveAll = "Ctrl+Shift+S",
            BuildDocumentation = "Ctrl+B"
        };
        var commands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand
        };
        _hotkeyService.RegisterHotkeys(settings, commands);
        _mockSaveCommand.CanExecute(null).Returns(true);
        _mockSaveCommand.When(x => x.Execute(null)).Do(x => throw new InvalidOperationException("Test exception"));

        // Act
        var result = _hotkeyService.TryExecuteHotkey(Key.S, KeyModifiers.Control);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void UpdateHotkey_WithExistingAction_UpdatesHotkeyMapping()
    {
        // Arrange
        var settings = new HotkeySettings
        {
            Save = "Ctrl+S",
            SaveAll = "Ctrl+Shift+S",
            BuildDocumentation = "Ctrl+B"
        };
        var commands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand
        };
        _hotkeyService.RegisterHotkeys(settings, commands);
        _mockSaveCommand.CanExecute(null).Returns(true);

        // Act - Update Save hotkey from Ctrl+S to Ctrl+O
        _hotkeyService.UpdateHotkey("Save", "Ctrl+O", _mockSaveCommand);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_hotkeyService.TryExecuteHotkey(Key.S, KeyModifiers.Control), Is.False, "Old hotkey should not work");
            Assert.That(_hotkeyService.TryExecuteHotkey(Key.O, KeyModifiers.Control), Is.True, "New hotkey should work");
        });
    }

    [Test]
    public void UpdateHotkey_WithNewAction_RegistersNewHotkey()
    {
        // Arrange - Empty service
        var newCommand = Substitute.For<ICommand>();
        newCommand.CanExecute(null).Returns(true);

        // Act
        _hotkeyService.UpdateHotkey("NewAction", "Ctrl+N", newCommand);

        // Assert
        Assert.That(_hotkeyService.TryExecuteHotkey(Key.N, KeyModifiers.Control), Is.True);
        newCommand.Received(1).Execute(null);
    }

    [Test]
    public void HotkeyExecuted_WhenHotkeyIsExecuted_EventIsRaised()
    {
        // Arrange
        var settings = new HotkeySettings
        {
            Save = "Ctrl+S",
            SaveAll = "Ctrl+Shift+S",
            BuildDocumentation = "Ctrl+B"
        };
        var commands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand
        };
        _hotkeyService.RegisterHotkeys(settings, commands);
        _mockSaveCommand.CanExecute(null).Returns(true);

        HotkeyExecutedEventArgs? eventArgs = null;
        _hotkeyService.HotkeyExecuted += (sender, args) => eventArgs = args;

        // Act
        _hotkeyService.TryExecuteHotkey(Key.S, KeyModifiers.Control);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs.Action, Is.EqualTo("Save"));
            Assert.That(eventArgs.Command, Is.EqualTo(_mockSaveCommand));
        });
    }

    [AvaloniaTest]
    public void ApplyToWindow_WithRegisteredHotkeys_AddsKeyBindingsToWindow()
    {
        // Arrange
        var window = new Window();
        var settings = new HotkeySettings
        {
            Save = "Ctrl+S",
            SaveAll = "Ctrl+Shift+S",
            BuildDocumentation = "Ctrl+B"
        };
        var commands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand,
            ["SaveAll"] = _mockSaveAllCommand
        };
        _hotkeyService.RegisterHotkeys(settings, commands);

        // Act
        _hotkeyService.ApplyToWindow(window);

        // Assert
        Assert.That(window.KeyBindings, Has.Count.EqualTo(2)); // Only Save and SaveAll have commands
    }

    [AvaloniaTest]
    public void ApplyToWindow_ClearsExistingKeyBindings_BeforeAddingNew()
    {
        // Arrange
        var window = new Window();
        window.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.A, KeyModifiers.Control) });

        var settings = new HotkeySettings
        {
            Save = "Ctrl+S",
            SaveAll = "Ctrl+Shift+S",
            BuildDocumentation = "Ctrl+B"
        };
        var commands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand
        };
        _hotkeyService.RegisterHotkeys(settings, commands);

        // Act
        _hotkeyService.ApplyToWindow(window);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(window.KeyBindings, Has.Count.EqualTo(1)); // Only the new Save binding
            var keyBinding = window.KeyBindings[0];
            Assert.That(keyBinding.Gesture, Is.TypeOf<KeyGesture>());
            var keyGesture = keyBinding.Gesture;
            Assert.That(keyGesture.Key, Is.EqualTo(Key.S));
            Assert.That(keyGesture.KeyModifiers, Is.EqualTo(KeyModifiers.Control));
            Assert.That(ReferenceEquals(keyBinding.Command, _mockSaveCommand), Is.True);
        });
    }

    [TestCase("Ctrl+S", Key.S, KeyModifiers.Control, true)]
    [TestCase("Shift+A", Key.A, KeyModifiers.Shift, true)]
    [TestCase("Alt+F4", Key.F4, KeyModifiers.Alt, true)]
    [TestCase("Meta+C", Key.C, KeyModifiers.Meta, true)]
    [TestCase("Cmd+C", Key.C, KeyModifiers.Meta, true)]
    [TestCase("Ctrl+Shift+S", Key.S, KeyModifiers.Control | KeyModifiers.Shift, true)]
    [TestCase("Ctrl+Alt+Delete", Key.Delete, KeyModifiers.Control | KeyModifiers.Alt, true)]
    [TestCase("", Key.None, KeyModifiers.None, false)]
    [TestCase("InvalidKey", Key.None, KeyModifiers.None, false)]
    [TestCase("Ctrl+", Key.None, KeyModifiers.None, false)]
    [TestCase("+S", Key.None, KeyModifiers.None, false)]
    [TestCase("InvalidModifier+S", Key.None, KeyModifiers.None, false)]
    public void KeyGestureParsing_WithVariousInputs_ParsesCorrectly(string keyGesture, Key expectedKey, KeyModifiers expectedModifiers, bool shouldSucceed)
    {
        // Arrange
        var settings = new HotkeySettings
        {
            Save = keyGesture,
            SaveAll = "Ctrl+Shift+A", // Valid fallback
            BuildDocumentation = "Ctrl+B" // Valid fallback
        };
        var commands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand,
            ["SaveAll"] = _mockSaveAllCommand,
            ["BuildDocumentation"] = _mockBuildCommand
        };

        // Act
        _hotkeyService.RegisterHotkeys(settings, commands);

        if (shouldSucceed)
        {
            _mockSaveCommand.CanExecute(null).Returns(true);
        }

        // Assert
        var result = _hotkeyService.TryExecuteHotkey(expectedKey, expectedModifiers);
        Assert.That(result, Is.EqualTo(shouldSucceed));
    }

    [Test]
    public void RegisterHotkeys_CalledMultipleTimes_ClearsPreviousRegistrations()
    {
        // Arrange
        var firstSettings = new HotkeySettings
        {
            Save = "Ctrl+S",
            SaveAll = "Ctrl+Shift+S",
            BuildDocumentation = "Ctrl+B"
        };
        var firstCommands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand
        };
        _hotkeyService.RegisterHotkeys(firstSettings, firstCommands);

        var secondSettings = new HotkeySettings
        {
            Save = "Ctrl+O",
            SaveAll = "Ctrl+Shift+O",
            BuildDocumentation = "Ctrl+P"
        };
        var secondCommands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand
        };

        // Act
        _hotkeyService.RegisterHotkeys(secondSettings, secondCommands);
        _mockSaveCommand.CanExecute(null).Returns(true);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_hotkeyService.TryExecuteHotkey(Key.S, KeyModifiers.Control), Is.False, "Old hotkey should be cleared");
            Assert.That(_hotkeyService.TryExecuteHotkey(Key.O, KeyModifiers.Control), Is.True, "New hotkey should work");
        });
    }

    [Test]
    public void UpdateHotkey_WithInvalidGesture_KeepsExistingMapping()
    {
        // Arrange
        var settings = new HotkeySettings
        {
            Save = "Ctrl+S",
            SaveAll = "Ctrl+Shift+S",
            BuildDocumentation = "Ctrl+B"
        };
        var commands = new Dictionary<string, ICommand>
        {
            ["Save"] = _mockSaveCommand
        };
        _hotkeyService.RegisterHotkeys(settings, commands);
        _mockSaveCommand.CanExecute(null).Returns(true);

        // Act - attempt invalid update
        _hotkeyService.UpdateHotkey("Save", "Ctrl+", _mockSaveCommand);

        // Assert - old mapping still works
        Assert.That(_hotkeyService.TryExecuteHotkey(Key.S, KeyModifiers.Control), Is.True);
    }
}
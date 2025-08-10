using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Desktop;

namespace Desktop.UITests;

public class MainWindowTests
{
    [AvaloniaTest]
    public void MainWindow_Should_Open()
    {
        var window = new MainWindow();
        window.Show();
        Assert.That(window, Is.Not.Null);
    }

    [AvaloniaTest]
    public void ClickMeButton_Should_Increment_Counter()
    {
        var window = new MainWindow();
        window.Show();

        // Find the button and label
        var button = window.FindControl<Avalonia.Controls.Button>("ClickMeButton");
        var label = window.FindControl<Avalonia.Controls.TextBlock>("ClickCountLabel");

        Assert.That(button, Is.Not.Null, "Button not found");
        Assert.That(label, Is.Not.Null, "Label not found");
        Assert.That(label!.Text, Is.EqualTo("button clicked 0 times"));

        // Simulate button click using Click event
        button!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));

        Assert.That(label.Text, Is.EqualTo("button clicked 1 times"));
    }
}

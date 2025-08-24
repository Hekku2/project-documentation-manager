using Desktop.Models;

namespace Desktop.Configuration;

public class ApplicationOptions
{
    public string DefaultProjectFolder { get; set; } = string.Empty;
    public string DefaultOutputFolder { get; set; } = "output";
    public HotkeySettings Hotkeys { get; set; } = new()
    {
        Save = "Ctrl+S",
        SaveAll = "Ctrl+Shift+S", 
        BuildDocumentation = "F6"
    };
}
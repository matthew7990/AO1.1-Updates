using System.IO;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>Fuentes de login — VB6 usa MS Sans Serif; bitmap ingame es otro sistema.</summary>
public static class AoUiFonts
{
    private static Font? _ui;

    public static Font Ui => _ui ??= Load();

    public static Font Title => Ui;

    private static Font Load()
    {
        foreach (var path in new[]
                 {
                     @"C:\Windows\Fonts\tahoma.ttf",
                     @"C:\Windows\Fonts\micross.ttf",
                     @"C:\Windows\Fonts\MS Sans Serif.ttf",
                 })
        {
            if (!File.Exists(path))
            {
                continue;
            }
            var font = new FontFile();
            if (font.LoadDynamicFont(path) != Error.Ok)
            {
                continue;
            }
            font.Antialiasing = TextServer.FontAntialiasing.Gray;
            font.Hinting = TextServer.Hinting.Light;
            font.GenerateMipmaps = false;
            return font;
        }

        return new SystemFont
        {
            FontNames = ["Tahoma", "MS Sans Serif", "Arial"],
            Antialiasing = TextServer.FontAntialiasing.Gray,
            Hinting = TextServer.Hinting.Light,
        };
    }
}

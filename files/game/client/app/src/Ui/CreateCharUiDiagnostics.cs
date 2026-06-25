using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.Ui;

internal static class CreateCharUiDiagnostics
{
    private static bool _logged;

    public static void LogOnce(GameResources? resources, InterfaceLoader? ui)
    {
        if (_logged)
        {
            return;
        }
        _logged = true;

        LogGrh(resources, 1169, "marco pantalla");
        LogGrh(resources, 727, "panel crear PJ");
        LogGrh(resources, 1171, "logo crear PJ");
        LogPreviewBody(resources, 1, "cuerpo preview CPBody");
        LogPreviewBody(resources, 52, "cuerpo preview CPBodyE");

        LogBmp(ui, "boton-volver-flecha");
        LogBmp(ui, "boton-crear-pj");
        LogBmp(ui, "boton-crear-personaje");
    }

    public static void Reset() => _logged = false;

    private static void LogPreviewBody(GameResources? resources, int bodyId, string label)
    {
        if (resources is null)
        {
            return;
        }
        var body = resources.Bodies.Get(bodyId);
        var slice = resources.Bodies.GetWalkFrame(bodyId, 3, moving: false, walkAnimTime: 0);
        if (body is null || slice is not { } frame)
        {
            GD.PrintErr($"[CrearPJ] {label}: body {bodyId} sin frames (body={body is not null} slice={slice is not null})");
            return;
        }
        var tex = frame.UsesGrh
            ? resources.Textures.Get(resources.Grhs.Get(frame.GrhIndex)?.FileNum ?? 0)
            : resources.Textures.Get(frame.FileNum);
        GD.Print(
            $"[CrearPJ] {label}: body={bodyId} file={frame.FileNum} grh={frame.GrhIndex} " +
            $"size={frame.Width}x{frame.Height} texture={(tex is null ? "FALTA" : "OK")}");
    }

    private static void LogGrh(GameResources? resources, int index, string label)
    {
        if (resources is null)
        {
            GD.PrintErr($"[CrearPJ] {label}: sin GameResources");
            return;
        }
        var def = resources.Grhs.Get(index);
        if (def is null)
        {
            GD.PrintErr($"[CrearPJ] {label}: grh {index} no existe");
            return;
        }
        if (def.FileNum <= 0)
        {
            GD.PrintErr($"[CrearPJ] {label}: grh {index} sin FileNum");
            return;
        }
        var tex = resources.Textures.Get(def.FileNum);
        GD.Print(
            $"[CrearPJ] {label}: grh {index} file={def.FileNum} " +
            $"src=({def.SX},{def.SY}) size={def.PixelWidth}x{def.PixelHeight} " +
            $"texture={(tex is null ? "FALTA" : "OK")}");
    }

    private static void LogBmp(InterfaceLoader? ui, string baseName)
    {
        if (ui is null)
        {
            GD.PrintErr($"[CrearPJ] bmp {baseName}: sin InterfaceLoader");
            return;
        }
        var normal = ui.LoadSpanish($"{baseName}-default.bmp");
        var over = ui.LoadSpanish($"{baseName}-over.bmp");
        var off = ui.LoadSpanish($"{baseName}-off.bmp");
        GD.Print(
            $"[CrearPJ] bmp {baseName}: default={(normal is null ? "FALTA" : normal.GetSize())} " +
            $"over={(over is null ? "FALTA" : over.GetSize())} off={(off is null ? "FALTA" : off.GetSize())}");
    }
}

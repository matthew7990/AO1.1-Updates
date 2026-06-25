using System;
using Argentum.Client.Resources;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>VB6 RenderUICrearPJ: Draw_Grh en ancla de pantalla (no Char_Render 256×256).</summary>
internal static class LoginPortraitDrawer
{
    private const int PreviewBodyId = 1;
    private const int PreviewShortBodyId = 52;
    private const int HeadOffsetBodyId = 1;

    public static void Draw(
        CanvasItem canvas,
        GameResources resources,
        int raceIndex,
        int headId,
        int heading,
        Vector2 anchor,
        float pixelScale = 1f)
    {
        heading = Math.Clamp(heading, 1, 4);
        var enanoOffset = GetEnanoOffset(raceIndex);
        var drawBodyId = enanoOffset > 0 ? PreviewShortBodyId : PreviewBodyId;

        var drawBody = resources.Bodies.Get(drawBodyId);
        var headOffsetBody = resources.Bodies.Get(HeadOffsetBodyId);
        var slice = resources.Bodies.GetWalkFrame(drawBodyId, heading, moving: false, walkAnimTime: 0);

        if (drawBody is null || headOffsetBody is null || slice is not { } frame)
        {
            GD.PrintErr(
                $"[CrearPJ] preview body falló · race={raceIndex} body={drawBodyId} heading={heading} " +
                $"drawBody={drawBody is not null} headBody={headOffsetBody is not null} slice={slice is not null}");
            DrawMissing(canvas, anchor, pixelScale, new Color(0.85f, 0.35f, 0.35f));
            return;
        }

        var bodyDest = ScaleRect(SliceDestAtAnchor(anchor.X, anchor.Y, frame.Width, frame.Height), anchor, pixelScale);
        if (!DrawSlice(canvas, resources, frame, bodyDest))
        {
            GD.PrintErr(
                $"[CrearPJ] textura cuerpo falló · body={drawBodyId} file={frame.FileNum} grh={frame.GrhIndex}");
            DrawMissing(canvas, anchor, pixelScale, new Color(0.85f, 0.55f, 0.2f));
        }

        var headGrh = resources.Heads.GetGrh(headId, heading);
        if (headGrh <= 0)
        {
            GD.PrintErr($"[CrearPJ] cabeza sin grh · head={headId} heading={heading}");
            return;
        }

        var headDef = resources.Grhs.ResolveDrawable(headGrh);
        if (headDef is null)
        {
            GD.PrintErr($"[CrearPJ] grh cabeza inválido · head={headId} grh={headGrh}");
            return;
        }

        var headY = anchor.Y + headOffsetBody.HeadOffsetY + enanoOffset;
        var headDest = ScaleRect(GrhDestAtAnchor(anchor.X, headY, headDef), anchor, pixelScale);
        if (!DrawGrhDef(canvas, resources, headDef, headDest))
        {
            GD.PrintErr($"[CrearPJ] textura cabeza falló · head={headId} grh={headGrh} file={headDef.FileNum}");
        }
    }

    /// <summary>Slot de cuenta — ancla de pie con SliceDestAtAnchor/GrhDestAtAnchor (cabeza alineada).</summary>
    public static void DrawCharacterSlot(
        CanvasItem canvas,
        GameResources resources,
        int bodyId,
        int headId,
        int heading,
        AoUiScale scale,
        float slotX,
        float slotY)
    {
        // Ajuste fino del slot de selección: más arriba y más a la izquierda para calzar mejor en el marco.
        var anchor = scale.MapPoint(slotX + 20f, slotY + 22f);
        DrawCharacter(canvas, resources, bodyId, headId, heading, anchor);
    }

    /// <summary>Retratos en pantalla de personajes (usa body real del PJ).</summary>
    public static void DrawCharacter(
        CanvasItem canvas,
        GameResources resources,
        int bodyId,
        int headId,
        int heading,
        Vector2 anchor,
        float pixelScale = 1f)
    {
        heading = Math.Clamp(heading, 1, 4);
        var body = resources.Bodies.Get(bodyId);
        var slice = resources.Bodies.GetWalkFrame(bodyId, heading, moving: false, walkAnimTime: 0);
        if (body is null || slice is not { } frame)
        {
            DrawMissing(canvas, anchor, pixelScale, new Color(0.4f, 0.6f, 0.9f));
            return;
        }

        var bodyDest = ScaleRect(SliceDestAtAnchor(anchor.X, anchor.Y, frame.Width, frame.Height), anchor, pixelScale);
        DrawSlice(canvas, resources, frame, bodyDest);

        var headGrh = resources.Heads.GetGrh(headId, heading);
        if (headGrh <= 0)
        {
            return;
        }
        var headDef = resources.Grhs.ResolveDrawable(headGrh);
        if (headDef is null)
        {
            return;
        }
        var headY = anchor.Y + body.HeadOffsetY;
        var headDest = ScaleRect(GrhDestAtAnchor(anchor.X, headY, headDef), anchor, pixelScale);
        DrawGrhDef(canvas, resources, headDef, headDest);
    }

    public static int GetEnanoOffset(int raceIndex) =>
        raceIndex is 4 or 5 ? 10 : 0;

    private static void DrawMissing(CanvasItem canvas, Vector2 anchor, float scale, Color color)
    {
        var size = 32f * scale;
        canvas.DrawRect(new Rect2(anchor - new Vector2(size / 2f, size), new Vector2(size, size)), color);
    }

    private static Rect2 ScaleRect(Rect2 rect, Vector2 anchor, float scale)
    {
        if (Math.Abs(scale - 1f) < 0.01f)
        {
            return SnapRect(rect);
        }
        var center = rect.GetCenter();
        var offset = center - anchor;
        var scaled = anchor + offset * scale;
        return SnapRect(new Rect2(scaled - rect.Size * scale / 2f, rect.Size * scale));
    }

    private static Rect2 SnapRect(Rect2 rect) =>
        new(MathF.Round(rect.Position.X), MathF.Round(rect.Position.Y),
            MathF.Round(rect.Size.X), MathF.Round(rect.Size.Y));

    private static Rect2 SliceDestAtAnchor(float anchorX, float anchorY, int width, int height)
    {
        var x = anchorX;
        var y = anchorY;
        var tileWidth = width / (float)CsmMap.TilePixels;
        var tileHeight = height / (float)CsmMap.TilePixels;
        if (tileWidth != 1f)
        {
            x += (CsmMap.TilePixels - width) / 2f;
        }
        if (tileHeight != 1f)
        {
            y -= (int)(tileHeight * CsmMap.TilePixels) - CsmMap.TilePixels;
        }
        return new Rect2(x, y, width, height);
    }

    private static Rect2 GrhDestAtAnchor(float anchorX, float anchorY, GrhDef def)
    {
        var x = anchorX;
        var y = anchorY;
        if (def.TileWidth != 1f)
        {
            x -= (int)(def.TileWidth * CsmMap.TilePixels / 2f) - CsmMap.TilePixels / 2f;
        }
        if (def.TileHeight != 1f)
        {
            y -= (int)(def.TileHeight * CsmMap.TilePixels) - CsmMap.TilePixels;
        }
        return new Rect2(x, y, def.PixelWidth, def.PixelHeight);
    }

    private static bool DrawSlice(CanvasItem canvas, GameResources resources, SpriteSlice slice, Rect2 dest)
    {
        if (slice.UsesGrh)
        {
            return DrawGrh(canvas, resources, slice.GrhIndex, dest);
        }
        var texture = resources.Textures.Get(slice.FileNum);
        if (texture is null)
        {
            return false;
        }
        canvas.DrawTextureRectRegion(texture, dest, new Rect2(slice.SX, slice.SY, slice.Width, slice.Height));
        return true;
    }

    private static bool DrawGrh(CanvasItem canvas, GameResources resources, int grhIndex, Rect2 dest)
    {
        var def = resources.Grhs.ResolveDrawable(grhIndex);
        return def is not null && DrawGrhDef(canvas, resources, def, dest);
    }

    private static bool DrawGrhDef(CanvasItem canvas, GameResources resources, GrhDef def, Rect2 dest)
    {
        if (def.FileNum <= 0)
        {
            return false;
        }
        var texture = resources.Textures.Get(def.FileNum);
        if (texture is null)
        {
            return false;
        }
        canvas.DrawTextureRectRegion(texture, dest, new Rect2(def.SX, def.SY, def.PixelWidth, def.PixelHeight));
        return true;
    }
}

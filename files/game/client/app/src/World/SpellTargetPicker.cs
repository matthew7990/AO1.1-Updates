using System;
using Argentum.Client.Core;
using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.World;

/// <summary>Apunta hechizos al personaje/NPC bajo el cursor (sprite), no solo al tile del grid.</summary>
public static class SpellTargetPicker
{
    public static bool TryResolveTile(
        WorldSession world,
        GameResources? resources,
        Vector2 screenPos,
        Vector2 viewportSize,
        out int tileX,
        out int tileY)
    {
        tileX = 0;
        tileY = 0;
        if (world.Map is null)
        {
            return false;
        }
        var camera = WorldCamera.Create(world, viewportSize);
        WorldCharacter? best = null;
        var bestArea = float.MaxValue;
        var tick = (int)Time.GetTicksMsec();
        foreach (var ch in world.Characters.All)
        {
            if (!TryCharacterScreenRect(resources, camera, ch, tick, out var rect))
            {
                continue;
            }
            if (!rect.HasPoint(screenPos))
            {
                continue;
            }
            var area = rect.Size.X * rect.Size.Y;
            if (area >= bestArea)
            {
                continue;
            }
            bestArea = area;
            best = ch;
        }
        if (best is not null)
        {
            tileX = best.TileX;
            tileY = best.TileY;
            return true;
        }
        return camera.TryScreenToTile(screenPos, out tileX, out tileY);
    }

    private static bool TryCharacterScreenRect(
        GameResources? resources,
        WorldCamera camera,
        WorldCharacter ch,
        int tick,
        out Rect2 rect)
    {
        rect = default;
        if (resources is null)
        {
            return false;
        }
        var pixelX = camera.TileToScreenX(ch.TileX, buffered: true) + ch.Motion.MoveOffsetX;
        var pixelY = camera.TileToScreenY(ch.TileY, buffered: true) + ch.Motion.MoveOffsetY;
        var heading = Math.Clamp(ch.Heading, 1, 4);
        var bodyDef = resources.Bodies.Get(ch.Body);
        var bodyFrame = resources.Bodies.GetWalkFrame(ch.Body, heading, ch.Motion.IsMoving, ch.Motion.WalkAnimTime);
        if (bodyDef is null || bodyFrame is not { } slice)
        {
            rect = new Rect2(pixelX, pixelY, CsmMap.TilePixels, CsmMap.TilePixels * 2);
            return true;
        }
        if (slice.UsesGrh)
        {
            var grhDef = resources.Grhs.ResolveDrawable(slice.GrhIndex, tick);
            if (grhDef is null)
            {
                return false;
            }
            rect = CharacterSpriteLayout.BodyScreenRect(pixelX, pixelY, bodyDef, grhDef);
        }
        else
        {
            rect = CharacterSpriteLayout.BodyScreenRect(pixelX, pixelY, bodyDef, slice);
        }
        return true;
    }
}

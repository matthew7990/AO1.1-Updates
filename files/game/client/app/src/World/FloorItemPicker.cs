using Argentum.Client.Core;
using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.World;

/// <summary>Detecta ítems dropeados en el piso (VB6: tile bajo el mouse + OBJInfo).</summary>
public static class FloorItemPicker
{
    public readonly struct Hit
    {
        public int TileX { get; init; }
        public int TileY { get; init; }
        public int ObjectIndex { get; init; }
        public int Amount { get; init; }
        public int ElementalTags { get; init; }
        public Vector2 AnchorScreen { get; init; }
    }

    public static bool TryPick(WorldSession world, GameResources? resources, Vector2 screenPos, Vector2 viewportSize, out Hit hit)
    {
        hit = default;
        if (world.Map is null || resources?.Items is null)
        {
            return false;
        }
        var camera = WorldCamera.Create(world, viewportSize);
        if (!camera.TryScreenToTile(screenPos, out var tileX, out var tileY))
        {
            return false;
        }
        if (tileX < CsmMap.MinMapTile || tileX > CsmMap.MaxMapTile
            || tileY < CsmMap.MinMapTile || tileY > CsmMap.MaxMapTile)
        {
            return false;
        }
        var tile = world.Map.Tiles[tileX, tileY];
        if (!tile.ObjectIsDroppedItem || tile.ObjectIndex <= 0)
        {
            return false;
        }
        var item = resources.Items.Get(tile.ObjectIndex);
        if (item is null || item.Agarrable != 0)
        {
            return false;
        }
        var tileXScreen = camera.TileToScreenX(tileX, buffered: true);
        var tileYScreen = camera.TileToScreenY(tileY, buffered: true);
        var tileRect = new Rect2(tileXScreen, tileYScreen, CsmMap.TilePixels, CsmMap.TilePixels);
        if (!tileRect.HasPoint(screenPos))
        {
            return false;
        }
        hit = new Hit
        {
            TileX = tileX,
            TileY = tileY,
            ObjectIndex = tile.ObjectIndex,
            Amount = tile.ObjectAmount,
            ElementalTags = tile.ObjectElementalTags,
            AnchorScreen = new Vector2(tileXScreen + CsmMap.TilePixels * 0.5f, tileYScreen),
        };
        return true;
    }
}

using System.Collections.Generic;
using System.Linq;

namespace Argentum.Client.World;

/// <summary>VB6 charlist — personajes visibles en pantalla (excluye el índice del jugador local).</summary>
public sealed class WorldCharacters
{
    public const int AreaDim = 12;

    private readonly Dictionary<short, WorldCharacter> _chars = new();

    public IEnumerable<WorldCharacter> All => _chars.Values;

    public WorldCharacter GetOrCreate(short charIndex)
    {
        if (!_chars.TryGetValue(charIndex, out var ch))
        {
            ch = new WorldCharacter { CharIndex = charIndex };
            _chars[charIndex] = ch;
        }
        return ch;
    }

    public void Upsert(WorldCharacter character) => _chars[character.CharIndex] = character;

    public bool TryGet(short charIndex, out WorldCharacter? character) =>
        _chars.TryGetValue(charIndex, out character);

    public void Remove(short charIndex) => _chars.Remove(charIndex);

    public void Clear() => _chars.Clear();

    public short CharIndexAt(int tileX, int tileY)
    {
        foreach (var ch in _chars.Values)
        {
            if (ch.TileX == tileX && ch.TileY == tileY)
            {
                return ch.CharIndex;
            }
        }
        return 0;
    }

    /// <summary>VB6 ModAreas.CambioDeArea — borra chars fuera del rectángulo visible.</summary>
    public void PruneOutsideArea(int centerX, int centerY)
    {
        var minX = (centerX / AreaDim - 1) * AreaDim;
        var maxX = minX + AreaDim * 3 - 1;
        var minY = (centerY / AreaDim - 1) * AreaDim;
        var maxY = minY + AreaDim * 3 - 1;
        foreach (var key in _chars.Keys.Where(k => _chars[k].TileX < minX || _chars[k].TileX > maxX
                                                   || _chars[k].TileY < minY || _chars[k].TileY > maxY).ToList())
        {
            _chars.Remove(key);
        }
    }
}

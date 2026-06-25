namespace Argentum.Client.World;

/// <summary>VB6 eTrigger + PRIMER_TRIGGER_TECHO (Declares.bas).</summary>
public static class MapTrigger
{
    public const int BajoTecho = 1;
    public const int ZonaSegura = 4;
    public const int NadoBajoTecho = 16;
    public const int PrimerTriggerTecho = 19;

    public static bool HayTecho(short trigger) =>
        trigger >= PrimerTriggerTecho
        || trigger is BajoTecho or ZonaSegura or NadoBajoTecho;

    /// <summary>VB6 TileEngine.NearRoof — trigger de techo en el vecindario 3×3.</summary>
    public static short NearRoof(CsmMap map, int x, int y)
    {
        for (var ly = y - 1; ly <= y + 1; ly++)
        {
            for (var lx = x - 1; lx <= x + 1; lx++)
            {
                if (lx < CsmMap.MinMapTile || lx > CsmMap.MaxMapTile
                    || ly < CsmMap.MinMapTile || ly > CsmMap.MaxMapTile)
                {
                    continue;
                }
                var trigger = map.Tiles[lx, ly].Trigger;
                if (HayTecho(trigger))
                {
                    return trigger;
                }
            }
        }
        return 0;
    }
}

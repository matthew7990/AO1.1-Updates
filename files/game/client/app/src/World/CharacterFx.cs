namespace Argentum.Client.World;

/// <summary>VB6 charlist[].FxList active FX on character.</summary>
public sealed class CharacterFx
{
    public int FxIndex { get; set; }
    public int Loops { get; set; }
    public long StartedMs { get; set; }

    public bool IsActive => FxIndex > 0;

    public void Set(int fxIndex, int loops, long nowMs)
    {
        FxIndex = fxIndex;
        Loops = loops;
        StartedMs = nowMs;
    }

    public void Clear() => FxIndex = 0;
}

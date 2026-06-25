namespace Argentum.Client.World;

/// <summary>VB6 HotkeyList[1..10] — teclas 1-0 apuntan a slots del libro de hechizos (1..40).</summary>
public sealed class SpellHotbar
{
    public const int SlotCount = 10;

    private readonly int[] _spellBookSlot = new int[SlotCount + 1];

    public int GetSpellBookSlot(int hotkey)
    {
        if (hotkey < 1 || hotkey > SlotCount)
        {
            return 0;
        }
        return _spellBookSlot[hotkey];
    }

    public void Assign(int hotkey, int spellBookSlot)
    {
        if (hotkey < 1 || hotkey > SlotCount)
        {
            return;
        }
        _spellBookSlot[hotkey] = spellBookSlot is >= 1 and <= SpellBook.MaxSlots ? spellBookSlot : 0;
    }

    public static string HotkeyLabel(int hotkey) => hotkey == 10 ? "0" : hotkey.ToString();
}

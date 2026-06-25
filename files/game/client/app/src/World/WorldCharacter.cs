namespace Argentum.Client.World;



/// <summary>VB6 charlist() entry — jugador u otro personaje/NPC visible.</summary>

public sealed class WorldCharacter

{

    public short CharIndex { get; set; }

    public int TileX { get; set; }

    public int TileY { get; set; }

    public int Body { get; set; }

    public int Head { get; set; }

    public int Weapon { get; set; }

    public int Shield { get; set; }

    public int Helmet { get; set; }

    public int Heading { get; set; } = 3;

    public bool IsNpc { get; set; }

    public int NpcNumber { get; set; }

    public string Name { get; set; } = "";
    public int Privilege { get; set; }

    public int MinHp { get; set; }

    public int MaxHp { get; set; }

    public int ShieldHp { get; set; }

    public bool Invisible { get; set; }

    public bool StepPhase { get; set; }

    public CharacterMotion Motion { get; } = new();
    public CharacterFx Fx { get; } = new();



    public void ConfirmMove(int newX, int newY)

    {

        if (newX == TileX && newY == TileY)

        {

            return;

        }

        var oldX = TileX;

        var oldY = TileY;

        if (System.Math.Abs(newX - oldX) > 1 || System.Math.Abs(newY - oldY) > 1)

        {

            TileX = newX;

            TileY = newY;

            Motion.Reset();

            return;

        }

        if (!Motion.IsMoving)

        {

            Motion.BeginStep(oldX, oldY, newX, newY);

        }

        TileX = newX;

        TileY = newY;

        UpdateHeadingFromDelta(newX - oldX, newY - oldY);

    }



    public void ApplyHpUpdate(int minHp, int maxHp, int shield)

    {

        MinHp = minHp;

        MaxHp = maxHp;

        ShieldHp = shield;

    }



    private void UpdateHeadingFromDelta(int dx, int dy)

    {

        if (dx > 0)

        {

            Heading = 2;

        }

        else if (dx < 0)

        {

            Heading = 4;

        }

        else if (dy > 0)

        {

            Heading = 3;

        }

        else if (dy < 0)

        {

            Heading = 1;

        }

    }

}


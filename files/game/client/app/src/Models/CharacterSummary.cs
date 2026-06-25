namespace Argentum.Client.Models;

public sealed class CharacterSummary
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public int Body { get; init; }
    public int Head { get; init; }
    public int Class { get; init; }
    public int Map { get; init; }
    public int PosX { get; init; }
    public int PosY { get; init; }
    public int Level { get; init; }
    public int Status { get; init; }
    public int Helmet { get; init; }
    public int Shield { get; init; }
    public int Weapon { get; init; }
    public int Backpack { get; init; }
}

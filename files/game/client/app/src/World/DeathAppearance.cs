using Argentum.Client.Network;

namespace Argentum.Client.World;

/// <summary>VB6 engine.bas: si .Muerto, dibujar casper aunque el body del paquete no haya llegado.</summary>
public static class DeathAppearance
{
    public static void Resolve(bool isDead, int body, int head, int weapon, int shield, int helmet,
        out int drawBody, out int drawHead, out int drawWeapon, out int drawShield, out int drawHelmet)
    {
        if (!isDead)
        {
            drawBody = body;
            drawHead = head;
            drawWeapon = weapon;
            drawShield = shield;
            drawHelmet = helmet;
            return;
        }
        drawBody = CharacterChangeReader.CasperBodyIdle;
        drawHead = 0;
        drawWeapon = 0;
        drawShield = 0;
        drawHelmet = 0;
    }
}

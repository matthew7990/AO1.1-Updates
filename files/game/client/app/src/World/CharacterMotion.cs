using System;
using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.World;

/// <summary>
/// VB6 TileEngine.MoveScreen + Char_Move_by_Head + ShowNextFrame:
/// UserPos/char.Pos ya están en el tile destino; AddtoUserPos desplaza la cámara;
/// MoveOffset desplaza el sprite desde -32·dir hasta 0; OffsetCounter desplaza el mapa.
/// </summary>
public sealed class CharacterMotion
{
    public const float ScrollPixelsPerFrame = 8.5f;
    public const float EngineBaseSpeed = 0.018f;

    /// <summary>VB6 AddtoUserPos — delta de tile pendiente en la cámara.</summary>
    public int PendingCameraX { get; private set; }
    public int PendingCameraY { get; private set; }

    /// <summary>VB6 OffsetCounterX/Y.</summary>
    public float CameraOffsetX { get; private set; }
    public float CameraOffsetY { get; private set; }

    /// <summary>VB6 MoveOffsetX/Y (arranca en -32·dir).</summary>
    public float MoveOffsetX { get; private set; }
    public float MoveOffsetY { get; private set; }

    public bool IsCameraScrolling => PendingCameraX != 0 || PendingCameraY != 0;

    public bool IsMoving => IsCameraScrolling || _scrollDirX != 0 || _scrollDirY != 0;
    public double WalkAnimTime { get; private set; }

    public int RenderCenterX(int tileX) => tileX - PendingCameraX;
    public int RenderCenterY(int tileY) => tileY - PendingCameraY;

    /// <summary>VB6 scrollDirectionX/Y.</summary>
    private int _scrollDirX;
    private int _scrollDirY;

    public void BeginStep(int fromX, int fromY, int toX, int toY)
    {
        var dx = toX - fromX;
        var dy = toY - fromY;
        if (dx == 0 && dy == 0)
        {
            return;
        }

        MovementDiagnostics.Log("MOTION_BEGIN",
            $"({fromX},{fromY})->({toX},{toY}) wasMoving={IsMoving} wasCam={IsCameraScrolling}");

        var wasAnimating = IsMoving;
        var wasCameraScrolling = IsCameraScrolling;
        PendingCameraX = dx;
        PendingCameraY = dy;
        _scrollDirX = dx;
        _scrollDirY = dy;
        MoveOffsetX = -CsmMap.TilePixels * dx;
        MoveOffsetY = -CsmMap.TilePixels * dy;
        WalkAnimTime = wasAnimating ? WalkAnimTime : 0;
        MovArmaEscudo = false;

        // VB6: OffsetCounter estático; solo se reinicia si la cámara no estaba scrolleando (UserMoving era false).
        if (!wasCameraScrolling)
        {
            CameraOffsetX = 0;
            CameraOffsetY = 0;
        }
    }

    /// <summary>Fila Y del mapa donde dibujar el cuerpo (VB6 orden por tile de origen al caminar).</summary>
    public int CharacterSortRow(int logicalTileY)
    {
        if (!IsMoving)
        {
            return logicalTileY;
        }
        if (PendingCameraY < 0)
        {
            return logicalTileY - PendingCameraY;
        }
        return logicalTileY;
    }

    public void Advance(double deltaSeconds)
    {
        if (!IsMoving)
        {
            return;
        }

        var timerTicksPerFrame = (float)(deltaSeconds * 1000.0 * EngineBaseSpeed);
        WalkAnimTime += deltaSeconds;

        if (PendingCameraX != 0)
        {
            CameraOffsetX = AdvanceCameraOffset(CameraOffsetX, PendingCameraX, timerTicksPerFrame);
            if (CameraOffsetX == 0)
            {
                PendingCameraX = 0;
            }
        }

        if (PendingCameraY != 0)
        {
            CameraOffsetY = AdvanceCameraOffset(CameraOffsetY, PendingCameraY, timerTicksPerFrame);
            if (CameraOffsetY == 0)
            {
                PendingCameraY = 0;
            }
        }

        if (_scrollDirX != 0)
        {
            MoveOffsetX = AdvanceMoveOffset(MoveOffsetX, _scrollDirX, timerTicksPerFrame);
            if (MoveOffsetX == 0)
            {
                _scrollDirX = 0;
            }
        }

        if (_scrollDirY != 0)
        {
            MoveOffsetY = AdvanceMoveOffset(MoveOffsetY, _scrollDirY, timerTicksPerFrame);
            if (MoveOffsetY == 0)
            {
                _scrollDirY = 0;
            }
        }
    }

    /// <summary>VB6 ShowNextFrame: OffsetCounter -= Scroll * AddtoUserPos * ttpf.</summary>
    private static float AdvanceCameraOffset(float offset, int addToUserPos, float timerTicksPerFrame)
    {
        var next = offset - ScrollPixelsPerFrame * addToUserPos * timerTicksPerFrame;
        var limit = -CsmMap.TilePixels * addToUserPos;
        if (addToUserPos > 0 && next <= limit)
        {
            return 0;
        }
        if (addToUserPos < 0 && next >= limit)
        {
            return 0;
        }
        return next;
    }

    /// <summary>VB6 Char_Render: MoveOffset += Scroll * Sgn(scrollDirection) * ttpf hasta cruzar 0.</summary>
    private static float AdvanceMoveOffset(float offset, int scrollDirection, float timerTicksPerFrame)
    {
        if (scrollDirection == 0)
        {
            return 0;
        }
        var next = offset + ScrollPixelsPerFrame * Math.Sign(scrollDirection) * timerTicksPerFrame;
        if (scrollDirection > 0 && next >= 0)
        {
            return 0;
        }
        if (scrollDirection < 0 && next <= 0)
        {
            return 0;
        }
        return next;
    }

    public void Reset()
    {
        if (IsMoving)
        {
            MovementDiagnostics.Log("MOTION_RESET", MovementDiagnostics.DescribeState(this));
        }
        PendingCameraX = 0;
        PendingCameraY = 0;
        _scrollDirX = 0;
        _scrollDirY = 0;
        CameraOffsetX = 0;
        CameraOffsetY = 0;
        MoveOffsetX = 0;
        MoveOffsetY = 0;
        WalkAnimTime = 0;
        MovArmaEscudo = false;
        AttackAnimStartMs = 0;
    }
    public bool MovArmaEscudo { get; private set; }
    public int AttackAnimStartMs { get; private set; }

    /// <summary>VB6 HandleArmaMov — reinicia anim GRH del arma/escudo (Loops=0).</summary>
    public void TriggerWeaponShieldAttack()
    {
        if (IsMoving)
        {
            return;
        }
        MovArmaEscudo = true;
        AttackAnimStartMs = (int)Time.GetTicksMsec();
    }

    /// <summary>Tick para ResolveDrawable de arma/escudo (caminar = loop; ataque = one-shot).</summary>
    public int GetDirectionalAnimTick(int grhIndex, GrhCatalog? grhs, bool moving, int nowMs)
    {
        if (grhIndex <= 0 || grhs is null)
        {
            return 0;
        }
        var def = grhs.Get(grhIndex);
        if (def is null || !def.Animated)
        {
            return 0;
        }
        if (moving)
        {
            MovArmaEscudo = false;
            return (int)(WalkAnimTime * 1000.0);
        }
        if (!MovArmaEscudo)
        {
            return 0;
        }
        var elapsed = nowMs - AttackAnimStartMs;
        var durationMs = Math.Max(grhs.GetAnimationDurationMs(grhIndex), 1);
        if (elapsed >= durationMs)
        {
            MovArmaEscudo = false;
            return 0;
        }
        return AttackAnimStartMs + elapsed;
    }

    public void AdvanceWeaponAnim(int nowMs, GrhCatalog? grhs, int weaponGrh, int shieldGrh)
    {
        if (!MovArmaEscudo || grhs is null)
        {
            return;
        }
        _ = GetDirectionalAnimTick(weaponGrh, grhs, false, nowMs);
        _ = GetDirectionalAnimTick(shieldGrh, grhs, false, nowMs);
    }
}

using Argentum.Client.Core;
using Godot;

namespace Argentum.Client.World;

/// <summary>
/// VB6 niebla + luz alrededor del jugador — viñeta radial difuminada sobre el mundo.
/// </summary>
public partial class WorldFogOverlay : ColorRect
{
    private WorldSession? _session;
    private ShaderMaterial? _shaderMaterial;
    private bool _loginPreview;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Color = Colors.Transparent;
        SetAnchorsPreset(LayoutPreset.FullRect);
        var shader = GD.Load<Shader>("res://shaders/world_fog.gdshader");
        _shaderMaterial = new ShaderMaterial { Shader = shader };
        Material = _shaderMaterial;
        Visible = false;
    }

    public void Bind(WorldSession? session, bool loginPreview = false)
    {
        _session = session;
        _loginPreview = loginPreview;
        Visible = session is not null && !loginPreview;
        UpdateUniforms();
    }

    public override void _Process(double delta)
    {
        if (_session is null || _loginPreview)
        {
            return;
        }
        UpdateUniforms();
    }

    private void UpdateUniforms()
    {
        if (_shaderMaterial is null || _session is null)
        {
            return;
        }
        var screen = GameViewport.LogicalSize;
        var camera = WorldCamera.Create(_session);
        var motion = _session.Motion;
        var center = new Vector2(
            camera.TileToScreenX(_session.TileX, buffered: false) + motion.MoveOffsetX + CsmMap.TilePixels * 0.5f,
            camera.TileToScreenY(_session.TileY, buffered: false) + motion.MoveOffsetY + CsmMap.TilePixels * 0.5f);

        var fogMap = _session.Map?.Audio.Fog ?? false;
        // Halo pequeño iluminado en el centro; niebla densa en el resto de la pantalla.
        var inner = fogMap ? CsmMap.TilePixels * 1.35f : CsmMap.TilePixels * 2.0f;
        var outer = Mathf.Min(screen.X, screen.Y) * (fogMap ? 0.48f : 0.58f);
        var darkness = fogMap ? 0.94f : 0.72f;
        var falloff = fogMap ? 2.8f : 2.2f;
        var centerGlow = fogMap ? 0.16f : 0.10f;
        var fogColor = fogMap
            ? new Color(0.38f, 0.44f, 0.56f)
            : new Color(0.08f, 0.09f, 0.14f);

        _shaderMaterial.SetShaderParameter("u_center", center);
        _shaderMaterial.SetShaderParameter("u_viewport", screen);
        _shaderMaterial.SetShaderParameter("u_inner_radius", inner);
        _shaderMaterial.SetShaderParameter("u_outer_radius", outer);
        _shaderMaterial.SetShaderParameter("u_darkness", darkness);
        _shaderMaterial.SetShaderParameter("u_falloff_power", falloff);
        _shaderMaterial.SetShaderParameter("u_center_glow", centerGlow);
        _shaderMaterial.SetShaderParameter("u_fog_color", fogColor);
    }
}

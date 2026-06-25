using System;
using Godot;

namespace Argentum.Client.Ui;

public partial class GraphicalButton : TextureButton
{
    private Texture2D? _defaultTexture;
    private Texture2D? _overTexture;
    private Texture2D? _offTexture;
    private bool _enabled = true;

    public void Initialize(Texture2D? defaultTexture, Texture2D? overTexture, Texture2D? offTexture)
    {
        _defaultTexture = defaultTexture;
        _overTexture = overTexture ?? defaultTexture;
        _offTexture = offTexture ?? defaultTexture;
        TextureNormal = _defaultTexture;
        TextureHover = _overTexture;
        TexturePressed = _overTexture;
        TextureDisabled = _offTexture;
        IgnoreTextureSize = false;
        StretchMode = StretchModeEnum.Keep;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public void PlaceAt(AoUiScale scale, float designX, float designY)
    {
        Position = scale.MapPoint(designX, designY);
        if (TextureNormal is not null)
        {
            var texSize = TextureNormal.GetSize();
            Size = texSize * scale.Uniform;
        }
    }

    public new bool Disabled
    {
        get => !_enabled;
        set
        {
            _enabled = !value;
            base.Disabled = value;
            TextureNormal = value ? _offTexture : _defaultTexture;
        }
    }
}

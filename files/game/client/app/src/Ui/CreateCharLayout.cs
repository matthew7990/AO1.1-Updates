using Godot;

namespace Argentum.Client.Ui;

/// <summary>Coordenadas VB6 engine.RenderUICrearPJ + frmConnect.render_MouseUp (canvas 1024×768).</summary>
internal static class CreateCharLayout
{
    public const int PanelGrh = 727;
    public const int LogoGrh = 1171;
    public const int FrameGrh = 1169;

    public const float PanelX = 475f;
    public const float PanelY = 545f;
    public const float LogoX = 494f;
    public const float LogoY = 190f;

    public const float TitleX = 280f;
    public const float TitleY = 125f;
    public const float NameLabelX = 460f;
    public const float NameLabelY = 205f;
    public const float NameFieldX = 416f;
    public const float NameFieldY = 224f;
    public const float NameFieldW = 142f;
    public const float NameFieldH = 20f;

    public const float SelectorW = 95f;
    public const float SelectorH = 21f;

    public const float ClassLabelX = 585f;
    public const float ClassLabelY = 255f;
    public const float ClassBoxX = 557f;
    public const float ClassBoxY = 275f;
    public const float ClassValueX = 605f;
    public const float ClassValueY = 277f;

    public const float RaceLabelX = 587f;
    public const float RaceLabelY = 305f;
    public const float RaceBoxX = 557f;
    public const float RaceBoxY = 305f;
    public const float RaceValueX = 600f;
    public const float RaceValueY = 323f;

    public const float GenderLabelX = 345f;
    public const float GenderLabelY = 260f;
    public const float GenderBoxX = 322f;
    public const float GenderBoxY = 280f;
    public const float GenderValueX = 365f;
    public const float GenderValueY = 277f;

    public const float HomeLabelX = 345f;
    public const float HomeLabelY = 305f;
    public const float HomeBoxX = 322f;
    public const float HomeBoxY = 320f;
    public const float HomeValueX = 365f;
    public const float HomeValueY = 322f;

    public const float AttrTitleX = 575f;
    public const float AttrTitleY = 347f;
    public const float AttrLabelX = 525f;
    public const float AttrValueX = 645f;
    public const float AttrRow0Y = 372f;
    public const float AttrRow1Y = 402f;
    public const float AttrRow2Y = 432f;
    public const float AttrRow3Y = 462f;
    public const float AttrRow4Y = 492f;

    public const float AspectBoxX = 275f;
    public const float AspectBoxY = 367f;
    public const float AspectBoxW = 185f;
    public const float AspectBoxH = 148f;
    public const float AspectLabelX = 340f;
    public const float AspectLabelY = 345f;
    public const float HeadArrowLeftX = 330f;
    public const float HeadArrowLeftY = 372f;
    public const float HeadArrowRightX = 398f;
    public const float HeadArrowRightY = 372f;
    public const float BodyArrowLeftX = 288f;
    public const float BodyArrowLeftY = 408f;
    public const float BodyArrowRightX = 418f;
    public const float BodyArrowRightY = 408f;
    public const float DisplayNameX = 365f;
    public const float DisplayNameY = 478f;

    public const float PortraitX = 349f;
    public const float PortraitY = 446f;

    public const float BackBtnX = 148f;
    public const float BackBtnY = 630f;
    public const float ConfirmBtnX = 731f;
    public const float ConfirmBtnY = 630f;

    public static readonly Rect2 BackBtnHit = new(148, 630, 98, 40);
    public static readonly Rect2 BackBtnAltHit = new(289, 525, 160, 37);
    public static readonly Rect2 ConfirmBtnHit = new(731, 630, 98, 40);
    public static readonly Rect2 ConfirmBtnAltHit = new(532, 525, 160, 37);

    public static readonly Rect2 ClassBox = new(ClassBoxX, ClassBoxY, SelectorW, SelectorH);
    public static readonly Rect2 RaceBox = new(RaceBoxX, RaceBoxY, SelectorW, SelectorH);
    public static readonly Rect2 GenderBox = new(GenderBoxX, GenderBoxY, SelectorW, SelectorH);
    public static readonly Rect2 HomeBox = new(HomeBoxX, HomeBoxY, SelectorW, SelectorH);

    public static readonly Rect2 HeadArrowLeftHit = new(325, 371, 19, 16);
    public static readonly Rect2 HeadArrowRightHit = new(394, 373, 17, 13);
    public static readonly Rect2 HeadingLeftHit = new(282, 428, 40, 40);
    public static readonly Rect2 HeadingRightHit = new(412, 427, 34, 43);

    public static bool HitSelector(AoUiScale scale, Vector2 screenPos, Rect2 box, ref int index, int maxIndex)
    {
        if (!scale.Hit(box, screenPos))
        {
            return false;
        }
        var designPoint = scale.ScreenToDesign(screenPos);
        var midX = box.Position.X + box.Size.X * 0.5f;
        Cycle(ref index, maxIndex, designPoint.X < midX ? -1 : 1);
        return true;
    }

    private static void Cycle(ref int index, int maxIndex, int delta)
    {
        index += delta;
        if (index < 1)
        {
            index = maxIndex;
        }
        else if (index > maxIndex)
        {
            index = 1;
        }
    }
}

using System.Globalization;
using Argentum.Client.Resources;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>VB6 RenderUICrearPJ: etiquetas, cajas, flechas y valores de crear personaje.</summary>
internal sealed partial class CreateCharTextLayer : Control
{
    private static readonly Color LabelWhite = Colors.White;
    private static readonly Color LabelGray = new(0.78f, 0.78f, 0.78f);
    private static readonly Color ValueGray = new(0.78f, 0.78f, 0.78f);
    private static readonly Color NameBlue = new(0f, 0.5f, 0.75f);
    private static readonly Color BoxFill = new(1f, 1f, 1f, 0.39f);

    public string ClassText { get; set; } = "";
    public string RaceText { get; set; } = "";
    public string GenderText { get; set; } = "";
    public string HomeText { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int Fuerza { get; set; }
    public int Agilidad { get; set; }
    public int Inteligencia { get; set; }
    public int Constitucion { get; set; }
    public int Carisma { get; set; }

    public CreateCharTextLayer()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        var scale = new AoUiScale(GetViewportRect().Size);

        DrawLabel(scale, CreateCharLayout.TitleX, CreateCharLayout.TitleY,
            "Creación de Personaje", LabelGray, 12, AoUiFonts.Title);
        DrawLabel(scale, CreateCharLayout.NameLabelX, CreateCharLayout.NameLabelY,
            "Nombre", LabelWhite, 13, AoUiFonts.Title);

        DrawSelector(scale, "Clase", CreateCharLayout.ClassLabelX, CreateCharLayout.ClassLabelY,
            CreateCharLayout.ClassBoxX, CreateCharLayout.ClassBoxY,
            CreateCharLayout.ClassValueX, CreateCharLayout.ClassValueY, ClassText, LabelWhite);
        DrawSelector(scale, "Raza", CreateCharLayout.RaceLabelX, CreateCharLayout.RaceLabelY,
            CreateCharLayout.RaceBoxX, CreateCharLayout.RaceBoxY,
            CreateCharLayout.RaceValueX, CreateCharLayout.RaceValueY, RaceText, LabelWhite);
        DrawSelector(scale, "Género", CreateCharLayout.GenderLabelX, CreateCharLayout.GenderLabelY,
            CreateCharLayout.GenderBoxX, CreateCharLayout.GenderBoxY,
            CreateCharLayout.GenderValueX, CreateCharLayout.GenderValueY, GenderText, LabelWhite);
        DrawSelector(scale, "Hogar", CreateCharLayout.HomeLabelX, CreateCharLayout.HomeLabelY,
            CreateCharLayout.HomeBoxX, CreateCharLayout.HomeBoxY,
            CreateCharLayout.HomeValueX, CreateCharLayout.HomeValueY, HomeText, LabelGray);

        DrawLabel(scale, CreateCharLayout.AttrTitleX, CreateCharLayout.AttrTitleY,
            "Atributos", LabelWhite, 13, AoUiFonts.Title);
        DrawAttrRow(scale, CreateCharLayout.AttrLabelX, CreateCharLayout.AttrRow0Y, "Fuerza", Fuerza);
        DrawAttrRow(scale, CreateCharLayout.AttrLabelX, CreateCharLayout.AttrRow1Y, "Agilidad", Agilidad);
        DrawAttrRow(scale, CreateCharLayout.AttrLabelX, CreateCharLayout.AttrRow2Y, "Inteligencia", Inteligencia);
        DrawAttrRow(scale, CreateCharLayout.AttrLabelX, CreateCharLayout.AttrRow3Y, "Constitución", Constitucion);
        DrawAttrRow(scale, CreateCharLayout.AttrLabelX, CreateCharLayout.AttrRow4Y, "Carisma", Carisma);

        DrawRect(scale.MapRect(CreateCharLayout.AspectBoxX, CreateCharLayout.AspectBoxY,
            CreateCharLayout.AspectBoxW, CreateCharLayout.AspectBoxH), new Color(0, 0, 0, 0.31f));
        DrawLabel(scale, CreateCharLayout.AspectLabelX, CreateCharLayout.AspectLabelY,
            "Aspecto", LabelWhite, 13, AoUiFonts.Title);
        DrawArrow(scale, CreateCharLayout.HeadArrowLeftX, CreateCharLayout.HeadArrowLeftY, "<");
        DrawArrow(scale, CreateCharLayout.HeadArrowRightX, CreateCharLayout.HeadArrowRightY, ">");
        DrawArrow(scale, CreateCharLayout.BodyArrowLeftX, CreateCharLayout.BodyArrowLeftY, "<", large: true);
        DrawArrow(scale, CreateCharLayout.BodyArrowRightX, CreateCharLayout.BodyArrowRightY, ">", large: true);

        if (DisplayName.Length > 0)
        {
            DrawCentered(scale, CreateCharLayout.DisplayNameX, CreateCharLayout.DisplayNameY, DisplayName, NameBlue);
        }
    }

    private void DrawSelector(
        AoUiScale scale,
        string field,
        float labelX,
        float labelY,
        float boxX,
        float boxY,
        float valueX,
        float valueY,
        string value,
        Color labelColor)
    {
        DrawLabel(scale, labelX, labelY, field, labelColor, 13, AoUiFonts.Title);
        DrawRect(scale.MapRect(boxX, boxY, CreateCharLayout.SelectorW, CreateCharLayout.SelectorH), BoxFill);
        DrawArrow(scale, boxX - 17, boxY + 2, "<");
        DrawArrow(scale, boxX + 99, boxY + 2, ">");
        DrawCentered(scale, valueX, valueY, value, ValueGray);
    }

    private void DrawAttrRow(AoUiScale scale, float labelX, float y, string label, int value)
    {
        DrawLabel(scale, labelX, y, label, LabelWhite, 11, AoUiFonts.Ui);
        var color = value > CharacterCreationCatalog.BaseAttribute
            ? new Color(0.2f, 1f, 0.2f)
            : value < CharacterCreationCatalog.BaseAttribute
                ? new Color(1f, 0.3f, 0.3f)
                : Colors.White;
        DrawCentered(scale, CreateCharLayout.AttrValueX, y + 3, value.ToString(CultureInfo.InvariantCulture), color);
    }

    private void DrawArrow(AoUiScale scale, float x, float y, string text, bool large = false)
    {
        var fontSize = scale.FontSize(large ? 14 : 11);
        DrawString(AoUiFonts.Ui, scale.MapPoint(x, y).Round(), text, fontSize: fontSize, modulate: LabelWhite);
    }

    private void DrawLabel(AoUiScale scale, float x, float y, string text, Color color, int designSize, Font font)
    {
        DrawString(font, scale.MapPoint(x, y).Round(), text, fontSize: scale.FontSize(designSize), modulate: color);
    }

    private void DrawCentered(AoUiScale scale, float x, float y, string text, Color color)
    {
        if (text.Length == 0)
        {
            return;
        }
        var font = AoUiFonts.Ui;
        var fontSize = scale.FontSize(11);
        var size = font.GetStringSize(text, HorizontalAlignment.Center, -1, fontSize);
        var center = scale.MapPoint(x, y);
        var pos = (center - size / 2f + new Vector2(0, size.Y * 0.75f)).Round();
        DrawString(font, pos, text, fontSize: fontSize, modulate: color);
    }
}

using System;
using System.Collections.Generic;
using Argentum.Client.Resources;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>Tooltip de ítems reutilizable para piso e inventario, con layout estilo action-RPG.</summary>
public partial class FloorItemTooltip : Control
{
    private static readonly Color ShadowColor = new(0f, 0f, 0f, 0.34f);
    private static readonly Color PanelFill = new(0.04f, 0.045f, 0.08f, 0.97f);
    private static readonly Color PanelInner = new(0.14f, 0.11f, 0.08f, 0.8f);
    private static readonly Color PanelBorder = new(0.7f, 0.59f, 0.36f, 0.88f);
    private static readonly Color PanelAccent = new(0.9f, 0.78f, 0.46f, 0.45f);
    private static readonly Color MetaColor = new(0.71f, 0.69f, 0.64f);
    private static readonly Color ModifierColor = new(0.73f, 0.84f, 0.75f);
    private static readonly Color RequirementColor = new(0.92f, 0.85f, 0.47f);
    private static readonly Color StateColor = new(0.63f, 0.83f, 1f);
    private static readonly Color DescriptionColor = new(0.55f, 0.54f, 0.58f);
    private static readonly Color SeparatorColor = new(0.52f, 0.44f, 0.28f, 0.65f);

    private const float PaddingX = 14f;
    private const float PaddingY = 12f;
    private const float MaxContentWidth = 320f;
    private const float MinTooltipWidth = 220f;
    private const int CategoryFontSize = 10;
    private const int TitleFontSize = 16;

    private string _categoryLine = "";
    private string _title = "";
    private Color _titleColor = Colors.White;
    private readonly List<TooltipLine> _lines = new();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        ZIndex = 100;
    }

    public void HideTooltip()
    {
        Visible = false;
    }

    public void ShowItem(Vector2 screenPos, ItemDef? item, int amount, int elementalTags, bool equipped = false)
    {
        _categoryLine = BuildCategoryLine(item, elementalTags);
        _title = ItemAffixes.BuildDisplayName(item, elementalTags);
        _titleColor = ItemAffixes.TitleColor(elementalTags);
        _lines.Clear();

        if (amount > 1)
        {
            AddWrappedLine($"Cantidad: {amount}", TooltipLineKind.State, centered: true);
        }

        var modifierLines = ItemAffixes.BuildModifierLines(item, elementalTags);
        if (modifierLines.Count > 0)
        {
            AddSeparator();
            foreach (var line in modifierLines)
            {
                AddWrappedLine(line, ClassifyModifierLine(line));
            }
        }

        var showRequirements = item is not null && (item.MinLevel > 0 || ItemAffixes.HasModifiers(elementalTags));
        if (showRequirements || item is { Valor: > 0 } || equipped)
        {
            AddSeparator();
            if (showRequirements)
            {
                AddWrappedLine($"Nivel requerido: {ItemAffixes.RequiredLevel(item, elementalTags)}", TooltipLineKind.Requirement);
            }
            if (item is { Valor: > 0 })
            {
                AddWrappedLine($"Valor: {item.Valor:N0}", TooltipLineKind.Meta);
            }
            if (equipped)
            {
                AddWrappedLine("Equipado", TooltipLineKind.State, centered: true);
            }
        }

        if (!string.IsNullOrWhiteSpace(item?.Texto))
        {
            AddSeparator();
            AddWrappedLine($"“{item.Texto.Trim()}”", TooltipLineKind.Description, centered: true);
        }

        Visible = true;
        QueueRedraw();
        CallDeferred(MethodName.UpdatePosition, screenPos);
    }

    private string BuildCategoryLine(ItemDef? item, int elementalTags)
    {
        var rarity = ItemAffixes.RarityLabel(elementalTags);
        var type = ItemAffixes.TypeLabel(item?.ObjType ?? 0);
        return type == "Objeto" ? rarity : $"{rarity} · {type}";
    }

    private void AddWrappedLine(string text, TooltipLineKind kind, bool centered = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var font = ThemeDB.FallbackFont;
        foreach (var line in WrapText(font, text.Trim(), FontSizeFor(kind), MaxContentWidth))
        {
            _lines.Add(new TooltipLine(line, kind, centered));
        }
    }

    private void AddSeparator()
    {
        if (_lines.Count == 0 || _lines[^1].Kind == TooltipLineKind.Separator)
        {
            return;
        }
        _lines.Add(new TooltipLine("", TooltipLineKind.Separator, false));
    }

    private void UpdatePosition(Vector2 screenPos)
    {
        var font = ThemeDB.FallbackFont;
        var size = MeasureTooltip(font);
        var vp = GetViewportRect().Size;
        var x = screenPos.X + 18f;
        if (x + size.X > vp.X - 4f)
        {
            x = screenPos.X - size.X - 18f;
        }
        if (x < 4f)
        {
            x = Mathf.Clamp(screenPos.X - size.X * 0.5f, 4f, Mathf.Max(4f, vp.X - size.X - 4f));
        }

        var y = screenPos.Y - size.Y * 0.5f;
        y = Mathf.Clamp(y, 4f, Mathf.Max(4f, vp.Y - size.Y - 4f));

        Position = new Vector2(x, y);
        Size = size;
    }

    private Vector2 MeasureTooltip(Font font)
    {
        var contentWidth = MathF.Max(
            font.GetStringSize(_categoryLine, fontSize: CategoryFontSize).X,
            font.GetStringSize(_title, fontSize: TitleFontSize).X);
        var height = PaddingY * 2f;
        height += LineHeight(font, CategoryFontSize, 2f);
        height += LineHeight(font, TitleFontSize, 6f);

        foreach (var line in _lines)
        {
            if (line.Kind == TooltipLineKind.Separator)
            {
                height += 10f;
                continue;
            }

            var fontSize = FontSizeFor(line.Kind);
            contentWidth = MathF.Max(contentWidth, font.GetStringSize(line.Text, fontSize: fontSize).X);
            height += LineHeight(font, fontSize, line.Kind == TooltipLineKind.Description ? 3f : 2f);
        }

        var width = Mathf.Clamp(contentWidth + PaddingX * 2f, MinTooltipWidth, MaxContentWidth + PaddingX * 2f);
        return new Vector2(width, height);
    }

    public override void _Draw()
    {
        if (!Visible)
        {
            return;
        }

        var rect = new Rect2(Vector2.Zero, Size);
        DrawRect(new Rect2(new Vector2(2f, 3f), Size), ShadowColor);
        DrawRect(rect, PanelFill);
        DrawRect(rect.Grow(-1f), PanelInner, false, 1f);
        DrawRect(rect, PanelBorder, false, 1f);
        DrawLine(new Vector2(10f, 4f), new Vector2(Size.X - 10f, 4f), PanelAccent, 1f);
        DrawLine(new Vector2(10f, Size.Y - 4f), new Vector2(Size.X - 10f, Size.Y - 4f), PanelAccent, 1f);

        var font = ThemeDB.FallbackFont;
        var contentWidth = Size.X - PaddingX * 2f;
        var y = PaddingY;

        DrawTextLine(font, _categoryLine, new Vector2(PaddingX, y), CategoryFontSize, MetaColor, centered: true, width: contentWidth);
        y += LineHeight(font, CategoryFontSize, 2f);
        DrawTextLine(font, _title, new Vector2(PaddingX, y), TitleFontSize, _titleColor, centered: true, width: contentWidth);
        y += LineHeight(font, TitleFontSize, 6f);

        foreach (var line in _lines)
        {
            if (line.Kind == TooltipLineKind.Separator)
            {
                DrawLine(new Vector2(PaddingX, y + 3f), new Vector2(Size.X - PaddingX, y + 3f), SeparatorColor, 1f);
                y += 10f;
                continue;
            }

            var fontSize = FontSizeFor(line.Kind);
            DrawTextLine(font, line.Text, new Vector2(PaddingX, y), fontSize, ColorFor(line.Kind), line.Centered, contentWidth);
            y += LineHeight(font, fontSize, line.Kind == TooltipLineKind.Description ? 3f : 2f);
        }
    }

    private void DrawTextLine(Font font, string text, Vector2 pos, int fontSize, Color color, bool centered, float width)
    {
        var baseline = pos.Y + font.GetAscent(fontSize);
        var alignment = centered ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        var drawWidth = centered ? width : -1f;
        DrawString(font, new Vector2(pos.X, baseline), text, alignment, drawWidth, fontSize, color);
    }

    private static float LineHeight(Font font, int fontSize, float extraSpacing) => font.GetHeight(fontSize) + extraSpacing;

    private static int FontSizeFor(TooltipLineKind kind) => kind switch
    {
        TooltipLineKind.Description => 12,
        _ => 13,
    };

    private static Color ColorFor(TooltipLineKind kind) => kind switch
    {
        TooltipLineKind.Element => new Color("d7c27f"),
        TooltipLineKind.Modifier => ModifierColor,
        TooltipLineKind.Requirement => RequirementColor,
        TooltipLineKind.State => StateColor,
        TooltipLineKind.Description => DescriptionColor,
        _ => MetaColor,
    };

    private static TooltipLineKind ClassifyModifierLine(string line)
    {
        if (line.StartsWith("[", StringComparison.Ordinal))
        {
            return TooltipLineKind.Element;
        }
        if (line.StartsWith("+", StringComparison.Ordinal)
            || line.Contains("Regenera", StringComparison.Ordinal)
            || line.Contains("Inmune", StringComparison.Ordinal))
        {
            return TooltipLineKind.Modifier;
        }
        return TooltipLineKind.Meta;
    }

    private static IEnumerable<string> WrapText(Font font, string text, int fontSize, float maxWidth)
    {
        var paragraphs = text.Replace("\r", "").Split('\n');
        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                yield return "";
                continue;
            }

            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                continue;
            }

            var current = words[0];
            for (var i = 1; i < words.Length; i++)
            {
                var candidate = current + " " + words[i];
                if (font.GetStringSize(candidate, fontSize: fontSize).X <= maxWidth)
                {
                    current = candidate;
                    continue;
                }

                yield return current;
                current = words[i];
            }

            yield return current;
        }
    }

    private enum TooltipLineKind
    {
        Meta,
        Modifier,
        Element,
        Requirement,
        State,
        Description,
        Separator,
    }

    private readonly record struct TooltipLine(string Text, TooltipLineKind Kind, bool Centered);
}

using System.Collections.Generic;
using Argentum.Client.Resources;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Ui;

/// <summary>Panel de hechizos (VB6 picHechiz / hlst + cmdlanzar).</summary>
public partial class SpellScreen : Control
{
    private WorldSession? _world;
    private SpellCatalog? _spells;
    private Gameplay.GameplaySession? _gameplay;
    private readonly List<string> _rows = new();
    private int _selectedSlot = 1;
    private Label? _detail;
    private ItemList? _list;

    public bool IsOpen => Visible;
    public int SelectedSpellBookSlot => _selectedSlot;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        var panel = new PanelContainer
        {
            Position = new Vector2(24, 120),
            CustomMinimumSize = new Vector2(280, 360),
        };
        AddChild(panel);
        var vbox = new VBoxContainer();
        panel.AddChild(vbox);
        vbox.AddChild(new Label { Text = "Hechizos" });
        _list = new ItemList { CustomMinimumSize = new Vector2(260, 220) };
        _list.ItemSelected += OnItemSelected;
        vbox.AddChild(_list);
        _detail = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(260, 60),
        };
        vbox.AddChild(_detail);
        var castBtn = new Button { Text = "Lanzar (H)" };
        castBtn.Pressed += OnCastPressed;
        vbox.AddChild(castBtn);
        var closeBtn = new Button { Text = "Cerrar" };
        closeBtn.Pressed += Close;
        vbox.AddChild(closeBtn);
    }

    public void Bind(WorldSession? world, SpellCatalog? spells, Gameplay.GameplaySession? gameplay)
    {
        _world = world;
        _spells = spells;
        _gameplay = gameplay;
        RebuildList();
    }

    public void Open()
    {
        RebuildList();
        Visible = true;
    }

    public void Close() => Visible = false;

    public void Toggle()
    {
        if (Visible)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void RebuildList()
    {
        if (_list is null || _world is null)
        {
            return;
        }
        _rows.Clear();
        _list.Clear();
        for (var slot = 1; slot <= SpellBook.MaxSlots; slot++)
        {
            var label = $"{slot,2}. {_world.Spells.SlotLabel(slot, _spells)}";
            _rows.Add(label);
            _list.AddItem(label);
        }
        UpdateDetail();
    }

    private void OnItemSelected(long index)
    {
        _selectedSlot = (int)index + 1;
        UpdateDetail();
    }

    private void UpdateDetail()
    {
        if (_detail is null || _world is null)
        {
            return;
        }
        var id = _world.Spells.GetSlot(_selectedSlot);
        if (id <= 0)
        {
            _detail.Text = "Slot vacío.";
            return;
        }
        var entry = _spells?.Get(id);
        _detail.Text = entry is null
            ? $"Hechizo #{id}"
            : $"{entry.Name}\n{entry.Desc}\nMana: {entry.ManaCost}  Sta: {entry.StaCost}";
    }

    public async void CastSelected()
    {
        if (_gameplay is null || _world is null || _world.IsDead)
        {
            return;
        }
        if (_world.Spells.GetSlot(_selectedSlot) <= 0)
        {
            _world.Console.Add("Seleccioná un hechizo.", new Color("f0d878"));
            return;
        }
        await _gameplay.SendCastSpellAsync(_selectedSlot);
    }

    private void OnCastPressed() => CastSelected();
}

using Godot;
using Ragna.Core.Items;
using Ragna.Gameplay.Items;

namespace Ragna.Ui;

/// <summary>
/// A basic window showing the player's <see cref="PlayerInventory"/> as a grid of slots. Press
/// <see cref="ToggleAction"/> (I) to open or close it, drag it by its title bar, and drag a slot
/// onto another to move, merge or swap stacks. Hover a slot to see the item's name and description.
/// </summary>
/// <remarks>
/// The slots are built in code from the inventory's size. Each item shows as a square in its
/// <see cref="ItemDefinition.Tint"/> until there are item icons.
/// </remarks>
public partial class InventoryWindow : PanelContainer
{
    public const string ToggleAction = "toggle_inventory";

    private readonly List<InventorySlotView> _slots = [];
    private Inventory? _inventory;
    private Label? _footer;
    private bool _dragging;

    /// <summary>The player's inventory node.</summary>
    [Export]
    public PlayerInventory? Source { get; set; }

    /// <summary>Width and height of one slot.</summary>
    [Export(PropertyHint.Range, "24,96,1,suffix:px")]
    public int SlotSize { get; set; } = 40;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        if (Source is null)
        {
            GD.PushWarning($"{Name}: no Source inventory set.");
            Visible = false;
            return;
        }

        _inventory = Source.Inventory;
        Build(_inventory);
        _inventory.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree()
    {
        if (_inventory is not null)
        {
            _inventory.Changed -= Refresh;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_inventory is not null && @event.IsActionPressed(ToggleAction))
        {
            Visible = !Visible;
            GetViewport().SetInputAsHandled();
        }
    }

    private void Build(Inventory inventory)
    {
        var style = new StyleBoxFlat { BgColor = new Color(0.12f, 0.13f, 0.16f, 0.92f) };
        style.SetCornerRadiusAll(4);
        style.SetContentMarginAll(8);
        AddThemeStyleboxOverride("panel", style);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        AddChild(column);

        var titleBar = new HBoxContainer { MouseFilter = MouseFilterEnum.Stop };
        titleBar.GuiInput += OnTitleBarInput;
        titleBar.AddChild(new Label
        {
            Text = "Inventory",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        });
        var close = new Button { Text = "×", Flat = true, TooltipText = "Close (I)", FocusMode = FocusModeEnum.None };
        close.Pressed += () => Visible = false;
        titleBar.AddChild(close);
        column.AddChild(titleBar);

        var grid = new GridContainer { Columns = inventory.Columns };
        grid.AddThemeConstantOverride("h_separation", 2);
        grid.AddThemeConstantOverride("v_separation", 2);
        for (var i = 0; i < inventory.Capacity; i++)
        {
            var slot = new InventorySlotView();
            slot.Setup(inventory, i, SlotSize);
            _slots.Add(slot);
            grid.AddChild(slot);
        }

        column.AddChild(grid);

        _footer = new Label { HorizontalAlignment = HorizontalAlignment.Right };
        column.AddChild(_footer);
    }

    private void Refresh()
    {
        if (_inventory is null)
        {
            return;
        }

        foreach (var slot in _slots)
        {
            slot.Refresh();
        }

        if (_footer is not null)
        {
            _footer.Text = $"{_inventory.Capacity - _inventory.EmptySlots} / {_inventory.Capacity}";
        }
    }

    private void OnTitleBarInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                _dragging = button.Pressed;
                AcceptEvent();
                break;
            case InputEventMouseMotion motion when _dragging:
                Position = ClampToViewport(Position + motion.Relative);
                AcceptEvent();
                break;
        }
    }

    private Vector2 ClampToViewport(Vector2 position)
    {
        var max = GetViewportRect().Size - Size;
        return new Vector2(Mathf.Clamp(position.X, 0, Mathf.Max(0, max.X)), Mathf.Clamp(position.Y, 0, Mathf.Max(0, max.Y)));
    }
}

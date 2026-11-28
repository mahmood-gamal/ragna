using Godot;
using Ragna.Core.Items;
using Ragna.Core.Monsters;

namespace Ragna.Ui;

/// <summary>One slot of the <see cref="InventoryWindow"/>: the item's colour square and its quantity.</summary>
public partial class InventorySlotView : Panel
{
    private const string DragKey = "inventory_slot";

    private Inventory _inventory = new(1, 1);
    private int _slot;
    private ColorRect _icon = null!;
    private Label _quantity = null!;

    /// <summary>Shows <paramref name="slot"/> of <paramref name="inventory"/>. Call once, before adding it to the tree.</summary>
    public void Setup(Inventory inventory, int slot, int size)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        _inventory = inventory;
        _slot = slot;
        CustomMinimumSize = new Vector2(size, size);
        MouseFilter = MouseFilterEnum.Stop;

        var inset = Mathf.Max(4, size / 6);
        _icon = new ColorRect { MouseFilter = MouseFilterEnum.Ignore };
        _icon.SetAnchorsPreset(LayoutPreset.FullRect);
        _icon.OffsetLeft = inset;
        _icon.OffsetTop = inset;
        _icon.OffsetRight = -inset;
        _icon.OffsetBottom = -inset;
        AddChild(_icon);

        _quantity = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        _quantity.SetAnchorsPreset(LayoutPreset.FullRect);
        _quantity.OffsetRight = -2;
        _quantity.AddThemeFontSizeOverride("font_size", 12);
        _quantity.AddThemeConstantOverride("outline_size", 4);
        _quantity.AddThemeColorOverride("font_outline_color", Colors.Black);
        AddChild(_quantity);
    }

    /// <summary>Shows what the slot holds now.</summary>
    public void Refresh()
    {
        if (_icon is null)
        {
            return;
        }

        if (_inventory[_slot] is { } stack)
        {
            _icon.Visible = true;
            _icon.Color = TintOf(stack.Item);
            _quantity.Text = stack.Quantity > 1 ? stack.Quantity.ToString() : "";
            TooltipText = string.IsNullOrEmpty(stack.Item.Description)
                ? $"{stack.Item.Name} x{stack.Quantity}"
                : $"{stack.Item.Name} x{stack.Quantity}\n{stack.Item.Description}";
        }
        else
        {
            _icon.Visible = false;
            _quantity.Text = "";
            TooltipText = "";
        }
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (_inventory[_slot] is not { } stack)
        {
            return default;
        }

        var preview = new ColorRect { Color = TintOf(stack.Item), Size = _icon.Size, Modulate = new Color(1, 1, 1, 0.8f) };
        SetDragPreview(preview);
        return new Godot.Collections.Dictionary { [DragKey] = _slot };
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) =>
        data.VariantType == Variant.Type.Dictionary && data.AsGodotDictionary().ContainsKey(DragKey);

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var from = data.AsGodotDictionary()[DragKey].AsInt32();
        _inventory.Move(from, _slot);
    }

    private static Color TintOf(ItemDefinition item) =>
        MonsterDefinition.TryParseTint(item.Tint, out var r, out var g, out var b) ? new Color(r, g, b) : Colors.White;
}

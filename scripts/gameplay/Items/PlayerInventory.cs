using Godot;
using Ragna.Core.Items;

namespace Ragna.Gameplay.Items;

/// <summary>
/// The player's grid inventory. Holds a <see cref="Core.Items.Inventory"/> of
/// <see cref="Columns"/> × <see cref="Rows"/> slots; <see cref="PlayerPickup"/> puts picked-up items
/// in it and <see cref="Ui.InventoryWindow"/> shows it. The rules live in the core class.
/// </summary>
/// <remarks>Put it as a child of the player, next to the <see cref="PlayerPickup"/> node.</remarks>
public partial class PlayerInventory : Node
{
    private Inventory? _inventory;

    /// <summary>Slots per row. Read once when the node enters the tree.</summary>
    [Export(PropertyHint.Range, "1,16,1")]
    public int Columns { get; set; } = 8;

    /// <summary>Number of rows. Read once when the node enters the tree.</summary>
    [Export(PropertyHint.Range, "1,16,1")]
    public int Rows { get; set; } = 5;

    /// <summary>The slots. Created on first use with <see cref="Columns"/> and <see cref="Rows"/>.</summary>
    public Inventory Inventory => _inventory ??= new Inventory(Columns, Rows);

    public override void _EnterTree() => _ = Inventory;
}

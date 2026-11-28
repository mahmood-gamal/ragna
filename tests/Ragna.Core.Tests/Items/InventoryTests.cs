using Ragna.Core.Items;

namespace Ragna.Core.Tests.Items;

public sealed class InventoryTests
{
    private static readonly ItemDefinition Gel = new() { Id = "slime_gel", Name = "Slime Gel", MaxStack = 10 };
    private static readonly ItemDefinition Tusk = new() { Id = "boar_tusk", Name = "Boar Tusk", MaxStack = 10 };
    private static readonly ItemDefinition Sword = new() { Id = "sword", Name = "Sword", MaxStack = 1 };

    [Fact]
    public void Starts_empty()
    {
        var inventory = new Inventory(4, 3);

        Assert.Equal(12, inventory.Capacity);
        Assert.Equal(12, inventory.EmptySlots);
        Assert.False(inventory.IsFull);
        Assert.All(inventory.Slots, s => Assert.Null(s));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void Rejects_an_empty_grid(int columns, int rows)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Inventory(columns, rows));
    }

    [Fact]
    public void Add_puts_a_new_item_in_the_first_empty_slot()
    {
        var inventory = new Inventory(3, 2);

        var left = inventory.Add(Gel, 3);

        Assert.Equal(0, left);
        Assert.Equal(new ItemStack(Gel, 3), inventory[0]);
        Assert.Equal(3, inventory.Count("slime_gel"));
        Assert.Equal(5, inventory.EmptySlots);
    }

    [Fact]
    public void Add_stacks_onto_the_same_item()
    {
        var inventory = new Inventory(3, 2);
        inventory.Add(Gel, 3);
        inventory.Add(Tusk, 1);

        inventory.Add(Gel, 4);

        Assert.Equal(new ItemStack(Gel, 7), inventory[0]);
        Assert.Equal(new ItemStack(Tusk, 1), inventory[1]);
        Assert.Null(inventory[2]);
    }

    [Fact]
    public void Add_matches_ids_ignoring_case()
    {
        var inventory = new Inventory(2, 1);
        inventory.Add(Gel, 1);

        inventory.Add(Gel with { Id = "SLIME_GEL" }, 1);

        Assert.Equal(2, inventory[0]!.Value.Quantity);
        Assert.Null(inventory[1]);
    }

    [Fact]
    public void Add_fills_a_stack_then_spills_into_empty_slots()
    {
        var inventory = new Inventory(3, 1);
        inventory.Add(Gel, 8);

        inventory.Add(Gel, 15);

        Assert.Equal(10, inventory[0]!.Value.Quantity);
        Assert.Equal(10, inventory[1]!.Value.Quantity);
        Assert.Equal(3, inventory[2]!.Value.Quantity);
        Assert.Equal(23, inventory.Count("slime_gel"));
    }

    [Fact]
    public void Add_fills_existing_stacks_before_earlier_empty_slots()
    {
        var inventory = new Inventory(3, 1);
        inventory.Add(Tusk, 1);
        inventory.Add(Gel, 2);
        inventory.RemoveAt(0, 1);

        inventory.Add(Gel, 3);

        Assert.Null(inventory[0]);
        Assert.Equal(new ItemStack(Gel, 5), inventory[1]);
    }

    [Fact]
    public void Items_that_do_not_stack_take_a_slot_each()
    {
        var inventory = new Inventory(3, 1);

        inventory.Add(Sword, 2);

        Assert.Equal(new ItemStack(Sword, 1), inventory[0]);
        Assert.Equal(new ItemStack(Sword, 1), inventory[1]);
        Assert.Null(inventory[2]);
    }

    [Fact]
    public void Add_returns_what_does_not_fit_when_full()
    {
        var inventory = new Inventory(2, 1);
        inventory.Add(Tusk, 1);

        var left = inventory.Add(Gel, 14);

        Assert.Equal(4, left);
        Assert.Equal(10, inventory.Count("slime_gel"));
        Assert.True(inventory.IsFull);
    }

    [Fact]
    public void A_full_inventory_still_takes_more_of_a_stack_with_room()
    {
        var inventory = new Inventory(2, 1);
        inventory.Add(Gel, 5);
        inventory.Add(Tusk, 1);
        Assert.True(inventory.IsFull);

        Assert.Equal(0, inventory.Add(Gel, 5));
        Assert.Equal(1, inventory.Add(Gel, 1));
        Assert.Equal(1, inventory.Add(Sword, 1));
        Assert.Equal(10, inventory.Count("slime_gel"));
    }

    [Fact]
    public void SpaceFor_counts_room_in_stacks_and_empty_slots()
    {
        var inventory = new Inventory(3, 1);
        inventory.Add(Gel, 7);
        inventory.Add(Tusk, 1);

        Assert.Equal(13, inventory.SpaceFor(Gel));
        Assert.Equal(19, inventory.SpaceFor(Tusk));
        Assert.Equal(1, inventory.SpaceFor(Sword));
    }

    [Fact]
    public void TryAddAll_adds_nothing_unless_everything_fits()
    {
        var inventory = new Inventory(1, 1);
        inventory.Add(Gel, 8);
        var changes = 0;
        inventory.Changed += () => changes++;

        Assert.False(inventory.TryAddAll(Gel, 3));
        Assert.Equal(8, inventory.Count("slime_gel"));
        Assert.Equal(0, changes);

        Assert.True(inventory.TryAddAll(Gel, 2));
        Assert.Equal(10, inventory.Count("slime_gel"));
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Remove_takes_from_the_last_stacks_first()
    {
        var inventory = new Inventory(3, 1);
        inventory.Add(Gel, 25);

        var removed = inventory.Remove("slime_gel", 7);

        Assert.Equal(7, removed);
        Assert.Equal(10, inventory[0]!.Value.Quantity);
        Assert.Equal(8, inventory[1]!.Value.Quantity);
        Assert.Null(inventory[2]);
    }

    [Fact]
    public void Remove_takes_what_there_is_when_asked_for_more()
    {
        var inventory = new Inventory(3, 1);
        inventory.Add(Gel, 4);
        inventory.Add(Tusk, 2);

        Assert.Equal(4, inventory.Remove("SLIME_GEL", 9));
        Assert.Equal(0, inventory.Count("slime_gel"));
        Assert.Null(inventory[0]);
        Assert.Equal(new ItemStack(Tusk, 2), inventory[1]);
    }

    [Fact]
    public void Remove_of_a_missing_item_changes_nothing()
    {
        var inventory = new Inventory(2, 1);
        var changes = 0;
        inventory.Changed += () => changes++;

        Assert.Equal(0, inventory.Remove("slime_gel", 1));
        Assert.Equal(0, changes);
    }

    [Fact]
    public void RemoveAt_takes_from_one_slot()
    {
        var inventory = new Inventory(2, 1);
        inventory.Add(Gel, 15);

        Assert.Equal(3, inventory.RemoveAt(1, 3));
        Assert.Equal(new ItemStack(Gel, 2), inventory[1]);
        Assert.Equal(2, inventory.RemoveAt(1, 10));
        Assert.Null(inventory[1]);
        Assert.Equal(0, inventory.RemoveAt(1, 1));
    }

    [Fact]
    public void Removing_frees_a_slot_in_a_full_inventory()
    {
        var inventory = new Inventory(1, 1);
        inventory.Add(Tusk, 1);
        Assert.True(inventory.IsFull);

        inventory.Remove("boar_tusk", 1);

        Assert.False(inventory.IsFull);
        Assert.Equal(0, inventory.Add(Gel, 1));
    }

    [Fact]
    public void Move_onto_an_empty_slot_moves_the_stack()
    {
        var inventory = new Inventory(3, 2);
        inventory.Add(Gel, 3);

        Assert.True(inventory.Move(0, 4));

        Assert.Null(inventory[0]);
        Assert.Equal(new ItemStack(Gel, 3), inventory[1, 1]);
    }

    [Fact]
    public void Move_onto_a_different_item_swaps()
    {
        var inventory = new Inventory(2, 1);
        inventory.Add(Gel, 3);
        inventory.Add(Tusk, 1);

        Assert.True(inventory.Move(0, 1));

        Assert.Equal(new ItemStack(Tusk, 1), inventory[0]);
        Assert.Equal(new ItemStack(Gel, 3), inventory[1]);
    }

    [Fact]
    public void Move_onto_the_same_item_merges_what_fits()
    {
        var inventory = new Inventory(3, 1);
        inventory.Add(Gel, 10);
        inventory.Add(Gel, 6);
        inventory.Move(0, 2);
        inventory.RemoveAt(1, 3);

        // Slot 1 holds 3, slot 2 holds 10: 10 onto 3 makes 10 and leaves 3.
        Assert.True(inventory.Move(2, 1));

        Assert.Equal(new ItemStack(Gel, 10), inventory[1]);
        Assert.Equal(new ItemStack(Gel, 3), inventory[2]);
    }

    [Fact]
    public void Move_onto_a_full_stack_of_the_same_item_does_nothing()
    {
        var inventory = new Inventory(2, 1);
        inventory.Add(Gel, 15);

        Assert.False(inventory.Move(1, 0));
        Assert.Equal(10, inventory[0]!.Value.Quantity);
        Assert.Equal(5, inventory[1]!.Value.Quantity);
    }

    [Fact]
    public void Move_from_an_empty_slot_or_onto_itself_does_nothing()
    {
        var inventory = new Inventory(2, 1);
        inventory.Add(Gel, 1);

        Assert.False(inventory.Move(1, 0));
        Assert.False(inventory.Move(0, 0));
        Assert.Equal(new ItemStack(Gel, 1), inventory[0]);
    }

    [Fact]
    public void Slots_are_numbered_row_by_row()
    {
        var inventory = new Inventory(4, 3);

        Assert.Equal(0, inventory.SlotAt(0, 0));
        Assert.Equal(3, inventory.SlotAt(3, 0));
        Assert.Equal(4, inventory.SlotAt(0, 1));
        Assert.Equal(11, inventory.SlotAt(3, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => inventory.SlotAt(4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => inventory.SlotAt(0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => inventory[12]);
    }

    [Fact]
    public void Changed_fires_on_each_change()
    {
        var inventory = new Inventory(2, 1);
        var changes = 0;
        inventory.Changed += () => changes++;

        inventory.Add(Gel, 1);
        inventory.Move(0, 1);
        inventory.RemoveAt(1, 1);
        inventory.Clear();

        Assert.Equal(4, changes);
    }

    [Fact]
    public void Add_that_fits_nothing_does_not_fire_changed()
    {
        var inventory = new Inventory(1, 1);
        inventory.Add(Sword, 1);
        var changes = 0;
        inventory.Changed += () => changes++;

        Assert.Equal(2, inventory.Add(Tusk, 2));
        Assert.Equal(0, changes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantities_must_be_positive(int quantity)
    {
        var inventory = new Inventory(1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => inventory.Add(Gel, quantity));
        Assert.Throws<ArgumentOutOfRangeException>(() => inventory.Remove("slime_gel", quantity));
        Assert.Throws<ArgumentOutOfRangeException>(() => inventory.RemoveAt(0, quantity));
    }
}

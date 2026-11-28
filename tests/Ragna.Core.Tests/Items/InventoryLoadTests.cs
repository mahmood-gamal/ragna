using Ragna.Core.Items;

namespace Ragna.Core.Tests.Items;

public sealed class InventoryLoadTests
{
    private static readonly ItemDefinition Gel = new() { Id = "slime_gel", Name = "Slime Gel", MaxStack = 10 };

    [Fact]
    public void Load_replaces_the_slots_and_raises_changed_once()
    {
        var inventory = new Inventory(2, 2);
        inventory.Add(Gel, 1);
        var changes = 0;
        inventory.Changed += () => changes++;

        inventory.Load([(3, new ItemStack(Gel, 4))]);

        Assert.Null(inventory[0]);
        Assert.Equal(new ItemStack(Gel, 4), inventory[3]);
        Assert.Equal(1, changes);
    }

    [Theory]
    [InlineData(4, 1)]
    [InlineData(-1, 1)]
    [InlineData(0, 0)]
    [InlineData(0, 11)]
    public void Load_rejects_bad_stacks_and_changes_nothing(int slot, int quantity)
    {
        var inventory = new Inventory(2, 2);
        inventory.Add(Gel, 1);

        Assert.ThrowsAny<ArgumentException>(() => inventory.Load([(slot, new ItemStack(Gel, quantity))]));

        Assert.Equal(new ItemStack(Gel, 1), inventory[0]);
    }

    [Fact]
    public void Load_rejects_a_slot_listed_twice()
    {
        var inventory = new Inventory(2, 2);

        Assert.Throws<ArgumentException>(() => inventory.Load([(1, new ItemStack(Gel, 1)), (1, new ItemStack(Gel, 2))]));
    }
}

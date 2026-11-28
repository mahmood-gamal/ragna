using System.Numerics;
using Ragna.Core.Combat;
using Ragna.Core.Items;
using Ragna.Core.Stats;

namespace Ragna.Core.Save;

/// <summary>
/// Copies live game state into <see cref="SaveData"/> parts and back. Restoring changes the
/// existing objects in place, so the HUD and anything else listening to them keeps working.
/// </summary>
public static class SaveMapper
{
    public static PlayerSave CapturePlayer(CharacterStats stats, Health health, SpPool sp, Vector3 position)
    {
        ArgumentNullException.ThrowIfNull(stats);
        ArgumentNullException.ThrowIfNull(health);
        ArgumentNullException.ThrowIfNull(sp);
        return new PlayerSave
        {
            Level = stats.Level,
            Exp = stats.Exp,
            StatPoints = stats.StatPoints,
            Str = stats.Base.Str,
            Agi = stats.Base.Agi,
            Vit = stats.Base.Vit,
            Int = stats.Base.Int,
            Dex = stats.Base.Dex,
            Luk = stats.Base.Luk,
            Hp = health.Current,
            Sp = sp.Current,
            Position = new PositionSave(position.X, position.Y, position.Z),
        };
    }

    public static InventorySave CaptureInventory(Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var slots = new List<SlotSave>();
        for (var i = 0; i < inventory.Capacity; i++)
        {
            if (inventory[i] is { } stack)
            {
                slots.Add(new SlotSave(i, stack.Item.Id, stack.Quantity));
            }
        }

        return new InventorySave { Columns = inventory.Columns, Rows = inventory.Rows, Slots = slots };
    }

    /// <summary>
    /// Sets the player's level, EXP, stats, HP and SP from <paramref name="save"/>. Max HP and SP
    /// follow the restored stats; HP is kept at least 1 so a save never loads a dead player.
    /// </summary>
    /// <exception cref="InvalidDataException">A stat is out of the table's range; nothing changes.</exception>
    public static void RestorePlayer(PlayerSave save, CharacterStats stats, Health health, SpPool sp)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(stats);
        ArgumentNullException.ThrowIfNull(health);
        ArgumentNullException.ThrowIfNull(sp);
        try
        {
            stats.Restore(
                save.Level,
                save.Exp,
                new BaseStats(save.Str, save.Agi, save.Vit, save.Int, save.Dex, save.Luk),
                save.StatPoints);
        }
        catch (ArgumentOutOfRangeException e)
        {
            throw new InvalidDataException($"Saved player stats are out of range: {e.Message}", e);
        }

        health.SetMax(stats.Derived.MaxHp);
        health.SetCurrent(save.Hp);
        sp.SetMax(stats.Derived.MaxSp);
        sp.SetCurrent(save.Sp);
    }

    /// <summary>
    /// Fills <paramref name="inventory"/> from <paramref name="save"/>, looking items up in
    /// <paramref name="items"/>. Stacks go back in their slots. A stack whose slot no longer
    /// exists, or that is bigger than its item's max stack, has the extra added wherever it fits.
    /// Unknown items, and anything that doesn't fit, are dropped.
    /// </summary>
    /// <returns>A line for each thing that couldn't be restored as saved. Empty when all was.</returns>
    public static IReadOnlyList<string> RestoreInventory(InventorySave save, Inventory inventory, ItemDatabase items)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(items);
        var problems = new List<string>();
        var placed = new List<(int Slot, ItemStack Stack)>();
        var taken = new HashSet<int>();
        var extra = new List<(ItemDefinition Item, int Quantity)>();

        foreach (var slot in save.Slots)
        {
            if (slot.Quantity < 1)
            {
                problems.Add($"Slot {slot.Slot}: skipped {slot.Quantity} × {slot.Item}.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(slot.Item) || !items.TryGet(slot.Item, out var item))
            {
                problems.Add($"Slot {slot.Slot}: unknown item '{slot.Item}' dropped.");
                continue;
            }

            if (slot.Slot < 0 || slot.Slot >= inventory.Capacity || !taken.Add(slot.Slot))
            {
                extra.Add((item, slot.Quantity));
                continue;
            }

            var quantity = Math.Min(slot.Quantity, item.MaxStack);
            placed.Add((slot.Slot, new ItemStack(item, quantity)));
            if (slot.Quantity > quantity)
            {
                extra.Add((item, slot.Quantity - quantity));
            }
        }

        inventory.Load(placed);
        foreach (var (item, quantity) in extra)
        {
            var left = inventory.Add(item, quantity);
            if (left > 0)
            {
                problems.Add($"No room for {left} × {item.Id}; dropped.");
            }
        }

        return problems;
    }
}

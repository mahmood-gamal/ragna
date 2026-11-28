using System.Numerics;
using Ragna.Core.Combat;
using Ragna.Core.Items;
using Ragna.Core.Save;
using Ragna.Core.Stats;

namespace Ragna.Core.Tests.Save;

public sealed class SaveMapperTests
{
    private static readonly ItemDefinition Gel = new() { Id = "slime_gel", Name = "Slime Gel", MaxStack = 10 };
    private static readonly ItemDefinition Sword = new() { Id = "sword", Name = "Sword", MaxStack = 1 };
    private static readonly ItemDatabase Items = new([Gel, Sword]);

    private static (CharacterStats Stats, Health Health, SpPool Sp) NewPlayer()
    {
        var stats = new CharacterStats(new StatTable(), 1, 0, new BaseStats(10, 10, 5, 1, 10, 5), 0);
        var health = new Health(stats.Derived.MaxHp);
        var sp = new SpPool(stats.Derived.MaxSp);

        // As PlayerCombat does: max HP and SP follow the stats.
        stats.Changed += () =>
        {
            health.SetMax(stats.Derived.MaxHp);
            sp.SetMax(stats.Derived.MaxSp);
        };
        return (stats, health, sp);
    }

    [Fact]
    public void Player_round_trips_through_json()
    {
        var (stats, health, sp) = NewPlayer();
        stats.AddExp(5000);
        while (stats.TryRaise(StatType.Vit))
        {
        }

        health.TakeDamage(7);
        Assert.True(sp.TrySpend(1) || sp.Max == 0);
        var save = new SaveData { Player = SaveMapper.CapturePlayer(stats, health, sp, new Vector3(4, 0.5f, -2)) };

        var loaded = SaveSerializer.FromJson(SaveSerializer.ToJson(save));
        var (restoredStats, restoredHealth, restoredSp) = NewPlayer();
        SaveMapper.RestorePlayer(loaded.Player, restoredStats, restoredHealth, restoredSp);

        Assert.Equal(stats.Level, restoredStats.Level);
        Assert.Equal(stats.Exp, restoredStats.Exp);
        Assert.Equal(stats.StatPoints, restoredStats.StatPoints);
        Assert.Equal(stats.Base, restoredStats.Base);
        Assert.Equal(stats.Derived, restoredStats.Derived);
        Assert.Equal(health.Max, restoredHealth.Max);
        Assert.Equal(health.Current, restoredHealth.Current);
        Assert.Equal(sp.Max, restoredSp.Max);
        Assert.Equal(sp.Current, restoredSp.Current);
        Assert.Equal(new PositionSave(4, 0.5f, -2), loaded.Player.Position);
    }

    [Fact]
    public void Restoring_a_dead_player_leaves_them_alive()
    {
        var (stats, health, sp) = NewPlayer();
        var save = SaveMapper.CapturePlayer(stats, health, sp, Vector3.Zero) with { Hp = 0 };

        SaveMapper.RestorePlayer(save, stats, health, sp);

        Assert.Equal(1, health.Current);
        Assert.False(health.IsDead);
    }

    [Fact]
    public void Restoring_raises_change_events_on_the_same_objects()
    {
        var (stats, health, sp) = NewPlayer();
        var expChanged = 0;
        var hpChanged = 0;
        stats.ExpChanged += () => expChanged++;
        health.Changed += () => hpChanged++;

        SaveMapper.RestorePlayer(new PlayerSave { Level = 5, Str = 10, Agi = 10, Vit = 5, Int = 1, Dex = 10, Luk = 5, Hp = 10 }, stats, health, sp);

        Assert.Equal(5, stats.Level);
        Assert.True(expChanged > 0);
        Assert.True(hpChanged > 0);
    }

    [Fact]
    public void Out_of_range_stats_are_rejected_and_nothing_changes()
    {
        var (stats, health, sp) = NewPlayer();
        var before = stats.Base;

        Assert.Throws<InvalidDataException>(() =>
            SaveMapper.RestorePlayer(new PlayerSave { Level = 2, Str = 500 }, stats, health, sp));

        Assert.Equal(1, stats.Level);
        Assert.Equal(before, stats.Base);
    }

    [Fact]
    public void Inventory_round_trips_through_json()
    {
        var inventory = new Inventory(4, 2);
        inventory.Add(Gel, 13);
        inventory.Add(Sword, 2);
        inventory.Move(0, 7);
        var save = new SaveData { Inventory = SaveMapper.CaptureInventory(inventory) };

        var loaded = SaveSerializer.FromJson(SaveSerializer.ToJson(save));
        var restored = new Inventory(4, 2);
        var problems = SaveMapper.RestoreInventory(loaded.Inventory, restored, Items);

        Assert.Empty(problems);
        Assert.Equal(inventory.Slots, restored.Slots);
    }

    [Fact]
    public void Capture_lists_only_filled_slots()
    {
        var inventory = new Inventory(3, 3);
        inventory.Add(Gel, 3);
        inventory.Move(0, 5);

        var save = SaveMapper.CaptureInventory(inventory);

        Assert.Equal(3, save.Columns);
        Assert.Equal(3, save.Rows);
        Assert.Equal([new SlotSave(5, "slime_gel", 3)], save.Slots);
    }

    [Fact]
    public void Restore_replaces_what_was_there()
    {
        var inventory = new Inventory(2, 2);
        inventory.Add(Sword, 4);

        SaveMapper.RestoreInventory(new InventorySave { Slots = [new SlotSave(3, "slime_gel", 2)] }, inventory, Items);

        Assert.Equal(0, inventory.Count("sword"));
        Assert.Equal(new ItemStack(Gel, 2), inventory[3]);
    }

    [Fact]
    public void Unknown_items_are_dropped_and_reported()
    {
        var inventory = new Inventory(2, 2);

        var problems = SaveMapper.RestoreInventory(
            new InventorySave { Slots = [new SlotSave(0, "removed_item", 3), new SlotSave(1, "SLIME_GEL", 2)] },
            inventory,
            Items);

        Assert.Single(problems);
        Assert.Contains("removed_item", problems[0]);
        Assert.Null(inventory[0]);
        Assert.Equal(new ItemStack(Gel, 2), inventory[1]);
    }

    [Fact]
    public void Stacks_in_slots_that_no_longer_exist_move_to_free_slots()
    {
        var inventory = new Inventory(2, 1);

        var problems = SaveMapper.RestoreInventory(
            new InventorySave { Slots = [new SlotSave(1, "sword", 1), new SlotSave(30, "slime_gel", 4)] },
            inventory,
            Items);

        Assert.Empty(problems);
        Assert.Equal(new ItemStack(Gel, 4), inventory[0]);
        Assert.Equal(new ItemStack(Sword, 1), inventory[1]);
    }

    [Fact]
    public void Oversized_stacks_spill_and_what_does_not_fit_is_reported()
    {
        var inventory = new Inventory(2, 1);

        var problems = SaveMapper.RestoreInventory(
            new InventorySave { Slots = [new SlotSave(0, "slime_gel", 25)] },
            inventory,
            Items);

        Assert.Equal(new ItemStack(Gel, 10), inventory[0]);
        Assert.Equal(new ItemStack(Gel, 10), inventory[1]);
        Assert.Single(problems);
        Assert.Contains("5 × slime_gel", problems[0]);
    }

    [Fact]
    public void Dialogue_flags_round_trip_through_json()
    {
        var flags = new DialogueFlags();
        flags.Set("met_elder");
        flags.Set("quest_declined", false);
        var save = new SaveData { DialogueFlags = flags.Snapshot() };

        var restored = new DialogueFlags();
        restored.Set("stale_flag");
        restored.Load(SaveSerializer.FromJson(SaveSerializer.ToJson(save)).DialogueFlags);

        Assert.True(restored.Get("met_elder"));
        Assert.False(restored.Get("quest_declined"));
        Assert.True(restored.Has("quest_declined"));
        Assert.False(restored.Has("stale_flag"));
    }
}

using Ragna.Core.Combat;
using Ragna.Core.Hud;

namespace Ragna.Core.Tests.Hud;

public sealed class HudMessagesTests
{
    [Fact]
    public void Damage_dealt()
    {
        Assert.Equal(
            new LogMessage(MessageKind.DamageDealt, "You hit Slime for 12 damage."),
            HudMessages.DamageDealt(new AttackResult(AttackOutcome.Hit, 12), "Slime"));
        Assert.Equal(
            "Critical! You hit Slime for 30 damage.",
            HudMessages.DamageDealt(new AttackResult(AttackOutcome.Critical, 30), "Slime").Text);
        Assert.Equal("You miss Slime.", HudMessages.DamageDealt(AttackResult.Miss, "Slime").Text);
    }

    [Fact]
    public void Damage_taken()
    {
        Assert.Equal(
            new LogMessage(MessageKind.DamageTaken, "Boar hits you for 7 damage."),
            HudMessages.DamageTaken(new AttackResult(AttackOutcome.Hit, 7), "Boar"));
        Assert.Equal(
            "Critical! Boar hits you for 14 damage.",
            HudMessages.DamageTaken(new AttackResult(AttackOutcome.Critical, 14), "Boar").Text);
        Assert.Equal("Boar misses you.", HudMessages.DamageTaken(AttackResult.Miss, "Boar").Text);
    }

    [Fact]
    public void Loot()
    {
        Assert.Equal(new LogMessage(MessageKind.Loot, "Slime defeated. +12 EXP."), HudMessages.Defeated("Slime", 12));
        Assert.Equal("Slime defeated.", HudMessages.Defeated("Slime", 0).Text);
        Assert.Equal(new LogMessage(MessageKind.Loot, "Picked up Slime Gel x3."), HudMessages.PickedUp("Slime Gel", 3));
        Assert.Equal("Picked up Boar Tusk.", HudMessages.PickedUp("Boar Tusk", 1).Text);
    }

    [Fact]
    public void Level_up()
    {
        Assert.Equal(
            new LogMessage(MessageKind.LevelUp, "Level up! You are now level 3 (6 stat points to spend)."),
            HudMessages.LevelUp(3, 6));
    }

    [Fact]
    public void Death_and_revive_are_system_messages()
    {
        Assert.Equal(new LogMessage(MessageKind.System, "You were defeated by Boar."), HudMessages.Died("Boar"));
        Assert.Equal(MessageKind.System, HudMessages.Revived().Kind);
    }

    [Fact]
    public void Save_and_load_messages()
    {
        Assert.Equal(new LogMessage(MessageKind.System, "Game saved."), HudMessages.Saved());
        Assert.Equal(new LogMessage(MessageKind.System, "Game loaded."), HudMessages.Loaded());
        Assert.Equal(new LogMessage(MessageKind.System, "Couldn't save: disk full"), HudMessages.SaveFailed("disk full"));
        Assert.Equal(new LogMessage(MessageKind.System, "Couldn't load: no save file."), HudMessages.LoadFailed("no save file."));
    }
}

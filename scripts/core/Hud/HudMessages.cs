using Ragna.Core.Combat;

namespace Ragna.Core.Hud;

/// <summary>The text of every message the HUD logs, so wording is tested and kept in one place.</summary>
public static class HudMessages
{
    /// <summary>The player attacked <paramref name="target"/>.</summary>
    public static LogMessage DamageDealt(AttackResult result, string target) => result.Outcome switch
    {
        AttackOutcome.Miss => new(MessageKind.DamageDealt, $"You miss {target}."),
        AttackOutcome.Critical => new(MessageKind.DamageDealt, $"Critical! You hit {target} for {result.Damage} damage."),
        _ => new(MessageKind.DamageDealt, $"You hit {target} for {result.Damage} damage."),
    };

    /// <summary><paramref name="attacker"/> attacked the player.</summary>
    public static LogMessage DamageTaken(AttackResult result, string attacker) => result.Outcome switch
    {
        AttackOutcome.Miss => new(MessageKind.DamageTaken, $"{attacker} misses you."),
        AttackOutcome.Critical => new(MessageKind.DamageTaken, $"Critical! {attacker} hits you for {result.Damage} damage."),
        _ => new(MessageKind.DamageTaken, $"{attacker} hits you for {result.Damage} damage."),
    };

    /// <summary>The player killed <paramref name="monster"/> and got <paramref name="exp"/> EXP.</summary>
    public static LogMessage Defeated(string monster, long exp) =>
        new(MessageKind.Loot, exp > 0 ? $"{monster} defeated. +{exp} EXP." : $"{monster} defeated.");

    /// <summary>The player picked up <paramref name="quantity"/> of <paramref name="item"/>.</summary>
    public static LogMessage PickedUp(string item, int quantity) =>
        new(MessageKind.Loot, quantity > 1 ? $"Picked up {item} x{quantity}." : $"Picked up {item}.");

    /// <summary>The player reached <paramref name="level"/> and now has <paramref name="statPoints"/> unspent.</summary>
    public static LogMessage LevelUp(int level, int statPoints) =>
        new(MessageKind.LevelUp, $"Level up! You are now level {level} ({statPoints} stat points to spend).");

    public static LogMessage Died(string killer) => new(MessageKind.System, $"You were defeated by {killer}.");

    public static LogMessage Revived() => new(MessageKind.System, "You are back on your feet.");
}

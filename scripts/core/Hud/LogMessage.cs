namespace Ragna.Core.Hud;

/// <summary>What a <see cref="LogMessage"/> is about. The HUD colours lines by kind.</summary>
public enum MessageKind
{
    /// <summary>Anything else, for example dying and coming back.</summary>
    System,

    /// <summary>Damage the player deals, or a miss.</summary>
    DamageDealt,

    /// <summary>Damage the player takes, or a monster's miss.</summary>
    DamageTaken,

    /// <summary>Kills, EXP and items picked up.</summary>
    Loot,

    /// <summary>The player reached a new level.</summary>
    LevelUp,
}

/// <summary>One line in the <see cref="MessageLog"/>.</summary>
public readonly record struct LogMessage(MessageKind Kind, string Text);

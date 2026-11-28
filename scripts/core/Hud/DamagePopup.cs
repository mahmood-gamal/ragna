using System.Globalization;
using Ragna.Core.Combat;

namespace Ragna.Core.Hud;

/// <summary>How a floating damage number looks. The game picks a colour for each.</summary>
public enum PopupStyle
{
    /// <summary>Damage the player deals to a monster.</summary>
    DamageDealt,

    /// <summary>A critical hit the player lands on a monster.</summary>
    Critical,

    /// <summary>Damage a monster deals to the player.</summary>
    DamageTaken,

    /// <summary>An attack that missed, either way.</summary>
    Miss,
}

/// <summary>What one floating damage number says and how big it is.</summary>
/// <param name="Style">Picks the colour.</param>
/// <param name="Text">The number, or <c>Miss</c>.</param>
/// <param name="Scale">Size relative to a normal hit. Critical hits are bigger.</param>
public readonly record struct DamagePopup(PopupStyle Style, string Text, float Scale)
{
    /// <summary>Size of a critical hit's number, relative to a normal hit.</summary>
    public const float CriticalScale = 1.5f;

    public const string MissText = "Miss";

    /// <summary>The number shown over a monster the player attacked.</summary>
    public static DamagePopup OnTarget(AttackResult result) => result.Outcome switch
    {
        AttackOutcome.Miss => new(PopupStyle.Miss, MissText, 1f),
        AttackOutcome.Critical => new(PopupStyle.Critical, Number(result), CriticalScale),
        _ => new(PopupStyle.DamageDealt, Number(result), 1f),
    };

    /// <summary>The number shown over the player when a monster attacks.</summary>
    public static DamagePopup OnPlayer(AttackResult result) => result.Outcome switch
    {
        AttackOutcome.Miss => new(PopupStyle.Miss, MissText, 1f),
        AttackOutcome.Critical => new(PopupStyle.DamageTaken, Number(result), CriticalScale),
        _ => new(PopupStyle.DamageTaken, Number(result), 1f),
    };

    private static string Number(AttackResult result) => result.Damage.ToString(CultureInfo.InvariantCulture);
}

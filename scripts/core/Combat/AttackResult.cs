namespace Ragna.Core.Combat;

public enum AttackOutcome
{
    Miss,
    Hit,
    Critical,
}

/// <summary>The result of one attack.</summary>
/// <param name="Outcome">Whether it missed, hit or was a critical hit.</param>
/// <param name="Damage">Damage dealt. 0 for a miss.</param>
public readonly record struct AttackResult(AttackOutcome Outcome, int Damage)
{
    public static AttackResult Miss { get; } = new(AttackOutcome.Miss, 0);

    public bool Landed => Outcome != AttackOutcome.Miss;
}

namespace Ragna.Core.Monsters;

/// <summary>AI tuning for <see cref="MonsterBrain"/>. Distances are in metres, times in seconds.</summary>
public sealed record MonsterBehavior
{
    /// <summary>
    /// Aggressive monsters attack a player who comes within <see cref="AggroRadius"/>. Passive ones
    /// only fight back when attacked.
    /// </summary>
    public bool Aggressive { get; init; }

    /// <summary>How close the player must come before an aggressive monster attacks.</summary>
    public float AggroRadius { get; init; } = 5f;

    /// <summary>Ground distance between centres at which the monster stops and attacks.</summary>
    public float AttackRange { get; init; } = 1.3f;

    /// <summary>How far from its spawn point the monster wanders while idle.</summary>
    public float WanderRadius { get; init; } = 4f;

    /// <summary>
    /// How far from its spawn point the monster follows a target. Past this, or when the target
    /// is this far from the spawn point, it gives up and walks home.
    /// </summary>
    public float LeashRadius { get; init; } = 12f;

    /// <summary>Shortest pause between two wanders.</summary>
    public float IdleTimeMin { get; init; } = 2f;

    /// <summary>Longest pause between two wanders.</summary>
    public float IdleTimeMax { get; init; } = 5f;

    /// <summary>A wander that hasn't arrived after this long ends, in case the point can't be reached.</summary>
    public float WanderTimeout { get; init; } = 6f;

    /// <summary>How close counts as arriving at a wander point or back home.</summary>
    public float ArriveDistance { get; init; } = 0.5f;

    /// <summary>Whether the monster gets its HP back when it reaches home after leashing.</summary>
    public bool HealOnReturn { get; init; } = true;

    /// <summary>Throws <see cref="ArgumentException"/> if any value is out of range.</summary>
    public void Validate()
    {
        Check(AggroRadius >= 0f, "aggroRadius can't be negative.");
        Check(AttackRange > 0f, "attackRange must be above 0.");
        Check(WanderRadius >= 0f, "wanderRadius can't be negative.");
        Check(LeashRadius > 0f, "leashRadius must be above 0.");
        Check(LeashRadius >= WanderRadius, "leashRadius must be at least wanderRadius.");
        Check(IdleTimeMin >= 0f && IdleTimeMax >= IdleTimeMin, "idleTimeMin must be 0 or more and at most idleTimeMax.");
        Check(WanderTimeout > 0f, "wanderTimeout must be above 0.");
        Check(ArriveDistance > 0f, "arriveDistance must be above 0.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new ArgumentException(message);
        }
    }
}

using System.Numerics;

namespace Ragna.Core.Combat;

/// <summary>Tuning for <see cref="AutoAttack"/>. Distances are in metres, times in seconds.</summary>
public sealed record AutoAttackSettings
{
    /// <summary>
    /// Ground distance between the attacker's and the target's centres at which the attacker stops
    /// and swings. Melee is about 1.5 m.
    /// </summary>
    public float Range { get; init; } = 1.5f;

    /// <summary>While chasing, the shortest time between two new paths.</summary>
    public float RepathInterval { get; init; } = 0.25f;

    /// <summary>While chasing, how far the target must move before a new path is asked for.</summary>
    public float RetargetDistance { get; init; } = 0.5f;
}

public enum AutoAttackAction
{
    /// <summary>No target: do nothing.</summary>
    None,

    /// <summary>Out of range: walk towards the target.</summary>
    Chase,

    /// <summary>In range, waiting for the next swing: stand still and face the target.</summary>
    Wait,

    /// <summary>In range and ready: stand still, face the target and attack now.</summary>
    Attack,
}

/// <summary>What the attacker should do this frame.</summary>
/// <param name="Action">What to do.</param>
/// <param name="Repath">With <see cref="AutoAttackAction.Chase"/>: ask for a new path to the target.</param>
public readonly record struct AutoAttackStep(AutoAttackAction Action, bool Repath = false);

/// <summary>
/// Ragnarok-style auto-attack: pick a target, walk into range, then attack it every attack delay
/// until it dies or another order is given. The engine node calls <see cref="Tick"/> every physics
/// frame and acts on the step it returns.
/// </summary>
/// <remarks>
/// The attack cooldown keeps running when the target changes or is dropped, so clicking between
/// targets never attacks faster than the attack speed allows.
/// </remarks>
public sealed class AutoAttack
{
    private float _sinceRepath;
    private Vector3? _chasePoint;

    public AutoAttack(AutoAttackSettings? settings = null)
    {
        Settings = settings ?? new AutoAttackSettings();
        if (Settings.Range <= 0f)
        {
            throw new ArgumentException("Range must be above 0.", nameof(settings));
        }

        if (Settings.RepathInterval < 0f || Settings.RetargetDistance < 0f)
        {
            throw new ArgumentException("RepathInterval and RetargetDistance can't be negative.", nameof(settings));
        }
    }

    public AutoAttackSettings Settings { get; }

    /// <summary>Whether there is a target to attack.</summary>
    public bool IsEngaged { get; private set; }

    /// <summary>Seconds until the next attack may start. 0 when ready.</summary>
    public double Cooldown { get; private set; }

    /// <summary>Starts attacking a new target, or the same one again. The next <see cref="Tick"/> repaths.</summary>
    public void Engage()
    {
        IsEngaged = true;
        _chasePoint = null;
        _sinceRepath = 0f;
    }

    /// <summary>Stops attacking, for example when the player clicks the ground.</summary>
    public void Disengage()
    {
        IsEngaged = false;
        _chasePoint = null;
    }

    /// <summary>
    /// Advances by <paramref name="delta"/> seconds and says what to do. <paramref name="attackDelay"/>
    /// is the attacker's seconds between attacks (<see cref="Stats.DerivedStats.AttackDelay"/>). A dead
    /// target ends the engagement. Distance is measured on the ground plane, so height is ignored.
    /// </summary>
    public AutoAttackStep Tick(float delta, Vector3 attacker, Vector3 target, bool targetAlive, double attackDelay)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(delta);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attackDelay);

        Cooldown = Math.Max(0, Cooldown - delta);
        if (!IsEngaged)
        {
            return new AutoAttackStep(AutoAttackAction.None);
        }

        if (!targetAlive)
        {
            Disengage();
            return new AutoAttackStep(AutoAttackAction.None);
        }

        if (!InRange(attacker, target))
        {
            _sinceRepath += delta;
            var repath = _chasePoint is not { } last
                || (_sinceRepath >= Settings.RepathInterval && Vector3.Distance(last, target) >= Settings.RetargetDistance);
            if (repath)
            {
                _chasePoint = target;
                _sinceRepath = 0f;
            }

            return new AutoAttackStep(AutoAttackAction.Chase, repath);
        }

        // Next time the target gets away, ask for a path straight away.
        _chasePoint = null;
        if (Cooldown > 0)
        {
            return new AutoAttackStep(AutoAttackAction.Wait);
        }

        Cooldown = attackDelay;
        return new AutoAttackStep(AutoAttackAction.Attack);
    }

    /// <summary>Whether <paramref name="target"/> is within <see cref="AutoAttackSettings.Range"/> on the ground plane.</summary>
    public bool InRange(Vector3 attacker, Vector3 target)
    {
        var dx = target.X - attacker.X;
        var dz = target.Z - attacker.Z;
        return (dx * dx) + (dz * dz) <= Settings.Range * Settings.Range;
    }
}

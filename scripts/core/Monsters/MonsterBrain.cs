using System.Numerics;
using Ragna.Core.Combat;

namespace Ragna.Core.Monsters;

public enum MonsterState
{
    /// <summary>Standing at or near home, waiting for the next wander.</summary>
    Idle,

    /// <summary>Walking to a random point near home.</summary>
    Wander,

    /// <summary>Running after a target that is out of attack range.</summary>
    Chase,

    /// <summary>In range of a target, attacking it every attack delay.</summary>
    Attack,

    /// <summary>Gave up on a target and walking back home. Ignores the player until it arrives.</summary>
    Return,

    /// <summary>Dead. Does nothing until <see cref="MonsterBrain.Respawn"/>.</summary>
    Dead,
}

public enum MonsterAction
{
    /// <summary>Stand still.</summary>
    Stand,

    /// <summary>Walk or run to <see cref="MonsterStep.Destination"/>.</summary>
    Move,

    /// <summary>Stand still, face the target and attack it now.</summary>
    Attack,
}

/// <summary>What the monster should do this frame.</summary>
/// <param name="Action">What to do.</param>
/// <param name="Destination">With <see cref="MonsterAction.Move"/>: where to go.</param>
/// <param name="Repath">With <see cref="MonsterAction.Move"/>: ask for a new path to <paramref name="Destination"/>.</param>
/// <param name="Run">With <see cref="MonsterAction.Move"/>: chase speed instead of walking speed.</param>
/// <param name="FaceTarget">Turn towards the target (while chasing, waiting to swing or attacking).</param>
public readonly record struct MonsterStep(
    MonsterAction Action,
    Vector3 Destination = default,
    bool Repath = false,
    bool Run = false,
    bool FaceTarget = false);

/// <summary>The player as a monster sees it.</summary>
/// <param name="Position">Where the player stands.</param>
/// <param name="Alive">Whether the player can still be attacked.</param>
public readonly record struct MonsterTarget(Vector3 Position, bool Alive);

/// <summary>
/// Monster AI: idle and wander around home, aggro on a nearby player (aggressive types) or when hit
/// (all types), chase into range and attack every attack delay, and leash back home when the target
/// dies or gets too far from home. The engine node calls <see cref="Tick"/> every physics frame and
/// acts on the step it returns. All distances are measured on the ground plane, so height is ignored.
/// </summary>
public sealed class MonsterBrain
{
    private readonly IRandomSource _random;
    private readonly AutoAttack _attack;
    private float _timer;
    private Vector3 _wanderPoint;
    private bool _provoked;
    private bool _pathRequested;

    public MonsterBrain(MonsterBehavior behavior, double attackDelay, Vector3 home, IRandomSource? random = null)
    {
        ArgumentNullException.ThrowIfNull(behavior);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attackDelay);
        behavior.Validate();
        Behavior = behavior;
        AttackDelay = attackDelay;
        Home = home;
        _random = random ?? SystemRandomSource.Shared;
        _attack = new AutoAttack(new AutoAttackSettings { Range = behavior.AttackRange });
        GoIdle();
    }

    /// <summary>Raised when the monster gets back home after leashing. Heal it here if <see cref="MonsterBehavior.HealOnReturn"/>.</summary>
    public event Action? ReturnedHome;

    /// <summary>Raised when the state changes, with the old and new state.</summary>
    public event Action<MonsterState, MonsterState>? StateChanged;

    public MonsterBehavior Behavior { get; }

    public double AttackDelay { get; }

    /// <summary>The spawn point the monster wanders around and leashes back to.</summary>
    public Vector3 Home { get; private set; }

    public MonsterState State { get; private set; }

    /// <summary>Whether the monster is chasing or attacking a target.</summary>
    public bool IsEngaged => State is MonsterState.Chase or MonsterState.Attack;

    /// <summary>Seconds until the next attack may start. 0 when ready.</summary>
    public double Cooldown => _attack.Cooldown;

    /// <summary>The point the monster is wandering to, while <see cref="MonsterState.Wander"/>.</summary>
    public Vector3? WanderPoint => State == MonsterState.Wander ? _wanderPoint : null;

    /// <summary>The monster was hit by the player: fight back on the next <see cref="Tick"/>. Ignored while dead or going home.</summary>
    public void Provoke()
    {
        if (State is not (MonsterState.Dead or MonsterState.Return))
        {
            _provoked = true;
        }
    }

    /// <summary>
    /// The engine reached the end of the current path, or found no path. Ends a wander early, and
    /// counts as being home while going home (the path gets as close as the navmesh allows).
    /// </summary>
    public void NotifyArrived()
    {
        switch (State)
        {
            case MonsterState.Wander:
                GoIdle();
                break;
            case MonsterState.Return:
                ArriveHome();
                break;
        }
    }

    public void Die()
    {
        _attack.Disengage();
        _provoked = false;
        SetState(MonsterState.Dead);
    }

    /// <summary>Back to life at <paramref name="home"/>, idle.</summary>
    public void Respawn(Vector3 home)
    {
        Home = home;
        _provoked = false;
        _attack.Disengage();
        GoIdle();
    }

    /// <summary>
    /// Advances by <paramref name="delta"/> seconds and says what to do. <paramref name="self"/> is where
    /// the monster stands; <paramref name="target"/> is the player, or <c>null</c> when there is none.
    /// </summary>
    public MonsterStep Tick(float delta, Vector3 self, MonsterTarget? target)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(delta);
        if (State == MonsterState.Dead)
        {
            return new MonsterStep(MonsterAction.Stand);
        }

        // Fighting or not, the attack timer keeps running.
        if (!IsEngaged)
        {
            _attack.Tick(delta, self, self, false, AttackDelay);
        }

        switch (State)
        {
            case MonsterState.Return:
                return TickReturn(self);
            case MonsterState.Chase or MonsterState.Attack:
                return TickEngaged(delta, self, target);
        }

        if (ShouldAggro(self, target))
        {
            _provoked = false;
            _attack.Engage();
            SetState(MonsterState.Chase);
            return TickEngaged(0f, self, target);
        }

        _provoked = false;
        return State == MonsterState.Wander ? TickWander(delta, self) : TickIdle(delta, self);
    }

    private bool ShouldAggro(Vector3 self, MonsterTarget? target)
    {
        if (target is not { Alive: true } t || GroundDistance(t.Position, Home) > Behavior.LeashRadius)
        {
            return false;
        }

        return _provoked || (Behavior.Aggressive && GroundDistance(self, t.Position) <= Behavior.AggroRadius);
    }

    private MonsterStep TickIdle(float delta, Vector3 self)
    {
        _timer -= delta;
        if (_timer > 0f || Behavior.WanderRadius <= 0f)
        {
            return new MonsterStep(MonsterAction.Stand);
        }

        _wanderPoint = RandomPointNearHome();
        _timer = Behavior.WanderTimeout;
        SetState(MonsterState.Wander);
        return TickWander(0f, self);
    }

    private MonsterStep TickWander(float delta, Vector3 self)
    {
        _timer -= delta;
        if (_timer <= 0f || GroundDistance(self, _wanderPoint) <= Behavior.ArriveDistance)
        {
            GoIdle();
            return new MonsterStep(MonsterAction.Stand);
        }

        return Move(_wanderPoint, run: false);
    }

    private MonsterStep TickEngaged(float delta, Vector3 self, MonsterTarget? target)
    {
        if (target is not { Alive: true } t
            || GroundDistance(self, Home) > Behavior.LeashRadius
            || GroundDistance(t.Position, Home) > Behavior.LeashRadius)
        {
            _attack.Disengage();
            _pathRequested = false;
            SetState(MonsterState.Return);
            return TickReturn(self);
        }

        var step = _attack.Tick(delta, self, t.Position, true, AttackDelay);
        switch (step.Action)
        {
            case AutoAttackAction.Chase:
                SetState(MonsterState.Chase);
                return new MonsterStep(MonsterAction.Move, t.Position, step.Repath, Run: true, FaceTarget: true);
            case AutoAttackAction.Attack:
                SetState(MonsterState.Attack);
                return new MonsterStep(MonsterAction.Attack, FaceTarget: true);
            default:
                SetState(MonsterState.Attack);
                return new MonsterStep(MonsterAction.Stand, FaceTarget: true);
        }
    }

    private MonsterStep TickReturn(Vector3 self)
    {
        if (GroundDistance(self, Home) <= Behavior.ArriveDistance)
        {
            ArriveHome();
            return new MonsterStep(MonsterAction.Stand);
        }

        return Move(Home, run: false);
    }

    private void ArriveHome()
    {
        GoIdle();
        ReturnedHome?.Invoke();
    }

    /// <summary>A move that asks for a path only the first time after a state change.</summary>
    private MonsterStep Move(Vector3 destination, bool run)
    {
        var repath = !_pathRequested;
        _pathRequested = true;
        return new MonsterStep(MonsterAction.Move, destination, repath, run);
    }

    private void GoIdle()
    {
        _timer = Behavior.IdleTimeMin + ((float)_random.NextDouble() * (Behavior.IdleTimeMax - Behavior.IdleTimeMin));
        SetState(MonsterState.Idle);
    }

    private Vector3 RandomPointNearHome()
    {
        // Square root keeps the points evenly spread over the disc instead of bunched at the centre.
        var angle = _random.NextDouble() * Math.Tau;
        var distance = Behavior.WanderRadius * Math.Sqrt(_random.NextDouble());
        return Home + new Vector3((float)(Math.Cos(angle) * distance), 0f, (float)(Math.Sin(angle) * distance));
    }

    private void SetState(MonsterState state)
    {
        if (State == state)
        {
            return;
        }

        var old = State;
        State = state;
        _pathRequested = false;
        StateChanged?.Invoke(old, state);
    }

    private static float GroundDistance(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dz = a.Z - b.Z;
        return MathF.Sqrt((dx * dx) + (dz * dz));
    }
}

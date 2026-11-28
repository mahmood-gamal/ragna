using System.Numerics;
using Ragna.Core.Monsters;
using Ragna.Core.Tests.Combat;

namespace Ragna.Core.Tests.Monsters;

public sealed class MonsterBrainTests
{
    private const double Delay = 1.0;

    private static readonly Vector3 Home = Vector3.Zero;

    private static readonly MonsterBehavior Passive = new()
    {
        Aggressive = false,
        AggroRadius = 5f,
        AttackRange = 1.5f,
        WanderRadius = 4f,
        LeashRadius = 10f,
        IdleTimeMin = 2f,
        IdleTimeMax = 2f,
        WanderTimeout = 6f,
        ArriveDistance = 0.5f,
    };

    private static readonly MonsterBehavior Aggressive = Passive with { Aggressive = true };

    private static MonsterBrain Brain(MonsterBehavior? behavior = null, params double[] rolls) =>
        new(behavior ?? Passive, Delay, Home, new FixedRandom(rolls.Length == 0 ? [0.5] : rolls));

    private static MonsterTarget Player(float x, float z = 0f, bool alive = true) => new(new Vector3(x, 0f, z), alive);

    [Fact]
    public void Starts_idle_and_stands_still()
    {
        var brain = Brain();

        Assert.Equal(MonsterState.Idle, brain.State);
        Assert.Equal(MonsterAction.Stand, brain.Tick(0.5f, Home, null).Action);
    }

    [Fact]
    public void Wanders_to_a_point_inside_the_wander_radius_after_idling()
    {
        var brain = Brain();

        var step = brain.Tick(2.1f, Home, null);

        Assert.Equal(MonsterState.Wander, brain.State);
        Assert.Equal(MonsterAction.Move, step.Action);
        Assert.True(step.Repath);
        Assert.False(step.Run);
        Assert.True(Vector3.Distance(Home, step.Destination) <= Passive.WanderRadius + 0.001f);
        Assert.Equal(0f, step.Destination.Y);
    }

    [Fact]
    public void Wander_asks_for_a_path_only_once()
    {
        var brain = Brain();
        var first = brain.Tick(2.1f, Home, null);

        var second = brain.Tick(0.1f, Home, null);

        Assert.Equal(MonsterAction.Move, second.Action);
        Assert.Equal(first.Destination, second.Destination);
        Assert.False(second.Repath);
    }

    [Fact]
    public void Goes_idle_on_reaching_the_wander_point()
    {
        var brain = Brain();
        var point = brain.Tick(2.1f, Home, null).Destination;

        var step = brain.Tick(0.1f, point, null);

        Assert.Equal(MonsterAction.Stand, step.Action);
        Assert.Equal(MonsterState.Idle, brain.State);
    }

    [Fact]
    public void Goes_idle_when_the_engine_says_the_path_ended()
    {
        var brain = Brain();
        brain.Tick(2.1f, Home, null);

        brain.NotifyArrived();

        Assert.Equal(MonsterState.Idle, brain.State);
    }

    [Fact]
    public void Gives_up_a_wander_that_takes_too_long()
    {
        var brain = Brain();
        brain.Tick(2.1f, Home, null);

        brain.Tick(Passive.WanderTimeout + 0.1f, Home, null);

        Assert.Equal(MonsterState.Idle, brain.State);
    }

    [Fact]
    public void Never_wanders_with_a_zero_wander_radius()
    {
        var brain = Brain(Passive with { WanderRadius = 0f });

        Assert.Equal(MonsterAction.Stand, brain.Tick(100f, Home, null).Action);
        Assert.Equal(MonsterState.Idle, brain.State);
    }

    [Fact]
    public void Passive_monster_ignores_a_nearby_player()
    {
        var brain = Brain();

        brain.Tick(0.1f, Home, Player(1f));

        Assert.Equal(MonsterState.Idle, brain.State);
    }

    [Fact]
    public void Aggressive_monster_chases_a_player_inside_the_aggro_radius()
    {
        var brain = Brain(Aggressive);

        var step = brain.Tick(0.1f, Home, Player(4f));

        Assert.Equal(MonsterState.Chase, brain.State);
        Assert.Equal(new MonsterStep(MonsterAction.Move, new Vector3(4f, 0f, 0f), Repath: true, Run: true, FaceTarget: true), step);
    }

    [Fact]
    public void Aggressive_monster_ignores_a_player_outside_the_aggro_radius()
    {
        var brain = Brain(Aggressive);

        brain.Tick(0.1f, Home, Player(6f));

        Assert.Equal(MonsterState.Idle, brain.State);
    }

    [Fact]
    public void Aggressive_monster_ignores_a_dead_player()
    {
        var brain = Brain(Aggressive);

        brain.Tick(0.1f, Home, Player(1f, alive: false));

        Assert.Equal(MonsterState.Idle, brain.State);
    }

    [Fact]
    public void Height_does_not_count_towards_the_aggro_radius()
    {
        var brain = Brain(Aggressive);

        brain.Tick(0.1f, Home, new MonsterTarget(new Vector3(4f, 20f, 0f), true));

        Assert.Equal(MonsterState.Chase, brain.State);
    }

    [Fact]
    public void Passive_monster_fights_back_when_provoked()
    {
        var brain = Brain();

        brain.Provoke();
        brain.Tick(0.1f, Home, Player(8f));

        Assert.Equal(MonsterState.Chase, brain.State);
    }

    [Fact]
    public void Provoked_while_wandering_fights_back()
    {
        var brain = Brain();
        brain.Tick(2.1f, Home, null);

        brain.Provoke();
        brain.Tick(0.1f, Home, Player(3f));

        Assert.Equal(MonsterState.Chase, brain.State);
    }

    [Fact]
    public void Attacks_at_once_in_range_then_every_attack_delay()
    {
        var brain = Brain(Aggressive);

        Assert.Equal(MonsterAction.Attack, brain.Tick(0.1f, Home, Player(1f)).Action);
        Assert.Equal(MonsterState.Attack, brain.State);

        var wait = brain.Tick(0.5f, Home, Player(1f));
        Assert.Equal(MonsterAction.Stand, wait.Action);
        Assert.True(wait.FaceTarget);

        Assert.Equal(MonsterAction.Attack, brain.Tick(0.5f, Home, Player(1f)).Action);
    }

    [Fact]
    public void Chases_again_when_the_target_steps_out_of_range()
    {
        var brain = Brain(Aggressive);
        brain.Tick(0.1f, Home, Player(1f));

        var step = brain.Tick(0.1f, Home, Player(3f));

        Assert.Equal(MonsterState.Chase, brain.State);
        Assert.Equal(MonsterAction.Move, step.Action);
        Assert.True(step.Repath);
    }

    [Fact]
    public void Leashes_home_when_the_target_dies()
    {
        var brain = Brain(Aggressive);
        var self = new Vector3(3f, 0f, 0f);
        brain.Tick(0.1f, self, Player(4f));

        var step = brain.Tick(0.1f, self, Player(4f, alive: false));

        Assert.Equal(MonsterState.Return, brain.State);
        Assert.Equal(new MonsterStep(MonsterAction.Move, Home, Repath: true), step);
    }

    [Fact]
    public void Leashes_home_when_the_target_leaves()
    {
        var brain = Brain(Aggressive);
        brain.Tick(0.1f, Home, Player(4f));

        brain.Tick(0.1f, new Vector3(5f, 0f, 0f), null);

        Assert.Equal(MonsterState.Return, brain.State);
    }

    [Fact]
    public void Leashes_home_when_chased_past_the_leash_radius()
    {
        var brain = Brain(Aggressive);
        brain.Tick(0.1f, Home, Player(4f));

        brain.Tick(0.1f, new Vector3(10.5f, 0f, 0f), Player(9f));

        Assert.Equal(MonsterState.Return, brain.State);
    }

    [Fact]
    public void Leashes_home_when_the_target_runs_past_the_leash_radius()
    {
        var brain = Brain(Aggressive);
        brain.Tick(0.1f, Home, Player(4f));

        brain.Tick(0.1f, new Vector3(8f, 0f, 0f), Player(11f));

        Assert.Equal(MonsterState.Return, brain.State);
    }

    [Fact]
    public void Ignores_the_player_while_going_home()
    {
        var brain = Brain(Aggressive);
        var self = new Vector3(8f, 0f, 0f);
        brain.Tick(0.1f, Home, Player(4f));
        brain.Tick(0.1f, self, Player(11f));

        brain.Provoke();
        var step = brain.Tick(0.1f, self, Player(8.5f));

        Assert.Equal(MonsterState.Return, brain.State);
        Assert.Equal(MonsterAction.Move, step.Action);
        Assert.False(step.Repath);
    }

    [Fact]
    public void Arriving_home_goes_idle_and_raises_returned_home()
    {
        var brain = Brain(Aggressive);
        var returned = 0;
        brain.ReturnedHome += () => returned++;
        brain.Tick(0.1f, Home, Player(4f));
        brain.Tick(0.1f, new Vector3(3f, 0f, 0f), null);

        var step = brain.Tick(0.1f, new Vector3(0.2f, 0f, 0f), null);

        Assert.Equal(MonsterAction.Stand, step.Action);
        Assert.Equal(MonsterState.Idle, brain.State);
        Assert.Equal(1, returned);
    }

    [Fact]
    public void Path_ending_while_going_home_counts_as_home()
    {
        var brain = Brain(Aggressive);
        var returned = 0;
        brain.ReturnedHome += () => returned++;
        brain.Tick(0.1f, Home, Player(4f));
        brain.Tick(0.1f, new Vector3(3f, 0f, 0f), null);

        brain.NotifyArrived();

        Assert.Equal(MonsterState.Idle, brain.State);
        Assert.Equal(1, returned);
    }

    [Fact]
    public void Path_ending_while_chasing_is_ignored()
    {
        var brain = Brain(Aggressive);
        brain.Tick(0.1f, Home, Player(4f));

        brain.NotifyArrived();

        Assert.Equal(MonsterState.Chase, brain.State);
    }

    [Fact]
    public void Aggros_again_once_home()
    {
        var brain = Brain(Aggressive);
        brain.Tick(0.1f, Home, Player(4f));
        brain.Tick(0.1f, new Vector3(3f, 0f, 0f), null);
        brain.Tick(0.1f, Home, null);

        brain.Tick(0.1f, Home, Player(4f));

        Assert.Equal(MonsterState.Chase, brain.State);
    }

    [Fact]
    public void Does_not_aggro_on_a_player_past_the_leash_radius_from_home()
    {
        var brain = Brain(Aggressive);
        var self = new Vector3(8f, 0f, 0f);

        brain.Tick(0.1f, self, Player(12f));

        Assert.Equal(MonsterState.Idle, brain.State);
    }

    [Fact]
    public void Attack_cooldown_keeps_running_while_not_fighting()
    {
        var brain = Brain(Aggressive);
        brain.Tick(0.1f, Home, Player(1f));
        brain.Tick(0.1f, Home, Player(1f, alive: false));

        brain.Tick(0.4f, new Vector3(0.1f, 0f, 0f), null);

        Assert.Equal(0.6, brain.Cooldown, 3);
    }

    [Fact]
    public void Dead_monster_does_nothing()
    {
        var brain = Brain(Aggressive);
        brain.Die();

        Assert.Equal(MonsterAction.Stand, brain.Tick(0.1f, Home, Player(1f)).Action);
        Assert.Equal(MonsterState.Dead, brain.State);
    }

    [Fact]
    public void Provoke_is_ignored_while_dead_and_cleared_by_respawn()
    {
        var brain = Brain();
        brain.Die();
        brain.Provoke();

        var newHome = new Vector3(20f, 0f, 0f);
        brain.Respawn(newHome);
        brain.Tick(0.1f, newHome, Player(21f));

        Assert.Equal(MonsterState.Idle, brain.State);
        Assert.Equal(newHome, brain.Home);
    }

    [Fact]
    public void Raises_state_changed()
    {
        var brain = Brain(Aggressive);
        var changes = new List<(MonsterState, MonsterState)>();
        brain.StateChanged += (from, to) => changes.Add((from, to));

        brain.Tick(0.1f, Home, Player(1f));

        Assert.Equal([(MonsterState.Idle, MonsterState.Chase), (MonsterState.Chase, MonsterState.Attack)], changes);
    }

    [Fact]
    public void Rejects_bad_settings()
    {
        Assert.Throws<ArgumentException>(() => new MonsterBrain(Passive with { LeashRadius = 2f }, Delay, Home));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonsterBrain(Passive, 0, Home));
    }
}

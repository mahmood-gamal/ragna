using System.Numerics;
using Ragna.Core.Combat;

namespace Ragna.Core.Tests.Combat;

public sealed class AutoAttackTests
{
    private const double Delay = 0.5;

    private static readonly Vector3 Origin = Vector3.Zero;
    private static readonly Vector3 Near = new(1f, 0f, 0f);
    private static readonly Vector3 Far = new(10f, 0f, 0f);

    private static AutoAttack Engaged(AutoAttackSettings? settings = null)
    {
        var attack = new AutoAttack(settings ?? new AutoAttackSettings { Range = 1.5f });
        attack.Engage();
        return attack;
    }

    [Fact]
    public void Does_nothing_without_a_target()
    {
        var attack = new AutoAttack();

        Assert.Equal(AutoAttackAction.None, attack.Tick(0.1f, Origin, Near, true, Delay).Action);
    }

    [Fact]
    public void Out_of_range_chases_and_asks_for_a_path_at_once()
    {
        var attack = Engaged();

        Assert.Equal(new AutoAttackStep(AutoAttackAction.Chase, Repath: true), attack.Tick(0f, Origin, Far, true, Delay));
    }

    [Fact]
    public void Chasing_a_still_target_does_not_repath()
    {
        var attack = Engaged();
        attack.Tick(0f, Origin, Far, true, Delay);

        Assert.False(attack.Tick(1f, Origin, Far, true, Delay).Repath);
    }

    [Fact]
    public void Chasing_a_moving_target_repaths_after_the_interval()
    {
        var attack = Engaged(new AutoAttackSettings { RepathInterval = 0.25f, RetargetDistance = 0.5f });
        attack.Tick(0f, Origin, Far, true, Delay);
        var moved = Far + new Vector3(0f, 0f, 2f);

        Assert.False(attack.Tick(0.1f, Origin, moved, true, Delay).Repath);
        Assert.True(attack.Tick(0.2f, Origin, moved, true, Delay).Repath);
    }

    [Fact]
    public void In_range_attacks_at_once_then_waits_for_the_delay()
    {
        var attack = Engaged();

        Assert.Equal(AutoAttackAction.Attack, attack.Tick(0f, Origin, Near, true, Delay).Action);
        Assert.Equal(AutoAttackAction.Wait, attack.Tick(0.3f, Origin, Near, true, Delay).Action);
        Assert.Equal(AutoAttackAction.Attack, attack.Tick(0.2f, Origin, Near, true, Delay).Action);
    }

    [Fact]
    public void Attack_speed_sets_the_number_of_attacks()
    {
        var attack = Engaged();
        var attacks = 0;

        // 10 seconds at 0.5 s per attack, in 60 fps steps.
        for (var i = 0; i < 600; i++)
        {
            if (attack.Tick(1f / 60f, Origin, Near, true, Delay).Action == AutoAttackAction.Attack)
            {
                attacks++;
            }
        }

        Assert.InRange(attacks, 20, 21);
    }

    [Fact]
    public void Range_ignores_height()
    {
        var attack = Engaged();

        Assert.True(attack.InRange(Origin, new Vector3(1f, 5f, 0f)));
        Assert.False(attack.InRange(Origin, new Vector3(1.2f, 0f, 1.2f)));
    }

    [Fact]
    public void A_dead_target_ends_the_engagement()
    {
        var attack = Engaged();

        Assert.Equal(AutoAttackAction.None, attack.Tick(0f, Origin, Near, false, Delay).Action);
        Assert.False(attack.IsEngaged);
    }

    [Fact]
    public void Disengage_stops_attacking()
    {
        var attack = Engaged();
        attack.Disengage();

        Assert.Equal(AutoAttackAction.None, attack.Tick(0f, Origin, Near, true, Delay).Action);
    }

    [Fact]
    public void Switching_targets_does_not_reset_the_cooldown()
    {
        var attack = Engaged();
        attack.Tick(0f, Origin, Near, true, Delay);

        attack.Engage();

        Assert.Equal(AutoAttackAction.Wait, attack.Tick(0.1f, Origin, Near, true, Delay).Action);
        Assert.Equal(0.4, attack.Cooldown, 6);
    }

    [Fact]
    public void Cooldown_runs_while_chasing()
    {
        var attack = Engaged();
        attack.Tick(0f, Origin, Near, true, Delay);

        attack.Tick(0.5f, Origin, Far, true, Delay);

        Assert.Equal(AutoAttackAction.Attack, attack.Tick(0f, Origin, Near, true, Delay).Action);
    }

    [Fact]
    public void Leaving_range_asks_for_a_path_at_once()
    {
        var attack = Engaged();
        attack.Tick(0f, Origin, Far, true, Delay);
        attack.Tick(0f, Origin, Near, true, Delay);

        Assert.True(attack.Tick(0f, Origin, Far, true, Delay).Repath);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void Range_must_be_positive(float range) =>
        Assert.Throws<ArgumentException>(() => new AutoAttack(new AutoAttackSettings { Range = range }));

    [Fact]
    public void Attack_delay_must_be_positive() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Engaged().Tick(0f, Origin, Near, true, 0));
}

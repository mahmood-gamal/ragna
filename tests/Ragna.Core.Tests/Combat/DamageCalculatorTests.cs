using Ragna.Core.Combat;

namespace Ragna.Core.Tests.Combat;

public sealed class DamageCalculatorTests
{
    // Rolls are: crit, hit, spread. 0.5 is no spread; 0.99 fails any roll below 99%.
    private const double NoCrit = 0.99;
    private const double Hits = 0.0;
    private const double Misses = 0.99;
    private const double MidSpread = 0.5;

    private static readonly CombatStats Attacker = new(Level: 1, Atk: 50, Def: 0, Hit: 20, Flee: 0, CritChance: 0);
    private static readonly CombatStats Defender = new(Level: 1, Atk: 0, Def: 10, Hit: 0, Flee: 20, CritChance: 0);

    private static readonly CombatRules NoVariance = new() { AtkVariance = 0 };

    [Fact]
    public void Hit_chance_is_base_plus_hit_minus_flee()
    {
        var calc = new DamageCalculator();

        Assert.Equal(80, calc.HitChance(Attacker, Defender));
        Assert.Equal(90, calc.HitChance(Attacker with { Hit = 30 }, Defender));
    }

    [Fact]
    public void Hit_chance_is_clamped()
    {
        var calc = new DamageCalculator();

        Assert.Equal(100, calc.HitChance(Attacker with { Hit = 500 }, Defender));
        Assert.Equal(5, calc.HitChance(Attacker, Defender with { Flee = 500 }));
    }

    [Fact]
    public void Always_hits_ignores_flee()
    {
        var calc = new DamageCalculator();
        var context = new AttackContext { AlwaysHits = true };

        Assert.Equal(100, calc.HitChance(Attacker, Defender with { Flee = 500 }, context));
    }

    [Fact]
    public void Hit_bonus_adds_to_hit()
    {
        var calc = new DamageCalculator();

        Assert.Equal(95, calc.HitChance(Attacker, Defender, new AttackContext { HitBonus = 15 }));
    }

    [Fact]
    public void A_normal_hit_deals_atk_minus_def()
    {
        var calc = new DamageCalculator(NoVariance, new FixedRandom(NoCrit, Hits, MidSpread));

        Assert.Equal(new AttackResult(AttackOutcome.Hit, 40), calc.Attack(Attacker, Defender));
    }

    [Fact]
    public void A_failed_hit_roll_misses_for_no_damage()
    {
        var calc = new DamageCalculator(NoVariance, new FixedRandom(NoCrit, Misses));

        var result = calc.Attack(Attacker, Defender);

        Assert.Equal(AttackResult.Miss, result);
        Assert.False(result.Landed);
    }

    [Fact]
    public void A_critical_always_lands_ignores_def_and_multiplies()
    {
        var random = new FixedRandom(0.0, MidSpread);
        var calc = new DamageCalculator(NoVariance, random);

        var result = calc.Attack(Attacker with { CritChance = 50 }, Defender with { Flee = 500 });

        Assert.Equal(new AttackResult(AttackOutcome.Critical, 70), result); // 50 × 1.4
        Assert.Equal(2, random.Calls); // crit and spread, no hit roll
    }

    [Fact]
    public void Criticals_can_respect_def_when_the_rules_say_so()
    {
        var rules = NoVariance with { CritIgnoresDef = false };
        var calc = new DamageCalculator(rules, new FixedRandom(0.0, MidSpread));

        var result = calc.Attack(Attacker with { CritChance = 50 }, Defender);

        Assert.Equal(56, result.Damage); // (50 - 10) × 1.4
    }

    [Fact]
    public void Attacks_that_cannot_crit_skip_the_crit_roll()
    {
        var random = new FixedRandom(Hits, MidSpread);
        var calc = new DamageCalculator(NoVariance, random);

        var result = calc.Attack(Attacker with { CritChance = 100 }, Defender, new AttackContext { CanCrit = false });

        Assert.Equal(AttackOutcome.Hit, result.Outcome);
        Assert.Equal(2, random.Calls);
    }

    [Fact]
    public void Zero_crit_chance_never_crits()
    {
        var calc = new DamageCalculator(NoVariance, new FixedRandom(0.0, Hits, MidSpread));

        Assert.Equal(AttackOutcome.Hit, calc.Attack(Attacker, Defender).Outcome);
    }

    [Theory]
    [InlineData(0.0, 35)]   // 50 × 0.9 - 10
    [InlineData(0.5, 40)]   // 50 - 10
    [InlineData(0.999, 44)] // ~50 × 1.1 - 10
    public void Atk_spreads_by_the_variance(double spreadRoll, int expected)
    {
        var calc = new DamageCalculator(new CombatRules { AtkVariance = 0.1 }, new FixedRandom(NoCrit, Hits, spreadRoll));

        Assert.Equal(expected, calc.Attack(Attacker, Defender).Damage);
    }

    [Fact]
    public void Damage_never_drops_below_the_minimum()
    {
        var calc = new DamageCalculator(NoVariance, new FixedRandom(NoCrit, Hits, MidSpread));

        Assert.Equal(1, calc.Attack(Attacker, Defender with { Def = 999 }).Damage);
    }

    [Fact]
    public void Skills_add_flat_atk_and_multiply_after_def()
    {
        var calc = new DamageCalculator(NoVariance, new FixedRandom(NoCrit, Hits, MidSpread));
        var bash = new AttackContext { Name = "Bash", FlatBonus = 10, SkillMultiplier = 3 };

        Assert.Equal(150, calc.Attack(Attacker, Defender, bash).Damage); // (50 + 10 - 10) × 3
    }

    [Fact]
    public void Damage_modifiers_run_in_order_after_the_formula()
    {
        var calc = new DamageCalculator(NoVariance, new FixedRandom(NoCrit, Hits, MidSpread));
        var context = new AttackContext { Modifiers = [new Add(5), new Times(2)] };

        Assert.Equal(90, calc.Attack(Attacker, Defender, context).Damage); // (40 + 5) × 2
    }

    [Fact]
    public void Damage_modifiers_are_not_called_on_a_miss()
    {
        var calc = new DamageCalculator(NoVariance, new FixedRandom(NoCrit, Misses));
        var spy = new Times(2);

        calc.Attack(Attacker, Defender, new AttackContext { Modifiers = [spy] });

        Assert.Equal(0, spy.Calls);
    }

    [Fact]
    public void Damage_modifiers_see_the_outcome()
    {
        var calc = new DamageCalculator(NoVariance, new FixedRandom(0.0, MidSpread));
        var spy = new Times(1);

        calc.Attack(Attacker with { CritChance = 100 }, Defender, new AttackContext { Modifiers = [spy] });

        Assert.Equal(AttackOutcome.Critical, spy.LastOutcome);
    }

    [Fact]
    public void Huge_damage_is_capped_instead_of_overflowing()
    {
        var calc = new DamageCalculator(NoVariance, new FixedRandom(NoCrit, Hits, MidSpread));
        var context = new AttackContext { SkillMultiplier = 1e300 };

        Assert.Equal(int.MaxValue, calc.Attack(Attacker, Defender, context).Damage);
    }

    [Fact]
    public void Real_random_rolls_stay_within_bounds()
    {
        var calc = new DamageCalculator(new CombatRules(), new SystemRandomSource(new Random(1234)));
        var attacker = Attacker with { CritChance = 10 };

        for (var i = 0; i < 1000; i++)
        {
            var result = calc.Attack(attacker, Defender);
            switch (result.Outcome)
            {
                case AttackOutcome.Miss:
                    Assert.Equal(0, result.Damage);
                    break;
                case AttackOutcome.Hit:
                    Assert.InRange(result.Damage, 35, 45);
                    break;
                case AttackOutcome.Critical:
                    Assert.InRange(result.Damage, 63, 77);
                    break;
            }
        }
    }

    private sealed class Add(double amount) : IDamageModifier
    {
        public double Modify(double damage, CombatStats attacker, CombatStats defender, AttackOutcome outcome) =>
            damage + amount;
    }

    private sealed class Times(double factor) : IDamageModifier
    {
        public int Calls { get; private set; }

        public AttackOutcome? LastOutcome { get; private set; }

        public double Modify(double damage, CombatStats attacker, CombatStats defender, AttackOutcome outcome)
        {
            Calls++;
            LastOutcome = outcome;
            return damage * factor;
        }
    }
}

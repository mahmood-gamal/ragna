using Ragna.Core.Combat;
using Ragna.Core.Monsters;

namespace Ragna.Core.Tests.Monsters;

public sealed class MonsterDatabaseTests
{
    private static string GameDataPath => Path.Combine(AppContext.BaseDirectory, "data", "monsters", "monsters.json");

    [Fact]
    public void Game_data_has_a_passive_and_an_aggressive_monster()
    {
        var db = MonsterDatabase.FromJson(File.ReadAllText(GameDataPath));

        var slime = db.Get("slime");
        var boar = db.Get("boar");

        Assert.Equal(2, db.All.Count);
        Assert.Equal("Slime", slime.Name);
        Assert.False(slime.Behavior.Aggressive);
        Assert.True(boar.Behavior.Aggressive);
        Assert.True(boar.MaxHp > slime.MaxHp);
    }

    [Fact]
    public void Lookup_ignores_case()
    {
        var db = MonsterDatabase.FromJson("""{ "monsters": [ { "id": "slime" } ] }""");

        Assert.True(db.TryGet("SLIME", out _));
        Assert.Throws<KeyNotFoundException>(() => db.Get("poring"));
    }

    [Fact]
    public void Missing_values_keep_defaults()
    {
        var monster = MonsterDatabase.FromJson("""{ "monsters": [ { "id": "slime", "maxHp": 10 } ] }""").Get("slime");

        Assert.Equal(10, monster.MaxHp);
        Assert.Equal(new MonsterBehavior(), monster.Behavior);
        Assert.Equal(new MonsterDefinition().ChaseSpeed, monster.ChaseSpeed);
    }

    [Fact]
    public void Combat_stats_come_from_the_definition()
    {
        var monster = new MonsterDefinition { Id = "a", Level = 3, Atk = 7, Def = 2, Hit = 9, Flee = 4 };

        Assert.Equal(new CombatStats(3, 7, 2, 9, 4, 0), monster.CombatStats);
    }

    [Theory]
    [InlineData("#ff8000", 1f, 128 / 255f, 0f)]
    [InlineData("#000000", 0f, 0f, 0f)]
    public void Parses_tints(string hex, float r, float g, float b)
    {
        Assert.True(MonsterDefinition.TryParseTint(hex, out var pr, out var pg, out var pb));
        Assert.Equal((r, g, b), (pr, pg, pb));
    }

    [Theory]
    [InlineData("""{ }""")]
    [InlineData("""{ "monsters": [ { "id": "" } ] }""")]
    [InlineData("""{ "monsters": [ { "id": "a" }, { "id": "A" } ] }""")]
    [InlineData("""{ "monsters": [ { "id": "a", "maxHp": 0 } ] }""")]
    [InlineData("""{ "monsters": [ { "id": "a", "chaseSpeed": 0 } ] }""")]
    [InlineData("""{ "monsters": [ { "id": "a", "attackDelay": 0 } ] }""")]
    [InlineData("""{ "monsters": [ { "id": "a", "respawnTime": -1 } ] }""")]
    [InlineData("""{ "monsters": [ { "id": "a", "tint": "green" } ] }""")]
    [InlineData("""{ "monsters": [ { "id": "a", "behavior": null } ] }""")]
    [InlineData("""{ "monsters": [ { "id": "a", "behavior": { "leashRadius": 1, "wanderRadius": 3 } } ] }""")]
    [InlineData("""{ "monsters": [ { "id": "a", "behavior": { "idleTimeMin": 5, "idleTimeMax": 1 } } ] }""")]
    [InlineData("""{ "monsters": [ """)]
    public void Rejects_bad_data(string json)
    {
        Assert.Throws<InvalidDataException>(() => MonsterDatabase.FromJson(json));
    }
}

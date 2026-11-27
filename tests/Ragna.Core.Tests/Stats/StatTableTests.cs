using Ragna.Core.Stats;

namespace Ragna.Core.Tests.Stats;

public sealed class StatTableTests
{
    private static string GameTablePath => Path.Combine(AppContext.BaseDirectory, "data", "stats", "stat_table.json");

    [Fact]
    public void Game_table_loads_and_validates()
    {
        var table = StatTableLoader.FromJson(File.ReadAllText(GameTablePath));

        Assert.Equal(99, table.MaxBaseLevel);
        Assert.Equal(99, table.MaxStatValue);
        Assert.Equal(48, table.StartingStatPoints);
        Assert.Equal(35, table.Hp.Base);
    }

    [Fact]
    public void Game_exp_curve_grows_every_level()
    {
        var table = StatTableLoader.FromJson(File.ReadAllText(GameTablePath));

        for (var level = 1; level < table.MaxBaseLevel - 1; level++)
        {
            Assert.True(table.Exp.ExpToNext(level + 1) > table.Exp.ExpToNext(level), $"Level {level}");
        }
    }

    [Fact]
    public void Missing_values_keep_defaults()
    {
        var table = StatTableLoader.FromJson("""{ "maxBaseLevel": 50 }""");

        Assert.Equal(50, table.MaxBaseLevel);
        Assert.Equal(new StatTable().Hp, table.Hp);
    }

    [Fact]
    public void Explicit_exp_table_overrides_formula()
    {
        var table = StatTableLoader.FromJson("""{ "maxBaseLevel": 4, "exp": { "table": [10, 20, 30] } }""");

        Assert.Equal(10, table.Exp.ExpToNext(1));
        Assert.Equal(30, table.Exp.ExpToNext(3));
    }

    [Theory]
    [InlineData("""{ "maxBaseLevel": 0 }""")]
    [InlineData("""{ "maxStatValue": 0 }""")]
    [InlineData("""{ "startingStatPoints": -1 }""")]
    [InlineData("""{ "maxBaseLevel": 4, "exp": { "table": [10, 20] } }""")]
    [InlineData("""{ "exp": { "factor": 0 } }""")]
    [InlineData("""{ "hp": { "base": 0 } }""")]
    [InlineData("""{ "attackSpeed": { "maxReduction": 1 } }""")]
    [InlineData("""{ "attackSpeed": { "minDelay": 0 } }""")]
    [InlineData("""{ "maxBaseLevel": "lots" }""")]
    [InlineData("not json")]
    [InlineData("null")]
    public void Bad_tables_are_rejected(string json)
    {
        Assert.Throws<InvalidDataException>(() => StatTableLoader.FromJson(json));
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(4, 3)]
    [InlineData(5, 4)]
    [InlineData(10, 5)]
    [InlineData(99, 22)]
    public void Stat_points_grow_every_five_levels(int level, int expected)
    {
        Assert.Equal(expected, new StatPointRules().PointsForReaching(level));
    }
}

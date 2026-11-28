using Ragna.Core.Items;
using Ragna.Core.Monsters;

namespace Ragna.Core.Tests.Items;

public sealed class ItemDatabaseTests
{
    private static string ItemsPath => Path.Combine(AppContext.BaseDirectory, "data", "items", "items.json");

    private static string MonstersPath => Path.Combine(AppContext.BaseDirectory, "data", "monsters", "monsters.json");

    [Fact]
    public void Game_data_loads_and_every_drop_is_a_known_item()
    {
        var items = ItemDatabase.FromJson(File.ReadAllText(ItemsPath));
        var monsters = MonsterDatabase.FromJson(File.ReadAllText(MonstersPath));

        items.CheckDrops(monsters);

        Assert.NotEmpty(items.All);
        Assert.All(monsters.All, m => Assert.NotEmpty(m.Drops));
        Assert.Equal("Slime Gel", items.Get("slime_gel").Name);
    }

    [Fact]
    public void Lookup_ignores_case()
    {
        var items = ItemDatabase.FromJson("""{ "items": [ { "id": "gel", "name": "Gel" } ] }""");

        Assert.True(items.TryGet("GEL", out _));
        Assert.Throws<KeyNotFoundException>(() => items.Get("tusk"));
    }

    [Fact]
    public void Missing_values_keep_defaults()
    {
        var item = ItemDatabase.FromJson("""{ "items": [ { "id": "gel" } ] }""").Get("gel");

        Assert.Equal("#ffffff", item.Tint);
        Assert.Equal("", item.Description);
    }

    [Theory]
    [InlineData("""{ "items": [ { "id": "" } ] }""")]
    [InlineData("""{ "items": [ { "id": "a", "tint": "green" } ] }""")]
    [InlineData("""{ "items": [ { "id": "a" }, { "id": "A" } ] }""")]
    [InlineData("""{ }""")]
    [InlineData("""{ "items": [ """)]
    public void Rejects_bad_data(string json)
    {
        Assert.Throws<InvalidDataException>(() => ItemDatabase.FromJson(json));
    }

    [Fact]
    public void Unknown_drop_fails_the_check()
    {
        var items = ItemDatabase.FromJson("""{ "items": [ { "id": "gel" } ] }""");
        var monsters = MonsterDatabase.FromJson("""{ "monsters": [ { "id": "slime", "drops": [ { "item": "tusk" } ] } ] }""");

        var e = Assert.Throws<InvalidDataException>(() => items.CheckDrops(monsters));
        Assert.Contains("tusk", e.Message);
    }

    [Fact]
    public void Monster_drops_are_read_from_json()
    {
        var monster = MonsterDatabase.FromJson(
            """{ "monsters": [ { "id": "slime", "drops": [ { "item": "gel", "chance": 0.7, "min": 1, "max": 2 }, { "item": "herb" } ] } ] }""")
            .Get("slime");

        Assert.Equal(
            [new DropEntry { Item = "gel", Chance = 0.7, Min = 1, Max = 2 }, new DropEntry { Item = "herb" }],
            monster.Drops);
    }

    [Fact]
    public void Monster_without_drops_drops_nothing()
    {
        var monster = MonsterDatabase.FromJson("""{ "monsters": [ { "id": "slime" } ] }""").Get("slime");

        Assert.Empty(monster.Drops);
    }

    [Theory]
    [InlineData("""{ "item": "gel", "chance": 2 }""")]
    [InlineData("""{ "item": "gel", "min": 3, "max": 1 }""")]
    [InlineData("""{ "chance": 0.5 }""")]
    public void Monster_with_a_bad_drop_is_rejected(string drop)
    {
        var json = $$"""{ "monsters": [ { "id": "slime", "drops": [ {{drop}} ] } ] }""";

        var e = Assert.Throws<InvalidDataException>(() => MonsterDatabase.FromJson(json));
        Assert.Contains("slime", e.Message);
    }
}

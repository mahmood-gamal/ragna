using Ragna.Core.Save;

namespace Ragna.Core.Tests.Save;

public sealed class SaveSerializerTests
{
    private static SaveData Sample() => new()
    {
        SavedAt = new DateTimeOffset(2026, 11, 28, 12, 30, 0, TimeSpan.Zero),
        Player = new PlayerSave
        {
            Level = 7,
            Exp = 123,
            StatPoints = 4,
            Str = 20,
            Agi = 15,
            Vit = 9,
            Int = 3,
            Dex = 12,
            Luk = 6,
            Hp = 88,
            Sp = 17,
            Position = new PositionSave(1.5f, 0.25f, -3.75f),
        },
        Inventory = new InventorySave
        {
            Columns = 8,
            Rows = 5,
            Slots = [new SlotSave(0, "slime_gel", 12), new SlotSave(9, "boar_tusk", 1)],
        },
        DialogueFlags = new Dictionary<string, bool> { ["met_elder"] = true, ["quest_declined"] = false },
    };

    [Fact]
    public void Round_trips_every_field()
    {
        var original = Sample();

        var loaded = SaveSerializer.FromJson(SaveSerializer.ToJson(original));

        Assert.Equal(original.Version, loaded.Version);
        Assert.Equal(original.SavedAt, loaded.SavedAt);
        Assert.Equal(original.Player, loaded.Player);
        Assert.Equal(original.Inventory.Columns, loaded.Inventory.Columns);
        Assert.Equal(original.Inventory.Rows, loaded.Inventory.Rows);
        Assert.Equal(original.Inventory.Slots, loaded.Inventory.Slots);
        Assert.Equal(original.DialogueFlags.OrderBy(f => f.Key), loaded.DialogueFlags.OrderBy(f => f.Key));
    }

    [Fact]
    public void Writing_twice_gives_the_same_text()
    {
        var json = SaveSerializer.ToJson(Sample());

        Assert.Equal(json, SaveSerializer.ToJson(SaveSerializer.FromJson(json)));
    }

    [Fact]
    public void Writes_camel_case_json_with_the_version()
    {
        var json = SaveSerializer.ToJson(Sample());

        Assert.Contains("\"version\": 1", json);
        Assert.Contains("\"dialogueFlags\"", json);
        Assert.Contains("\"met_elder\": true", json);
    }

    [Fact]
    public void Missing_parts_read_as_defaults()
    {
        var loaded = SaveSerializer.FromJson("""{ "version": 1, "player": { "level": 3, "position": null }, "inventory": { "slots": null } }""");

        Assert.Equal(3, loaded.Player.Level);
        Assert.Equal(new PositionSave(0, 0, 0), loaded.Player.Position);
        Assert.Empty(loaded.Inventory.Slots);
        Assert.Empty(loaded.DialogueFlags);
    }

    [Fact]
    public void Rejects_a_save_from_a_newer_build()
    {
        var e = Assert.Throws<InvalidDataException>(() => SaveSerializer.FromJson("""{ "version": 99 }"""));

        Assert.Contains("version 99", e.Message);
    }

    [Theory]
    [InlineData("""{ "version": 0 }""")]
    [InlineData("null")]
    [InlineData("not json")]
    [InlineData("""{ "version": "one" }""")]
    public void Rejects_text_that_is_not_a_save(string json)
    {
        Assert.Throws<InvalidDataException>(() => SaveSerializer.FromJson(json));
    }
}

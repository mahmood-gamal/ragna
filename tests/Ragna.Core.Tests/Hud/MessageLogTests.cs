using Ragna.Core.Hud;

namespace Ragna.Core.Tests.Hud;

public sealed class MessageLogTests
{
    [Fact]
    public void Keeps_messages_oldest_first()
    {
        var log = new MessageLog();

        log.Add(MessageKind.System, "one");
        log.Add(MessageKind.Loot, "two");

        Assert.Equal(["one", "two"], log.Messages.Select(m => m.Text));
        Assert.Equal(MessageKind.Loot, log.Messages[1].Kind);
    }

    [Fact]
    public void Drops_the_oldest_past_capacity()
    {
        var log = new MessageLog(3);

        for (var i = 1; i <= 5; i++)
        {
            log.Add(MessageKind.System, $"m{i}");
        }

        Assert.Equal(3, log.Count);
        Assert.Equal(["m3", "m4", "m5"], log.Messages.Select(m => m.Text));
    }

    [Fact]
    public void Raises_added_with_the_message()
    {
        var log = new MessageLog();
        var added = new List<LogMessage>();
        log.Added += added.Add;

        log.Add(MessageKind.LevelUp, "Level up!");

        Assert.Equal([new LogMessage(MessageKind.LevelUp, "Level up!")], added);
    }

    [Fact]
    public void Latest_returns_the_newest_messages_oldest_first()
    {
        var log = new MessageLog();
        foreach (var text in new[] { "a", "b", "c", "d" })
        {
            log.Add(MessageKind.System, text);
        }

        Assert.Equal(["c", "d"], log.Latest(2).Select(m => m.Text));
        Assert.Equal(4, log.Latest(10).Count);
        Assert.Empty(log.Latest(0));
    }

    [Fact]
    public void Rejects_empty_text() =>
        Assert.Throws<ArgumentException>(() => new MessageLog().Add(MessageKind.System, " "));

    [Fact]
    public void Capacity_must_be_positive() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new MessageLog(0));

    [Fact]
    public void Clear_empties_the_log()
    {
        var log = new MessageLog();
        log.Add(MessageKind.System, "x");

        log.Clear();

        Assert.Equal(0, log.Count);
    }
}

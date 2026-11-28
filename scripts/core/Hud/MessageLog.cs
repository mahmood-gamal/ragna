namespace Ragna.Core.Hud;

/// <summary>
/// The HUD's message log: the last <see cref="Capacity"/> messages, oldest first. Adding past the
/// capacity drops the oldest message.
/// </summary>
public sealed class MessageLog
{
    /// <summary>The <see cref="Capacity"/> used when none is given.</summary>
    public const int DefaultCapacity = 50;

    private readonly Queue<LogMessage> _messages = new();

    public MessageLog(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        Capacity = capacity;
    }

    /// <summary>Raised after a message is added, with that message.</summary>
    public event Action<LogMessage>? Added;

    /// <summary>Most messages kept.</summary>
    public int Capacity { get; }

    public int Count => _messages.Count;

    /// <summary>The kept messages, oldest first.</summary>
    public IReadOnlyList<LogMessage> Messages => [.. _messages];

    public void Add(LogMessage message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Text);
        _messages.Enqueue(message);
        while (_messages.Count > Capacity)
        {
            _messages.Dequeue();
        }

        Added?.Invoke(message);
    }

    public void Add(MessageKind kind, string text) => Add(new LogMessage(kind, text));

    /// <summary>The newest <paramref name="count"/> messages, oldest first.</summary>
    public IReadOnlyList<LogMessage> Latest(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return _messages.Skip(Math.Max(0, _messages.Count - count)).ToArray();
    }

    public void Clear() => _messages.Clear();
}

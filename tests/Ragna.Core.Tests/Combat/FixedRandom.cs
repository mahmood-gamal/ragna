using Ragna.Core.Combat;

namespace Ragna.Core.Tests.Combat;

/// <summary>Returns the given numbers in order, then repeats the last one.</summary>
internal sealed class FixedRandom(params double[] values) : IRandomSource
{
    private int _next;

    public int Calls { get; private set; }

    public double NextDouble()
    {
        Calls++;
        var value = values[Math.Min(_next, values.Length - 1)];
        _next++;
        return value;
    }
}

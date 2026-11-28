namespace Ragna.Core.Combat;

/// <summary>Random numbers for combat rolls, so tests can use fixed values.</summary>
public interface IRandomSource
{
    /// <summary>A number from 0 (inclusive) to 1 (exclusive).</summary>
    double NextDouble();
}

/// <summary><see cref="IRandomSource"/> backed by <see cref="Random"/>.</summary>
public sealed class SystemRandomSource(Random? random = null) : IRandomSource
{
    public static SystemRandomSource Shared { get; } = new(Random.Shared);

    private readonly Random _random = random ?? new Random();

    public double NextDouble() => _random.NextDouble();
}

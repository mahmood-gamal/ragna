using System.Numerics;
using Ragna.Core.Combat;

namespace Ragna.Core.Items;

/// <summary>Where dropped items land around a dead monster, so a pile doesn't stack on one spot.</summary>
public static class DropScatter
{
    /// <summary>
    /// Ground offsets (Y is 0) for <paramref name="count"/> items: spread evenly around a circle
    /// from a random starting angle, each between <paramref name="minRadius"/> and
    /// <paramref name="maxRadius"/> from the centre.
    /// </summary>
    public static IReadOnlyList<Vector3> Offsets(int count, float minRadius, float maxRadius, IRandomSource random)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentNullException.ThrowIfNull(random);
        if (minRadius < 0f || maxRadius < minRadius)
        {
            throw new ArgumentException("Radii must be 0 or more, with maxRadius at least minRadius.");
        }

        var offsets = new Vector3[count];
        var start = random.NextDouble() * Math.Tau;
        for (var i = 0; i < count; i++)
        {
            var angle = start + (i * Math.Tau / count);
            var distance = minRadius + ((float)random.NextDouble() * (maxRadius - minRadius));
            offsets[i] = new Vector3((float)Math.Cos(angle) * distance, 0f, (float)Math.Sin(angle) * distance);
        }

        return offsets;
    }
}

using Sim.Core.Domain;

namespace Sim.Core.Des;

public sealed class DeterministicRng
{
    private readonly Random _random;

    public DeterministicRng(int seed)
    {
        Seed = seed;
        _random = new Random(seed);
    }

    public int Seed { get; }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (minInclusive >= maxExclusive)
        {
            throw new DomainRuleViolationException(
                $"Invalid random integer range. MinInclusive: {minInclusive}, MaxExclusive: {maxExclusive}.");
        }

        return _random.Next(minInclusive, maxExclusive);
    }

    public decimal NextDecimal()
    {
        return (decimal)_random.NextDouble();
    }
}

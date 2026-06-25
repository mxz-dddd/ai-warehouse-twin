using Sim.Core.Des;
using Sim.Core.Domain;
using Xunit;

namespace Sim.Core.Tests.Des;

public sealed class DeterministicRngTests
{
    [Fact]
    public void NextInt_ProducesSameSequence_ForSameSeed()
    {
        var first = new DeterministicRng(123);
        var second = new DeterministicRng(123);

        var firstValues = Enumerable.Range(0, 5).Select(_ => first.NextInt(0, 100)).ToArray();
        var secondValues = Enumerable.Range(0, 5).Select(_ => second.NextInt(0, 100)).ToArray();

        Assert.Equal(firstValues, secondValues);
    }

    [Fact]
    public void NextInt_ProducesDifferentSequence_ForDifferentSeeds()
    {
        var first = new DeterministicRng(123);
        var second = new DeterministicRng(456);

        var firstValues = Enumerable.Range(0, 5).Select(_ => first.NextInt(0, 100)).ToArray();
        var secondValues = Enumerable.Range(0, 5).Select(_ => second.NextInt(0, 100)).ToArray();

        Assert.NotEqual(firstValues, secondValues);
    }

    [Fact]
    public void NextDecimal_ReturnsValueInHalfOpenUnitRange()
    {
        var rng = new DeterministicRng(123);

        for (var index = 0; index < 20; index++)
        {
            var value = rng.NextDecimal();
            Assert.True(value >= 0m);
            Assert.True(value < 1m);
        }
    }

    [Fact]
    public void NextInt_Throws_ForInvalidRange()
    {
        var rng = new DeterministicRng(123);

        Assert.Throws<DomainRuleViolationException>(() => rng.NextInt(5, 5));
        Assert.Throws<DomainRuleViolationException>(() => rng.NextInt(6, 5));
    }
}

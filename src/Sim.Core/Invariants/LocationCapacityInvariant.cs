using Sim.Core.Domain;

namespace Sim.Core.Invariants;

public static class LocationCapacityInvariant
{
    public static void EnsureWithinCapacity(
        decimal totalWeightGrams,
        decimal totalVolumeMm3,
        decimal maxWeightGrams,
        decimal maxVolumeMm3)
    {
        if (maxWeightGrams < 0)
        {
            throw new DomainRuleViolationException(
                $"Location max weight cannot be negative. MaxWeightGrams: {maxWeightGrams}.");
        }

        if (maxVolumeMm3 < 0)
        {
            throw new DomainRuleViolationException(
                $"Location max volume cannot be negative. MaxVolumeMm3: {maxVolumeMm3}.");
        }

        if (totalWeightGrams > maxWeightGrams)
        {
            throw new DomainRuleViolationException(
                $"Location weight capacity exceeded. TotalWeightGrams: {totalWeightGrams}, MaxWeightGrams: {maxWeightGrams}.");
        }

        if (totalVolumeMm3 > maxVolumeMm3)
        {
            throw new DomainRuleViolationException(
                $"Location volume capacity exceeded. TotalVolumeMm3: {totalVolumeMm3}, MaxVolumeMm3: {maxVolumeMm3}.");
        }
    }
}

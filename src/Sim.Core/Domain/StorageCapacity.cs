namespace Sim.Core.Domain;

public readonly record struct StorageCapacity
{
    public StorageCapacity(decimal maxWeightGrams, decimal maxVolumeMm3)
    {
        if (maxWeightGrams < 0)
        {
            throw new DomainRuleViolationException(
                $"Storage max weight cannot be negative. MaxWeightGrams: {maxWeightGrams}.");
        }

        if (maxVolumeMm3 < 0)
        {
            throw new DomainRuleViolationException(
                $"Storage max volume cannot be negative. MaxVolumeMm3: {maxVolumeMm3}.");
        }

        MaxWeightGrams = maxWeightGrams;
        MaxVolumeMm3 = maxVolumeMm3;
    }

    public decimal MaxWeightGrams { get; }

    public decimal MaxVolumeMm3 { get; }
}

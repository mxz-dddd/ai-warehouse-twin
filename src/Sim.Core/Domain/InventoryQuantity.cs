namespace Sim.Core.Domain;

public readonly record struct InventoryQuantity
{
    public InventoryQuantity(decimal value)
    {
        if (value < 0)
        {
            throw new DomainRuleViolationException(
                $"Inventory quantity cannot be negative. Quantity: {value}.");
        }

        Value = value;
    }

    public decimal Value { get; }

    public InventoryQuantity Add(InventoryQuantity other)
    {
        return new InventoryQuantity(Value + other.Value);
    }

    public InventoryQuantity Subtract(InventoryQuantity other)
    {
        var result = Value - other.Value;
        if (result < 0)
        {
            throw new DomainRuleViolationException(
                $"Inventory quantity subtraction would be negative. Before: {Value}, subtract: {other.Value}, after: {result}.");
        }

        return new InventoryQuantity(result);
    }
}

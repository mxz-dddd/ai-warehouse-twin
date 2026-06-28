namespace Sim.Validation;

/// <summary>
/// Outcome of validating a scenario document. <see cref="Errors"/> is empty when
/// <see cref="IsValid"/> is true, and lists every problem found (validation does
/// not stop at the first error) otherwise.
/// </summary>
public sealed record ScenarioValidationResult(bool IsValid, IReadOnlyList<ScenarioValidationError> Errors);

namespace Sim.Validation;

/// <summary>
/// A single problem found in a scenario document.
/// <para><see cref="Path"/> is a JSON path (e.g. <c>$.outbound.orders[0].quantity</c>)
/// locating the offending value. <see cref="Code"/> is a stable machine-readable
/// identifier. <see cref="Message"/> is a human-readable, customer-facing sentence.</para>
/// </summary>
public sealed record ScenarioValidationError(string Path, string Code, string Message);

using System.Text;
using System.Text.Json;

namespace Sim.Validation;

/// <summary>
/// Renders a <see cref="ScenarioValidationResult"/> for humans (friendly,
/// locatable text) or machines (stable JSON). Text output uses LF newlines only.
/// </summary>
public static class ScenarioErrorFormatter
{
    public static string FormatText(ScenarioValidationResult result, string? source = null)
    {
        if (result.IsValid)
        {
            return source is null
                ? $"校验通过:场景符合 {ScenarioValidator.ExpectedSchemaVersion} 输入规范。"
                : $"校验通过:{source} 符合 {ScenarioValidator.ExpectedSchemaVersion} 输入规范。";
        }

        var count = result.Errors.Count;
        var sb = new StringBuilder();
        sb.Append(source is null
            ? $"输入校验未通过,发现 {count} 处问题:"
            : $"输入校验未通过:{source}(发现 {count} 处问题):");
        sb.Append('\n');

        var index = 1;
        foreach (var error in result.Errors)
        {
            sb.Append('\n');
            sb.Append("  ").Append(index).Append(". ").Append(error.Message);
            sb.Append('\n');
            sb.Append("     位置:").Append(DisplayPath(error.Path));
            sb.Append('\n');
            index++;
        }

        sb.Append("\n请按上面每条的“位置”修改后重新校验。\n");
        return sb.ToString();
    }

    public static string FormatJson(ScenarioValidationResult result)
    {
        var payload = new ResultJson(
            ScenarioValidator.ExpectedSchemaVersion,
            result.IsValid,
            result.Errors.Count,
            result.Errors.Select(e => new ErrorJson(e.Path, e.Code, e.Message)).ToArray());

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static string DisplayPath(string path)
    {
        if (path == "$")
        {
            return "(顶层)";
        }

        return path.StartsWith("$.", StringComparison.Ordinal) ? path[2..] : path;
    }

    private sealed record ResultJson(string SchemaVersion, bool Valid, int ErrorCount, IReadOnlyList<ErrorJson> Errors);

    private sealed record ErrorJson(string Path, string Code, string Message);
}

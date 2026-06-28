using Sim.Validation;
using Xunit;

namespace Sim.Validation.Tests;

/// <summary>
/// Snapshot tests: each invalid sample under datasets/validation-cases/invalid/
/// must produce exactly the friendly error text frozen in its companion
/// <c>.expected.txt</c> golden. Regenerate goldens by running the suite with the
/// environment variable <c>UPDATE_GOLDENS=1</c>, then review the diff by eye.
/// </summary>
public class InvalidScenariosGoldenTests
{
    public static IEnumerable<object[]> InvalidFiles()
    {
        foreach (var file in Directory.EnumerateFiles(TestPaths.InvalidCasesDir(), "*.json"))
        {
            yield return new object[] { Path.GetFileName(file) };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidFiles))]
    public void InvalidScenario_ProducesGoldenErrors(string fileName)
    {
        var jsonPath = Path.Combine(TestPaths.InvalidCasesDir(), fileName);
        var goldenPath = Path.ChangeExtension(jsonPath, ".expected.txt");

        var result = ScenarioValidator.Validate(File.ReadAllText(jsonPath));

        Assert.False(result.IsValid, $"{fileName} 预期不合法,但校验通过了。");

        var actual = TestPaths.NormalizeNewlines(ScenarioErrorFormatter.FormatText(result));

        if (Environment.GetEnvironmentVariable("UPDATE_GOLDENS") == "1")
        {
            File.WriteAllText(goldenPath, actual);
        }

        Assert.True(
            File.Exists(goldenPath),
            $"缺少 golden 文件:{goldenPath}(用 UPDATE_GOLDENS=1 生成后人工核对)。");

        var golden = TestPaths.NormalizeNewlines(File.ReadAllText(goldenPath));
        Assert.Equal(golden, actual);
    }
}

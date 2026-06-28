using Sim.Validation;
using Xunit;

namespace Sim.Validation.Tests;

public class ValidScenariosTests
{
    public static IEnumerable<object[]> ValidFiles()
    {
        yield return new object[] { Path.Combine(TestPaths.DatasetsDir(), "sample-small-warehouse", "scenario.json") };
        yield return new object[] { Path.Combine(TestPaths.DatasetsDir(), "sample-each-pick", "scenario.json") };
        yield return new object[] { Path.Combine(TestPaths.TemplatesDir(), "scenario.template.json") };

        foreach (var file in Directory.EnumerateFiles(TestPaths.ValidCasesDir(), "*.json"))
        {
            yield return new object[] { file };
        }
    }

    [Theory]
    [MemberData(nameof(ValidFiles))]
    public void ValidScenario_PassesWithNoErrors(string path)
    {
        var result = ScenarioValidator.Validate(File.ReadAllText(path));

        Assert.True(
            result.IsValid,
            $"预期合法,但校验报错:\n{ScenarioErrorFormatter.FormatText(result, Path.GetFileName(path))}");
        Assert.Empty(result.Errors);
    }
}

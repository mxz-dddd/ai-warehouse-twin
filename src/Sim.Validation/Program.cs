using Sim.Validation;

// CLI: validate a customer scenario document and report problems.
// Exit codes: 0 = valid, 1 = invalid input, 2 = usage / file-read error.
string? file = null;
var format = "text";

for (var i = 0; i < args.Length; i++)
{
    var a = args[i];
    if (a is "--format" or "-f")
    {
        if (i + 1 >= args.Length)
        {
            return Usage("缺少 --format 的取值(text 或 json)。");
        }

        format = args[++i];
    }
    else if (a is "-h" or "--help")
    {
        PrintUsage(Console.Out);
        return 0;
    }
    else if (file is null)
    {
        file = a;
    }
    else
    {
        return Usage($"多余的参数:{a}");
    }
}

if (file is null)
{
    return Usage("请提供要校验的 scenario.json 文件路径。");
}

if (format is not ("text" or "json"))
{
    return Usage($"不支持的 --format 取值:{format}(仅支持 text 或 json)。");
}

string json;
try
{
    json = File.ReadAllText(file);
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"无法读取文件:{file}({ex.Message})");
    return 2;
}

var result = ScenarioValidator.Validate(json);

if (format == "json")
{
    Console.Out.WriteLine(ScenarioErrorFormatter.FormatJson(result));
    return result.IsValid ? 0 : 1;
}

if (result.IsValid)
{
    Console.Out.WriteLine(ScenarioErrorFormatter.FormatText(result, file));
    return 0;
}

// FormatText already ends with a newline; Write (not WriteLine) keeps the output
// LF-only and avoids appending a platform newline on Windows.
Console.Error.Write(ScenarioErrorFormatter.FormatText(result, file));
return 1;

int Usage(string message)
{
    Console.Error.WriteLine($"错误:{message}");
    Console.Error.WriteLine();
    PrintUsage(Console.Error);
    return 2;
}

void PrintUsage(TextWriter w)
{
    w.WriteLine("用法:");
    w.WriteLine("  dotnet run --project src/Sim.Validation -- <scenario.json>");
    w.WriteLine("  dotnet run --project src/Sim.Validation -- <scenario.json> --format json");
    w.WriteLine();
    w.WriteLine("退出码:0 = 校验通过 / 1 = 校验未通过 / 2 = 用法或读取文件错误");
}

namespace Codemetr.CSharp.Tool;

/// <summary>Parsed command line.</summary>
internal sealed record CliOptions(string Command, CodemetrRequest Request, string Format)
{
    public const string Json = "json", Text = "text";

    public const string Usage = """
        codemetr-csharp - C# code metrics per class and member, as a quality guard for local runs and CI

        Usage:
          codemetr-csharp analyze  [PATH...] [options]   metrics of every type, plus violations (exit 0 unless input error)
          codemetr-csharp check    [PATH...] [options]   guard: exit 1 on new/worsened violations or a stale baseline
          codemetr-csharp baseline [PATH...] [options]   record current violations as tolerated debt (ratchet)

        Options:
          --root DIR          base for relative paths, codemetr.json and codemetr.baseline.json (default: current directory)
          --type NAME         only types with this simple name, full name or full-name suffix
          --files-from FILE   additional file list, one path per line ('-' = stdin); missing and non-.cs entries are skipped
          --config FILE       rules file (default: ROOT/codemetr.json if present, else built-in defaults)
          --baseline FILE     baseline file (default: ROOT/codemetr.baseline.json)
          --format json|text  output format (default: json)
          --include-metrics   check: include per-type metrics in the report
          --mutation-report FILE  import a Stryker mutation-testing-elements v1/v2 JSON report (advisory)
          --top N             number of hotspots in the report (default: 10)

        Exit codes: 0 passed, 1 findings, 2 input or usage error.
        """;

    public static CliOptions Parse(string[] args, TextReader stdin)
    {
        if (args.Length == 0) throw new ArgumentException("Missing command");
        var command = args[0];
        if (command is not (CodemetrService.Analyze or CodemetrService.Check or CodemetrService.RecordBaseline))
            throw new ArgumentException($"Unknown command '{command}'");
        var root = Environment.CurrentDirectory;
        string? type = null, config = null, baseline = null, mutation = null;
        var format = Json;
        var includeMetrics = false;
        var top = CodemetrService.DefaultTop;
        var paths = new List<string>();
        var listed = new List<string>();
        var hasFileList = false;
        for (var index = 1; index < args.Length; index++)
        {
            string Value() => index + 1 < args.Length && !string.IsNullOrWhiteSpace(args[index + 1])
                ? args[++index] : throw new ArgumentException($"Missing value for {args[index]}");
            switch (args[index])
            {
                case "--root": root = Value(); break;
                case "--type": type = Value(); break;
                case "--config": config = Value(); break;
                case "--mutation-report": mutation = Value(); break;
                case "--baseline": baseline = Value(); break;
                case "--include-metrics": includeMetrics = true; break;
                case "--format":
                    format = Value();
                    if (format is not (Json or Text)) throw new ArgumentException("--format must be json or text");
                    break;
                case "--top":
                    if (!int.TryParse(Value(), out top) || top < 0) throw new ArgumentException("--top must be a non-negative integer");
                    break;
                case "--files-from":
                    var source = Value();
                    hasFileList = true;
                    var lines = source == "-" ? ReadLines(stdin) : File.ReadAllLines(source);
                    listed.AddRange(lines.Select(line => line.Trim()).Where(line => line.Length > 0));
                    break;
                default:
                    if (args[index].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"Unknown option '{args[index]}'");
                    paths.Add(args[index]);
                    break;
            }
        }
        return new CliOptions(command, new CodemetrRequest
        {
            Root = root, Paths = paths, ListedFiles = listed, HasFileList = hasFileList, Type = type, ConfigPath = config, BaselinePath = baseline,
            IncludeMetrics = includeMetrics, Top = top, MutationReport = mutation,
        }, format);
    }

    static IEnumerable<string> ReadLines(TextReader reader)
    {
        while (reader.ReadLine() is { } line) yield return line;
    }
}

using System.Reflection;
using System.Text.Json;

namespace Codemetr.CSharp.Tool;

/// <summary>The three operations behind the command line.</summary>
internal static class CodemetrService
{
    public const int DefaultTop = 10;
    public const string Analyze = "analyze", Check = "check", RecordBaseline = "baseline";

    public static string Version { get; } = typeof(CodemetrService).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0";

    public static CodemetrReport Run(string command, CodemetrRequest request, CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(request.Root);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Root not found: {request.Root}");
        var config = LoadConfig(root, request.ConfigPath);
        var documents = Collect(root, request, config);
        var all = CodeAnalyzer.Analyze(documents, cancellationToken);
        var selected = all.SelectTypes(request.Type);
        if (request.Type is not null && selected.Types.Count == 0 && all.ParseErrors.Count == 0)
            throw new InvalidDataException($"No analysed type matches '{request.Type}'");
        var violations = GuardEvaluator.Evaluate(selected.Types, config);
        var scope = AnalysisScope.Of(documents, all, request.Type is not null);
        var baselinePath = request.BaselinePath is null ? Path.Combine(root, Baseline.FileName) : Path.GetFullPath(request.BaselinePath, root);
        var baseline = File.Exists(baselinePath) ? Baseline.Load(baselinePath) : Baseline.Empty;

        string? written = null;
        if (command == RecordBaseline)
        {
            if (all.ParseErrors.Count > 0) throw new InvalidDataException("Refusing to record a baseline while documents fail to parse");
            var recorded = baseline.Record(violations, scope);
            File.WriteAllText(baselinePath, JsonSerializer.Serialize(recorded, CodemetrJson.Options) + "\n");
            baseline = recorded;
            written = baselinePath;
        }

        var comparison = baseline.Compare(violations, scope);
        var rows = comparison.Violations.Select(item => new ReportViolation(item.Violation.Rule, item.Status, item.Violation.Type,
            item.Violation.Member, item.Violation.File, item.Violation.Line, item.Violation.Value, item.Violation.Limit,
            item.BaselineValue, item.Violation.Message, item.Violation.Hint)).ToArray();
        var stale = comparison.Stale.Select(item => new ReportStale(item.Entry.Rule, item.Entry.Type, item.Entry.Member, item.Entry.File,
            item.Entry.Value, item.CurrentValue, item.CurrentValue is null
                ? "Recorded violation is fixed; run 'codemetr-csharp baseline' to remove it."
                : "Recorded violation improved; run 'codemetr-csharp baseline' to tighten it.")).ToArray();
        var summary = new ReportSummary(documents.Count, selected.Types.Count, rows.Length,
            rows.Count(r => r.Status == CheckedViolation.New), rows.Count(r => r.Status == CheckedViolation.Worsened),
            rows.Count(r => r.Status == CheckedViolation.Baselined), stale.Length, all.ParseErrors.Count);
        var passed = all.ParseErrors.Count == 0 && comparison.Passed;
        var includeTypes = command == Analyze || request.IncludeMetrics;
        return new CodemetrReport("codemetr-csharp", Version, command, passed, summary, rows, stale, all.ParseErrors,
            Hotspots(selected.Types, request.Top), includeTypes ? selected.Types : null, written);
    }

    static CodemetrConfig LoadConfig(string root, string? configPath)
    {
        if (configPath is not null) return CodemetrConfig.Load(Path.GetFullPath(configPath, root));
        var conventional = Path.Combine(root, CodemetrConfig.FileName);
        return File.Exists(conventional) ? CodemetrConfig.Load(conventional) : new CodemetrConfig();
    }

    static IReadOnlyList<SourceDocument> Collect(string root, CodemetrRequest request, CodemetrConfig config)
    {
        var documents = new List<SourceDocument>();
        if (request.Paths.Count > 0 || request.ListedFiles.Count == 0)
            documents.AddRange(SourceCollector.Collect(root, request.Paths.Count > 0 ? request.Paths : ["."], config.Exclude));
        if (request.ListedFiles.Count > 0)
            documents.AddRange(SourceCollector.Collect(root, request.ListedFiles, config.Exclude, lenient: true));
        var duplicate = documents.GroupBy(d => d.Path, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) throw new InvalidDataException($"Source listed twice: {duplicate.Key}");
        return documents;
    }

    // Hotspot order: complexity first, then coupling, inheritance, maintainability, size.
    static IReadOnlyList<string> Hotspots(IEnumerable<TypeMetrics> types, int top) => types
        .OrderByDescending(t => t.MaxCyclomaticComplexity).ThenByDescending(t => t.ClassCoupling)
        .ThenByDescending(t => t.DepthOfInheritance).ThenBy(t => t.MaintainabilityIndex).ThenByDescending(t => t.Lines)
        .ThenBy(t => t.FullName, StringComparer.Ordinal).Take(Math.Max(0, top))
        .Select(t => $"{t.File}:{t.Line} {t.FullName}: complexity {t.MaxCyclomaticComplexity} ({(t.MostComplexMember.Length == 0 ? "n/a" : t.MostComplexMember)}), "
            + $"maintainability {t.MaintainabilityIndex}, coupling {t.ClassCoupling}, inheritance {t.DepthOfInheritance}, lines {t.Lines}")
        .ToArray();
}

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
        var warnings = new List<string>();
        var listed = request.ListedFiles.Select(path => path.Replace('\\', '/')).ToArray();
        var paths = request.Paths.Count > 0 || !request.HasFileList ? (request.Paths.Count > 0 ? request.Paths : ["."]) : [];
        var documents = Collect(root, paths, listed, config);
        var missingListed = listed.Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !File.Exists(Path.GetFullPath(path, root)))
            .Select(path => Relative(root, Path.GetFullPath(path, root))).ToArray();
        var listedSources = listed.Count(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
        if (listedSources > 0 && missingListed.Length == listedSources)
            warnings.Add($"None of the {listedSources} listed C# files exists below {root}; check --root (file lists from git are relative to the repository root).");
        // The configured context is always compiled, so coupling and inheritance do not depend on which files a run selects.
        var all = CodeAnalyzer.Analyze(documents, SourceCollector.CollectContext(root, config.Context), request.Type, cancellationToken);
        var selected = all;
        if (request.Type is not null && selected.Types.Count == 0 && all.ParseErrors.Count == 0)
            throw new InvalidDataException($"No analysed type matches '{request.Type}'");
        var violations = GuardEvaluator.Evaluate(selected.Types, config);
        var directories = paths.Select(path => Path.GetFullPath(path, root)).Where(Directory.Exists).Select(path => Relative(root, path));
        var scope = new AnalysisScope(documents.Select(d => d.Path).Concat(missingListed),
            request.Type is null ? null : selected.Types.Select(t => t.FullName), directories, all.ParseErrors.Select(e => e.File));
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
            Hotspots(selected.Types, request.Top), includeTypes ? selected.Types : null, written, warnings.Count > 0 ? warnings : null);
    }

    static CodemetrConfig LoadConfig(string root, string? configPath)
    {
        if (configPath is not null) return CodemetrConfig.Load(Path.GetFullPath(configPath, root));
        var conventional = Path.Combine(root, CodemetrConfig.FileName);
        return File.Exists(conventional) ? CodemetrConfig.Load(conventional) : new CodemetrConfig();
    }

    static IReadOnlyList<SourceDocument> Collect(string root, IReadOnlyList<string> paths, IReadOnlyList<string> listed, CodemetrConfig config)
    {
        var documents = new List<SourceDocument>();
        if (paths.Count > 0) documents.AddRange(SourceCollector.Collect(root, paths, config.Exclude));
        if (listed.Count > 0) documents.AddRange(SourceCollector.Collect(root, listed, config.Exclude, lenient: true));
        return documents.DistinctBy(document => document.Path, StringComparer.Ordinal).ToArray();
    }

    static string Relative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/') is var relative && relative == "." ? "" : relative;

    // Hotspot order: complexity first, then coupling, inheritance, maintainability, size.
    static IReadOnlyList<string> Hotspots(IEnumerable<TypeMetrics> types, int top) => types
        .OrderByDescending(t => t.MaxCyclomaticComplexity).ThenByDescending(t => t.ClassCoupling)
        .ThenByDescending(t => t.DepthOfInheritance).ThenBy(t => t.MaintainabilityIndex).ThenByDescending(t => t.Lines)
        .ThenBy(t => t.FullName, StringComparer.Ordinal).Take(Math.Max(0, top))
        .Select(t => $"{t.File}:{t.Line} {t.FullName}: complexity {t.MaxCyclomaticComplexity} ({(t.MostComplexMember.Length == 0 ? "n/a" : t.MostComplexMember)}), "
            + $"maintainability {t.MaintainabilityIndex}, coupling {t.ClassCoupling}, inheritance {t.DepthOfInheritance}, lines {t.Lines}")
        .ToArray();
}

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
        var selection = FileSelector.Select(root, request, config, warnings);
        // The configured context is always compiled, so coupling and inheritance do not depend on which files a run selects.
        var all = CodeAnalyzer.Analyze(selection.Documents, SourceCollector.CollectContext(root, config.Context), request.Type, cancellationToken);
        warnings.AddRange(all.Warnings);
        if (request.Type is not null && all.Types.Count == 0 && all.ParseErrors.Count == 0 && selection.Documents.Count > 0)
            throw new InvalidDataException($"No analysed type matches '{request.Type}'");
        var violations = GuardEvaluator.Evaluate(all.Types, config);
        var scope = Scope(root, request, selection, all, config);
        var baselinePath = request.BaselinePath is null ? Path.Combine(root, Baseline.FileName) : Path.GetFullPath(request.BaselinePath, root);
        var baseline = File.Exists(baselinePath) ? Baseline.Load(baselinePath) : Baseline.Empty;
        var fileExists = (string path) => File.Exists(Path.GetFullPath(path, root));
        var comparison = baseline.Compare(violations, scope, fileExists);
        string? written = null;
        if (command == RecordBaseline)
        {
            if (all.ParseErrors.Count > 0) throw new InvalidDataException("Refusing to record a baseline while documents fail to parse");
            if (TryRecord(baselinePath, baseline, comparison, violations, warnings) is { } recorded)
            {
                written = baselinePath;
                comparison = recorded.Compare(violations, scope, fileExists);
            }
        }
        if (comparison.Unverified.Count > 0)
            warnings.Add($"{comparison.Unverified.Count} recorded violation(s) could not be verified in this run (inactive #if code, or a deleted " +
                "file in a file-list run); they stay in the baseline. A full run settles deleted files.");
        return ReportBuilder.Build(command, request, selection, all, comparison, written, warnings);
    }

    static AnalysisScope Scope(string root, CodemetrRequest request, FileSelection selection, AnalysisResult all, CodemetrConfig config)
    {
        var unparsed = all.ParseErrors.Select(e => e.File).ToHashSet(StringComparer.Ordinal);
        var directories = selection.Paths.Select(path => Path.GetFullPath(path, root)).Where(Directory.Exists).Select(path => Relative(root, path));
        return new AnalysisScope(selection.Documents.Select(d => d.Path).Concat(selection.MissingListed),
            request.Type is null ? null : all.Types.Select(t => t.FullName), directories, unparsed)
        {
            Measured = selection.Documents.Select(d => d.Path).Where(path => !unparsed.Contains(path)).ToHashSet(StringComparer.Ordinal),
            Uncertain = all.FilesWithInactiveCode.ToHashSet(StringComparer.Ordinal),
            Declarations = new DeclarationIndex(root, config.Exclude),
        };
    }

    // An existing baseline only ever tightens: the safe updates are written, new or worse debt and entries
    // that are no longer measured are kept out or kept as they are, and the run still fails on them.
    static Baseline? TryRecord(string path, Baseline baseline, BaselineComparison comparison, IReadOnlyList<Violation> violations, List<string> warnings)
    {
        var exists = File.Exists(path);
        var refused = exists ? comparison.Violations.Count(item => item.Fails) : 0;
        var unmeasured = comparison.Stale.Count(item => item.Unmeasured);
        if (refused > 0 || unmeasured > 0)
            warnings.Add($"An existing baseline only tightens: {refused} new or worse violation(s) were not recorded and {unmeasured} entry(ies) " +
                "of code that is no longer measured were kept. Fix the violations and measure the code again.");
        var recorded = exists ? baseline.Tighten(comparison) : Baseline.Initial(violations);
        File.WriteAllText(path, JsonSerializer.Serialize(recorded, CodemetrJson.Options) + "\n");
        return recorded;
    }

    static CodemetrConfig LoadConfig(string root, string? configPath)
    {
        if (configPath is not null) return CodemetrConfig.Load(Path.GetFullPath(configPath, root));
        var conventional = Path.Combine(root, CodemetrConfig.FileName);
        return File.Exists(conventional) ? CodemetrConfig.Load(conventional) : new CodemetrConfig();
    }

    internal static string Relative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/') is var relative && relative == "." ? "" : relative;
}

using System.Text.Json;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Codemetr.CSharp;

/// <summary>Contents of <c>codemetr.json</c>. Missing values fall back to <see cref="GuardRules.Default"/>.</summary>
public sealed record CodemetrConfig
{
    /// <summary>Conventional file name looked up in the analysis root.</summary>
    public const string FileName = "codemetr.json";

    /// <summary>Rules on top of the defaults.</summary>
    public GuardRules? Rules { get; init; }
    /// <summary>Overrides in order; later matches win.</summary>
    public RuleOverride[] Overrides { get; init; } = [];
    /// <summary>Globs over report paths that are not analysed at all.</summary>
    public string[] Exclude { get; init; } = [];
    /// <summary>
    /// Directories (relative to the root) compiled as context for type resolution, whatever a run measures.
    /// Narrow it to one code tree when a repository holds several, so equal type names do not collide.
    /// </summary>
    public string[] Context { get; init; } = ["."];

    /// <summary>Loads and validates a configuration file.</summary>
    public static CodemetrConfig Load(string path)
    {
        var config = JsonSerializer.Deserialize<CodemetrConfig>(File.ReadAllBytes(path), CodemetrJson.Options)
            ?? throw new InvalidDataException("Empty codemetr configuration");
        if (config.Overrides is null || config.Exclude is null || config.Context is null || config.Context.Length == 0
            || config.Exclude.Concat(config.Context).Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("codemetr configuration: 'overrides', 'exclude' and 'context' must be arrays of non-empty values");
        foreach (var rule in config.Overrides)
        {
            if (rule is null || rule.Rules is null) throw new InvalidDataException("A codemetr override needs 'rules'");
            if (rule.Files is null && rule.Type is null) throw new InvalidDataException("A codemetr override needs 'files' or 'type'");
        }
        return config;
    }

    /// <summary>Effective rules for one type.</summary>
    public GuardRules RulesFor(TypeMetrics type)
    {
        var rules = GuardRules.Default.Merge(Rules);
        foreach (var rule in Overrides)
        {
            var fileMatches = rule.Files is null || Glob(rule.Files).Match(type.File).HasMatches;
            var typeMatches = rule.Type is null || TypeName.Matches(type, rule.Type, allowSuffix: false);
            if (fileMatches && typeMatches) rules = rules.Merge(rule.Rules);
        }
        return rules;
    }

    static Matcher Glob(string pattern)
    {
        var matcher = new Matcher(StringComparison.Ordinal);
        matcher.AddInclude(pattern);
        return matcher;
    }
}

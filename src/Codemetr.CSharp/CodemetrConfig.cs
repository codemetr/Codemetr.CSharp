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

    /// <summary>Loads and validates a configuration file.</summary>
    public static CodemetrConfig Load(string path)
    {
        var config = JsonSerializer.Deserialize<CodemetrConfig>(File.ReadAllBytes(path), CodemetrJson.Options)
            ?? throw new InvalidDataException("Empty codemetr configuration");
        foreach (var rule in config.Overrides)
            if (rule.Files is null && rule.Type is null)
                throw new InvalidDataException("A codemetr override needs 'files' or 'type'");
        return config;
    }

    /// <summary>Effective rules for one type.</summary>
    public GuardRules RulesFor(TypeMetrics type)
    {
        var rules = GuardRules.Default.Merge(Rules);
        foreach (var rule in Overrides)
        {
            var fileMatches = rule.Files is null || Glob(rule.Files).Match(type.File).HasMatches;
            var typeMatches = rule.Type is null || rule.Type == type.FullName || rule.Type == type.Name;
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

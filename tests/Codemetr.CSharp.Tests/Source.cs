namespace Codemetr.CSharp.Tests;

internal static class Source
{
    public static AnalysisResult Analyze(string text, string path = "a.cs") => CodeAnalyzer.Analyze([new SourceDocument(path, text)]);

    public static TypeMetrics Type(string text, string name) => Analyze(text).Types.Single(type => type.Name == name);

    /// <summary>Member by name without signature, e.g. "M" for "M(int)"; the name must be unique in the type.</summary>
    public static MemberMetrics Member(string text, string type, string member) =>
        Type(text, type).Members.Single(m => m.Name == member || (m.Kind != "localFunction" && m.Name.StartsWith(member + "(", StringComparison.Ordinal)));
}

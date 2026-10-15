namespace Codemetr.CSharp.Tests;

internal static class Source
{
    public static AnalysisResult Analyze(string text, string path = "a.cs") => CodeAnalyzer.Analyze([new SourceDocument(path, text)]);

    public static TypeMetrics Type(string text, string name) => Analyze(text).Types.Single(type => type.Name == name);

    public static MemberMetrics Member(string text, string type, string member) => Type(text, type).Members.Single(m => m.Name == member);
}

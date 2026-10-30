using System.Text.Json;
using Codemetr.CSharp.Tool;

namespace Codemetr.CSharp.Tests;

public sealed class MemberHotspotTests
{
    [Fact]
    public async Task LongFlatMembersAreVisibleWithSourceAndExistingComplexity()
    {
        using var dir = new TempDirectory();
        dir.Write("b.cs", "class B { void Small() { Small(); } }");
        dir.Write("a.cs", "class A { void Long() {\n int x = 0;\n x++;\n x++;\n} }");
        var result = await Run(dir.Path, "analyze", "--top", "1");
        Assert.Equal(0, result.Exit);
        using var json = JsonDocument.Parse(result.Text);
        Assert.Equal(1, json.RootElement.GetProperty("schema").GetInt32());
        var row = Assert.Single(json.RootElement.GetProperty("memberHotspots").EnumerateArray());
        Assert.Equal("Long()", row.GetProperty("member").GetString());
        Assert.Equal("a.cs", row.GetProperty("file").GetString());
        Assert.Equal(3, row.GetProperty("codeLines").GetInt32());
        Assert.Equal(3, row.GetProperty("statementCount").GetInt32());
        Assert.Equal(1, row.GetProperty("cyclomaticComplexity").GetInt32());
        Assert.Equal(0, row.GetProperty("cognitiveComplexity").GetInt32());
        Assert.Equal(0, row.GetProperty("nestingDepth").GetInt32());
        Assert.Equal(0, row.GetProperty("parameterCount").GetInt32());
        Assert.False(row.TryGetProperty("coverage", out _));
        Assert.Single(json.RootElement.GetProperty("hotspots").EnumerateArray());
        Assert.Contains("Member hotspots:", (await Run(dir.Path, "analyze", "--format", "text")).Text);
        Assert.Contains("code lines 3, statements 3", (await Run(dir.Path, "analyze", "--format", "text")).Text);
    }

    [Fact]
    public async Task SelectionTopZeroAndDefaultGuardStayCompatible()
    {
        using var dir = new TempDirectory();
        dir.Write("a.cs", "class A { void M(int a) { } void M(string a) { } }");
        dir.Write("b.cs", "class B { void N() { N(); N(); } }");
        var selected = await Run(dir.Path, "check", "--type", "A");
        Assert.Equal(0, selected.Exit);
        using var json = JsonDocument.Parse(selected.Text);
        Assert.False(json.RootElement.TryGetProperty("types", out _));
        var rows = json.RootElement.GetProperty("memberHotspots").EnumerateArray().ToArray();
        Assert.Equal(new[] { "M(int)", "M(string)" }, rows.Select(r => r.GetProperty("member").GetString()));
        Assert.All(rows, row => Assert.Equal("A", row.GetProperty("type").GetString()));
        var zero = await Run(dir.Path, "analyze", "--top", "0");
        using var empty = JsonDocument.Parse(zero.Text);
        Assert.Empty(empty.RootElement.GetProperty("memberHotspots").EnumerateArray());
        Assert.Empty(empty.RootElement.GetProperty("hotspots").EnumerateArray());
        Assert.DoesNotContain("Member hotspots:", (await Run(dir.Path, "analyze", "--top", "0", "--format", "text")).Text);
    }

    [Fact]
    public async Task PartialMembersAppearOnceAndFileFiltersKeepTheRequestedScope()
    {
        using var dir = new TempDirectory();
        dir.Write("a.cs", "partial class A { void M() { M(); } }");
        dir.Write("b.cs", "partial class A { void N() { N(); } }");
        var first = await Run(dir.Path, "analyze");
        var repeated = await Run(dir.Path, "analyze");
        Assert.Equal(first.Text, repeated.Text);
        using var all = JsonDocument.Parse(first.Text);
        Assert.Equal(2, all.RootElement.GetProperty("memberHotspots").GetArrayLength());
        using var single = JsonDocument.Parse((await Run(dir.Path, "analyze", "b.cs")).Text);
        Assert.Equal("b.cs", Assert.Single(single.RootElement.GetProperty("memberHotspots").EnumerateArray()).GetProperty("file").GetString());
    }

    static async Task<(int Exit, string Text)> Run(string root, params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = await Program.RunAsync([.. args, "--root", root], new StringReader(""), output, error);
        return (exit, output.ToString());
    }
}

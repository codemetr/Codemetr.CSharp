using System.Text.Json;
using Codemetr.CSharp.Tool;

namespace Codemetr.CSharp.Tests;

public sealed class PartialGuardIntegrationTests
{
    [Fact]
    public async Task DeclarationGuardStaysCompatibleAndWholeGuardRequiresOptIn()
    {
        using var directory = new TempDirectory();
        directory.Write("a.cs", "partial class A { public void M() { } }");
        directory.Write("b.cs", "partial class A { public void N() { } }");
        directory.Write("codemetr.json", """{ "rules": { "maxPublicMethods": 1 } }""");
        Assert.Equal(0, (await Run(directory.Path, "check")).Exit);
        directory.Write("codemetr.json", """{ "aggregatePartialTypes": true, "rules": { "maxPublicMethods": 1 } }""");
        var whole = await Run(directory.Path, "check");
        Assert.Equal(1, whole.Exit);
        using var json = JsonDocument.Parse(whole.Out);
        Assert.Equal(["a.cs", "b.cs"], json.RootElement.GetProperty("violations").EnumerateArray().Select(row => row.GetProperty("file").GetString()));
    }

    [Fact]
    public async Task FileSelectionIncludesCounterpartDebtWithoutReclassifyingItOrUnrelatedDebt()
    {
        using var directory = new TempDirectory();
        directory.Write("a.cs", "partial class A { }");
        directory.Write("b.cs", "partial class A { void M(int a) { if (a > 0) { } } } class Unrelated { void N(int a) { if (a > 0) { } } }");
        directory.Write("codemetr.json", """{ "aggregatePartialTypes": true, "rules": { "maxCyclomaticComplexity": 1 } }""");
        Assert.Equal(0, (await Run(directory.Path, "baseline")).Exit);
        var selected = await Run(directory.Path, "check", "a.cs");
        Assert.Equal(0, selected.Exit);
        using var json = JsonDocument.Parse(selected.Out);
        var rows = json.RootElement.GetProperty("violations").EnumerateArray().ToArray();
        var debt = Assert.Single(rows);
        Assert.Equal("b.cs", debt.GetProperty("file").GetString());
        Assert.Equal("baselined", debt.GetProperty("status").GetString());
        Assert.Empty(json.RootElement.GetProperty("stale").EnumerateArray());
    }

    static async Task<(int Exit, string Out)> Run(string root, string command, params string[] args)
    {
        using var output = new StringWriter(); using var error = new StringWriter();
        var exit = await Program.RunAsync([command, "--root", root, .. args], new StringReader(""), output, error);
        return (exit, output.ToString());
    }
}

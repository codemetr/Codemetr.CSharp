using System.Text.Json;
using Codemetr.CSharp.Tool;

namespace Codemetr.CSharp.Tests;

public sealed class MemberExampleTests
{
    [Fact]
    public async Task RunnableExampleMatchesDocumentedCountsAndOptInVerdict()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Codemetr.CSharp.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = await Program.RunAsync(["check", "--root", Path.Combine(root.FullName, "examples/member-size"),
            "--include-metrics"], new StringReader(""), output, error);
        Assert.Equal(1, exit);
        using var json = JsonDocument.Parse(output.ToString());
        var row = json.RootElement.GetProperty("memberHotspots")[0];
        Assert.Equal("Accumulate(int)", row.GetProperty("member").GetString());
        Assert.Equal(9, row.GetProperty("codeLines").GetInt32());
        Assert.Equal(9, row.GetProperty("statementCount").GetInt32());
        Assert.Equal(1, row.GetProperty("cyclomaticComplexity").GetInt32());
        Assert.Equal(new[] { "memberCodeLines", "memberStatements" },
            json.RootElement.GetProperty("violations").EnumerateArray().Select(v => v.GetProperty("rule").GetString()));
    }
}

using System.Text.Json;
using Codemetr.CSharp.Tool;

namespace Codemetr.CSharp.Tests;

public sealed class MutationImportTests
{
    [Theory]
    [InlineData("1")]
    [InlineData("2.0.0")]
    public void ImportedScoreRetainsProvenanceAndKnownStatusCounts(string version)
    {
        using var directory = new TempDirectory();
        var statuses = new[] { "Killed", "Timeout", "Survived", "NoCoverage", "CompileError", "RuntimeError", "Ignored" };
        directory.Write("mutation.json", Report(statuses, version));
        var result = MutationReportReader.Read(directory.Path, "mutation.json");
        Assert.Equal(2, result.Detected);
        Assert.Equal(2, result.Undetected);
        Assert.Equal(3, result.Excluded);
        Assert.Equal(50.0, result.Score);
        Assert.Equal(64, result.Sha256.Length);
        Assert.Contains("not verified", result.Scope);
    }

    [Fact]
    public void EmptyOrPendingReportDoesNotInventAFinalScore()
    {
        using var directory = new TempDirectory();
        directory.Write("empty.json", Report([]));
        Assert.Null(MutationReportReader.Read(directory.Path, "empty.json").Score);
        directory.Write("pending.json", Report(["Killed", "Pending"]));
        Assert.Null(MutationReportReader.Read(directory.Path, "pending.json").Score);
    }

    [Theory]
    [InlineData("Unexpected", "1")]
    [InlineData("Killed", "3")]
    [InlineData("Killed", "1.garbage")]
    public void UnsupportedInputIsRejected(string status, string version)
    {
        using var directory = new TempDirectory();
        directory.Write("bad.json", Report([status], version));
        Assert.Throws<InvalidDataException>(() => MutationReportReader.Read(directory.Path, "bad.json"));
    }

    [Fact]
    public async Task CliIncludesMutationImportInJsonAndRejectsMalformedInput()
    {
        using var directory = new TempDirectory();
        directory.Write("a.cs", "class A { }");
        directory.Write("report.json", Report(["Killed"]));
        using var output = new StringWriter(); using var error = new StringWriter();
        var exit = await Program.RunAsync(["analyze", "--root", directory.Path, "--mutation-report", "report.json"], new StringReader(""), output, error);
        Assert.Equal(0, exit);
        using var json = JsonDocument.Parse(output.ToString());
        Assert.Equal(100.0, json.RootElement.GetProperty("mutation").GetProperty("score").GetDouble());
        directory.Write("report.json", "{}");
        Assert.Equal(2, await Program.RunAsync(["analyze", "--root", directory.Path, "--mutation-report", "report.json"], new StringReader(""), new StringWriter(), new StringWriter()));
    }


    [Fact]
    public void DuplicatePropertiesAndMutantIdsAreRejected()
    {
        using var directory = new TempDirectory();
        directory.Write("duplicate.json", """{ "schemaVersion": "1", "files": {}, "files": {} }""");
        Assert.Throws<InvalidDataException>(() => MutationReportReader.Read(directory.Path, "duplicate.json"));
        directory.Write("ids.json", """{ "schemaVersion": "1", "files": { "a.cs": { "mutants": [{ "id": "1", "status": "Killed" }, { "id": "1", "status": "Survived" }] } } }""");
        Assert.Throws<InvalidDataException>(() => MutationReportReader.Read(directory.Path, "ids.json"));
    }

    [Fact]
    public async Task InvalidImportCannotWriteABaseline()
    {
        using var directory = new TempDirectory();
        directory.Write("a.cs", "class A { }");
        directory.Write("bad.json", "{}");
        var exit = await Program.RunAsync(["baseline", "--root", directory.Path, "--mutation-report", "bad.json"],
            new StringReader(""), new StringWriter(), new StringWriter());
        Assert.Equal(2, exit);
        Assert.False(File.Exists(Path.Combine(directory.Path, "codemetr.baseline.json")));
    }

    static string Report(string[] statuses, string version = "1") => JsonSerializer.Serialize(new
    {
        schemaVersion = version,
        files = new Dictionary<string, object> { ["src/A.cs"] = new { mutants = statuses.Select((status, index) => new { id = index.ToString(), status }) } },
    });
}

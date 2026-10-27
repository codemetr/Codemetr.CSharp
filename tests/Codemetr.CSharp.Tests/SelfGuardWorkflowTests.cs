using System.Text.Json;
using System.Xml.Linq;

namespace Codemetr.CSharp.Tests;

public sealed class SelfGuardWorkflowTests
{
    [Fact]
    public void BothWorkflowsRestorePublishedGuardBeforeUsingIt()
    {
        var root = RepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts/check-metrics.sh"));
        Assert.Contains("dotnet tool restore --configfile .config/codemetr.nuget.config", script);
        Assert.Contains("dotnet tool run codemetr-csharp -- check src --format text", script);
        foreach (var name in new[] { "ci.yml", "release.yml" })
        {
            var workflow = File.ReadAllText(Path.Combine(root, ".github/workflows", name));
            var guard = workflow.IndexOf("run: bash scripts/check-metrics.sh", StringComparison.Ordinal);
            Assert.True(guard >= 0);
            var smoke = workflow.IndexOf("run: dotnet tool run codemetr-csharp -- --help", StringComparison.Ordinal);
            Assert.True(smoke < 0 || smoke > guard);
            Assert.DoesNotContain("--add-source", workflow);
            Assert.DoesNotContain("Codemetr.CSharp.Tool.dll check", workflow);
        }
        var source = XDocument.Load(Path.Combine(root, ".config/codemetr.nuget.config"));
        Assert.Single(source.Descendants("packageSources").Elements("clear"));
        Assert.Equal("https://api.nuget.org/v3/index.json", Assert.Single(source.Descendants("packageSources").Elements("add")).Attribute("value")!.Value);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".config/dotnet-tools.json")));
        Assert.Equal("0.0.1", manifest.RootElement.GetProperty("tools").GetProperty("codemetr.csharp.tool").GetProperty("version").GetString());
    }

    static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Codemetr.CSharp.slnx"))) return directory.FullName;
        throw new InvalidOperationException("Repository root was not found");
    }
}

namespace Codemetr.CSharp.Tool;

/// <summary>Human-readable rendering of a report.</summary>
internal static class TextReport
{
    public static void Write(CodemetrReport report, TextWriter output)
    {
        var s = report.Summary;
        var verdict = report.Command == CodemetrService.Analyze ? $"{s.Violations} violation(s)" : report.Passed ? "PASSED" : "FAILED";
        output.WriteLine($"codemetr-csharp {report.Command}: {verdict} - {s.Types} types in {s.Files} files, "
            + $"{s.New} new, {s.Worsened} worsened, {s.Baselined} baselined, {s.Stale} stale, {s.ParseErrors} parse errors, {s.Skipped} skipped");
        foreach (var warning in report.Warnings ?? []) output.WriteLine("warning: " + warning);
        foreach (var error in report.ParseErrors)
            output.WriteLine($"{error.File}:{error.Line}: parse error {error.Id}: {error.Message}");
        foreach (var v in report.Violations)
        {
            var status = v.Status == Codemetr.CSharp.CheckedViolation.Worsened ? $"worsened from {v.BaselineValue}" : v.Status;
            output.WriteLine($"{v.File}:{v.Line}: [{status}] {v.Rule}: {v.Message}");
            if (v.Status != Codemetr.CSharp.CheckedViolation.Baselined) output.WriteLine($"    hint: {v.Hint}");
        }
        foreach (var entry in report.Stale)
            output.WriteLine($"{entry.File}: [stale] {entry.Rule} {entry.Type}{(entry.Member is null ? "" : "." + entry.Member)}: {entry.Message}");
        if (report.Hotspots.Count > 0)
        {
            output.WriteLine("Hotspots:");
            for (var index = 0; index < report.Hotspots.Count; index++) output.WriteLine($"  {index + 1}. {report.Hotspots[index]}");
        }
        if (report.BaselineWritten is not null) output.WriteLine($"Baseline written: {report.BaselineWritten}");
    }
}

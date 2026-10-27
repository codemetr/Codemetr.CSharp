using System.Globalization;

namespace Codemetr.CSharp.Tool;

/// <summary>Renders advisory measurements without changing the guard verdict.</summary>
internal static class AdvisoryTextReport
{
    public static void Write(CodemetrReport report, TextWriter output)
    {
        if (report.Completeness is { } completeness)
        {
            output.WriteLine($"Analysis scope: {completeness.Scope}");
            output.WriteLine($"Analysis limits: {completeness.CompilationErrorCount} compilation errors, {completeness.UnresolvedTypes.Count} unresolved coupling names, "
                + $"{completeness.UnmeasuredPartialDeclarations.Count} unmeasured partial parts, {completeness.InactiveFiles.Count} files with inactive code");
        }
        foreach (var type in report.WholeTypes ?? [])
        {
            if (type.Declarations.Count > 1)
                output.WriteLine($"Whole partial type {type.FullName}: {type.Declarations.Count(site => site.Measured)} measured parts, "
                    + $"{type.PublicMethodCount} public methods, {type.InstanceFieldCount} fields, coupling {type.ClassCoupling}");
            if (type.Members.Count > 0)
                output.WriteLine($"Cognitive complexity {type.FullName}: max {type.Members.Max(member => member.CognitiveComplexity)} (advisory C# heuristic)");
        }
        if (report.Dependencies is { } graph)
        {
            output.WriteLine($"Dependency scope: {graph.Scope}");
            foreach (var cycle in graph.TypeCycles) output.WriteLine("Type cycle: " + string.Join(" -> ", cycle.Witness));
            foreach (var cycle in graph.NamespaceCycles) output.WriteLine("Namespace cycle: " + string.Join(" -> ", cycle.Witness));
        }
        foreach (var cohesion in report.Cohesion ?? [])
            if (cohesion.Applicable && cohesion.Groups.Count > 1)
                output.WriteLine($"Cohesion {cohesion.Type}: {cohesion.Groups.Count} disconnected instance-method groups (advisory)");
        if (report.Mutation is { } mutation)
            output.WriteLine($"Imported mutation score: {mutation.Score?.ToString("F2", CultureInfo.InvariantCulture) ?? "unavailable"}; "
                + $"detected {mutation.Detected}, undetected {mutation.Undetected}, excluded/pending {mutation.Excluded}; SHA256 {mutation.Sha256}; {mutation.Scope}");
    }
}

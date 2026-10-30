using Codemetr.CSharp;

namespace Codemetr.CSharp.Tool;

/// <summary>Ranks measured declaration members without duplicating whole partial-type rows.</summary>
internal static class MemberHotspots
{
    public static IReadOnlyList<MemberHotspot> Build(IEnumerable<TypeMetrics> types, int top) => types
        .SelectMany(type => type.Members.Select(member => new MemberHotspot(type.FullName, member.Name,
            member.Kind, member.File ?? type.File, member.Line, member.CodeLines, member.StatementCount,
            member.CyclomaticComplexity, member.CognitiveComplexity, member.NestingDepth, member.ParameterCount)))
        .OrderByDescending(member => member.CodeLines).ThenByDescending(member => member.StatementCount)
        .ThenByDescending(member => member.CognitiveComplexity).ThenByDescending(member => member.CyclomaticComplexity)
        .ThenBy(member => member.File, StringComparer.Ordinal).ThenBy(member => member.Type, StringComparer.Ordinal)
        .ThenBy(member => member.Member, StringComparer.Ordinal).ThenBy(member => member.Line)
        .Take(Math.Max(0, top)).ToArray();

    public static void Write(IReadOnlyList<MemberHotspot> members, TextWriter output)
    {
        if (members.Count == 0) return;
        output.WriteLine("Member hotspots:");
        for (var index = 0; index < members.Count; index++)
        {
            var member = members[index];
            output.WriteLine($"  {index + 1}. {member.File}:{member.Line} {member.Type}.{member.Member}: "
                + $"code lines {member.CodeLines}, statements {member.StatementCount}, complexity {member.CyclomaticComplexity}, "
                + $"cognitive {member.CognitiveComplexity}, nesting {member.NestingDepth}, parameters {member.ParameterCount}");
        }
    }
}

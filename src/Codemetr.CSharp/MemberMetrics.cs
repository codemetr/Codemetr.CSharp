namespace Codemetr.CSharp;

/// <summary>Metrics of one executable member (method, constructor, operator, accessor or local function).</summary>
/// <param name="Name">Member name; accessors are <c>Property.get</c>, local functions <c>Outer.Local</c>.</param>
/// <param name="Kind">method, constructor, destructor, operator, conversion, accessor or localFunction.</param>
/// <param name="Line">1-based start line.</param>
/// <param name="Lines">Line span of the declaration.</param>
/// <param name="CyclomaticComplexity">McCabe complexity: 1 plus one per decision point.</param>
/// <param name="ParameterCount">Declared parameters.</param>
/// <param name="NestingDepth">Deepest control-flow nesting; a statement directly in the body is depth 0.</param>
public sealed record MemberMetrics(
    string Name,
    string Kind,
    int Line,
    int Lines,
    int CyclomaticComplexity,
    int ParameterCount,
    int NestingDepth);

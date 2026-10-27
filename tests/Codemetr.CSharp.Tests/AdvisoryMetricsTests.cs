namespace Codemetr.CSharp.Tests;

public sealed class AdvisoryMetricsTests
{
    [Theory]
    [InlineData("if (a) { } if (b) { }", 2)]
    [InlineData("if (a) { if (b) { } }", 3)]
    [InlineData("if (a) { } else if (b) { } else { }", 3)]
    [InlineData("if (a && b && c) { }", 2)]
    [InlineData("if (a && b || c) { }", 3)]
    [InlineData("switch (a) { case true: break; default: break; }", 1)]
    [InlineData("void Local() { if (a) { } }", 0)]
    public void CognitiveHeuristicHasHandCalculatedScores(string body, int expected)
    {
        var member = Source.Type($"class A {{ void M(bool a, bool b, bool c) {{ {body} }} }}", "A").Members.Single(member => member.Name == "M(bool, bool, bool)");
        Assert.Equal(expected, member.CognitiveComplexity);
    }

    [Fact]
    public void DependencyCyclesHaveRealEdgesAndRespectFilteredScope()
    {
        const string code = "namespace X { class A { Y.B b; } } namespace Y { class B { X.A a; } }";
        var result = Source.Analyze(code);
        var graph = result.Dependencies!;
        var cycle = Assert.Single(graph.TypeCycles);
        Assert.Equal(["X.A", "Y.B"], cycle.Nodes);
        Assert.Equal(["X.A", "Y.B", "X.A"], cycle.Witness);
        Assert.Single(graph.NamespaceCycles);
        Assert.All(graph.Types, type => { Assert.Equal(1, type.FanIn); Assert.Equal(1, type.FanOut); });
        var selected = CodeAnalyzer.Analyze([new SourceDocument("a.cs", code)], typeQuery: "A").Dependencies!;
        Assert.Empty(selected.TypeCycles);
        Assert.Equal(["Y.B"], Assert.Single(selected.Types).ContextTypes);
        Assert.Equal(0, selected.Types[0].FanIn);
    }

    [Fact]
    public void MissingAndAmbiguousReferencesDoNotCreateGraphEdges()
    {
        var missing = Source.Analyze("class A { Missing x; }").Dependencies!;
        Assert.Equal(["?Missing"], Assert.Single(missing.Types).Unresolved);
        Assert.Empty(missing.Types[0].Outgoing);
        var duplicate = Source.Analyze("class A { } class A { } class B { A a; }").Dependencies!;
        Assert.Equal(["A"], duplicate.AmbiguousTypes);
        Assert.Empty(Assert.Single(duplicate.Types).Outgoing);
    }

    [Fact]
    public void CohesionUsesThisFieldsAndInstanceCallsButNotOtherInstances()
    {
        var shared = Source.Analyze("class A { int x; void M() { x++; } void N() { x--; } }").Cohesion.Single();
        Assert.True(shared.Applicable);
        Assert.Single(shared.Groups);
        var separate = Source.Analyze("class A { int x, y; void M() { x++; } void N() { y++; } }").Cohesion.Single();
        Assert.Equal(2, separate.Groups.Count);
        var other = Source.Analyze("class A { int x; void M(A other) { other.x++; } void N() { x++; } }").Cohesion.Single();
        Assert.Equal(2, other.Groups.Count);
        Assert.Single(Source.Analyze("class A { int x; void M() { N(); } void N() { x++; } }").Cohesion.Single().Groups);
        Assert.False(Source.Analyze("class A { void M() { } void N() { } }").Cohesion.Single().Applicable);
        Assert.False(Source.Analyze("record A(int Value);").Cohesion.Single().Applicable);
    }
    [Fact]
    public void GenericAndNestedSourceTypesRetainTheirGraphIdentity()
    {
        var result = Source.Analyze("class Outer<T> { public class Inner { Outer<T> parent; } Inner child; }");
        Assert.Equal(["Outer<T>", "Outer<T>.Inner"], result.Dependencies!.TypeCycles.Single().Nodes);
        var filtered = result.SelectTypes("Inner").Dependencies!;
        Assert.Single(filtered.Types);
        Assert.Empty(filtered.TypeCycles);
    }

    [Fact]
    public void CycleTraversalHandlesLargeGraphsWithoutRecursiveStackGrowth()
    {
        var graph = Enumerable.Range(0, 5000).ToDictionary(index => index.ToString(),
            index => new[] { ((index + 1) % 5000).ToString() }, StringComparer.Ordinal);
        var cycle = Assert.Single(StrongComponents.Cycles(graph));
        Assert.Equal(5000, cycle.Nodes.Count);
        Assert.Equal(5001, cycle.Witness.Count);
        for (var index = 1; index < cycle.Witness.Count; index++)
            Assert.Contains(cycle.Witness[index], graph[cycle.Witness[index - 1]]);
    }

    [Fact]
    public void CohesionDistinguishesOverloadsShadowingAndNameof()
    {
        var overloaded = Source.Analyze("class A { int x; void M() { N(1); } void N(int v) { x++; } void N(string v) { } }").Cohesion.Single();
        Assert.Equal(2, overloaded.Groups.Count);
        var shadowed = Source.Analyze("class A { int x; void M() { int x = 1; x++; } void N() { x++; } }").Cohesion.Single();
        Assert.Equal(2, shadowed.Groups.Count);
        var named = Source.Analyze("class A { int x; void M() { var name = nameof(x); } void N() { x++; } }").Cohesion.Single();
        Assert.Equal(2, named.Groups.Count);
    }

}

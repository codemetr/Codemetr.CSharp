namespace Codemetr.CSharp.Tests;

public sealed class TypeMetricsTests
{
    [Fact]
    public void EveryTypeInAFileIsMeasuredOnItsOwnIncludingNestedTypes()
    {
        const string code = """
            namespace Shop;
            public class Order
            {
                public void A() { if (true) { } }
                public void B() { }
                private int field1, field2;
                private static int shared;
                private const int Limit = 1;
                public sealed class Line { public void C() { if (true) { } if (false) { } } private int x; }
            }
            public record struct Money(decimal Amount);
            public interface IStore { void Save(); }
            public enum Kind { A, B }
            """;
        var types = Source.Analyze(code).Types;
        Assert.Equal(["Shop.Order", "Shop.Order.Line", "Shop.Money", "Shop.IStore", "Shop.Kind"], types.Select(t => t.FullName));
        Assert.Equal(["class", "class", "record struct", "interface", "enum"], types.Select(t => t.Kind));
        var order = types[0];
        Assert.Equal(2, order.PublicMethodCount);
        Assert.Equal(2, order.InstanceFieldCount);
        Assert.Equal(2, order.MaxCyclomaticComplexity);
        Assert.DoesNotContain(order.Members, m => m.Name == "C");
        Assert.Equal(3, types[1].MaxCyclomaticComplexity);
        Assert.Equal(1, types[1].InstanceFieldCount);
    }

    [Fact]
    public void AccessorsConstructorsAndOperatorsAreMembers()
    {
        const string code = """
            class C
            {
                int v;
                static C() { }
                public C(int a, int b) { v = a + b; }
                public int P { get { return v > 0 ? v : 0; } set { v = value; } }
                public int Q => v;
                public int Auto { get; set; }
                public int this[int i] => i;
                public static C operator +(C a, C b) => a;
            }
            """;
        var members = Source.Type(code, "C").Members.Select(m => (m.Name, m.Kind)).ToArray();
        Assert.Equal([(".cctor()", "constructor"), (".ctor(int, int)", "constructor"), ("P.get", "accessor"), ("P.set", "accessor"),
            ("Q.get", "accessor"), ("this[int].get", "accessor"), ("operator +(C, C)", "operator")], members);
        Assert.Equal(2, Source.Type(code, "C").ConstructorParameterCount);
    }

    [Fact]
    public void PrimaryConstructorCountsForClassesButNotRecords()
    {
        Assert.Equal(3, Source.Type("class C(int a, int b, int c) { }", "C").ConstructorParameterCount);
        Assert.Equal(0, Source.Type("record R(int A, int B, int C, int D, int E, int F, int G, int H);", "R").ConstructorParameterCount);
    }

    [Fact]
    public void InheritanceDepthFollowsSourceAndPlatformBaseClasses()
    {
        const string code = """
            class A { }
            class B : A { }
            class C : B, System.IDisposable { public void Dispose() { } }
            class E : System.InvalidOperationException { }
            class U : MissingBase { }
            class J : IUnknown { }
            interface I : System.IDisposable { }
            """;
        var types = Source.Analyze(code).Types.ToDictionary(t => t.Name, t => t.DepthOfInheritance);
        Assert.Equal(0, types["A"]);
        Assert.Equal(2, types["C"]);
        Assert.Equal(3, types["E"]); // InvalidOperationException, SystemException, Exception (System.Object excluded)
        Assert.Equal(1, types["U"]);
        Assert.Equal(0, types["J"]); // unresolved IUnknown is taken for an interface, not a base class
        Assert.Equal(0, types["I"]);
    }

    [Fact]
    public void MaintainabilityIndexFallsWithSizeAndBranches()
    {
        var small = Source.Type("class S { int M(int a) => a; }", "S").MaintainabilityIndex;
        var body = string.Join("\n", Enumerable.Range(0, 80).Select(i => $"if (a == {i}) {{ b += a * {i} - c / {i + 1}; c = b ^ {i}; }}"));
        var large = Source.Type($"class L {{ int M(int a) {{ int b = 0, c = 1; {body} return b + c; }} }}", "L").MaintainabilityIndex;
        Assert.InRange(small, 0, 100);
        Assert.InRange(large, 0, 100);
        Assert.True(large < small - 30, $"large {large} small {small}");
    }

    [Fact]
    public void SyntaxErrorsAreReportedAndNotMeasured()
    {
        var result = CodeAnalyzer.Analyze([new SourceDocument("bad.cs", "class Broken { void M( { }"), new SourceDocument("ok.cs", "class Ok { }")]);
        Assert.Equal(["Ok"], result.Types.Select(t => t.Name));
        var error = Assert.Single(result.ParseErrors);
        Assert.Equal("bad.cs", error.File);
        Assert.StartsWith("CS", error.Id);
    }

    [Fact]
    public void TypeSelectionMatchesSimpleFullAndSuffixNames()
    {
        var result = Source.Analyze("namespace A.B { class Target { class Inner { } } class Other { } }");
        Assert.Equal(["A.B.Target"], result.SelectTypes("Target").Types.Select(t => t.FullName));
        Assert.Equal(["A.B.Target.Inner"], result.SelectTypes("Target.Inner").Types.Select(t => t.FullName));
        Assert.Equal(["A.B.Other"], result.SelectTypes("A.B.Other").Types.Select(t => t.FullName));
        Assert.Empty(result.SelectTypes("B.Missing").Types);
    }
}

namespace Codemetr.CSharp.Tests;

public sealed class CouplingTests
{
    [Fact]
    public void CountsStaticCallsInferredTypesAttributesAndCreatedTypes()
    {
        const string code = """
            using System.Text;
            [System.Obsolete]
            class C
            {
                string M(string path)
                {
                    var text = File.ReadAllText(path);
                    var builder = new StringBuilder(text);
                    return builder.ToString().Trim();
                }
            }
            """;
        var coupled = Source.Type(code, "C").CoupledTypes;
        Assert.Contains("System.IO.File", coupled);
        Assert.Contains("System.Text.StringBuilder", coupled);
        Assert.Contains("System.ObsoleteAttribute", coupled);
        Assert.DoesNotContain(coupled, name => name.Contains("String", StringComparison.Ordinal) && !name.Contains("Builder", StringComparison.Ordinal));
    }

    [Fact]
    public void ExcludesSelfBuiltInsTypeParametersAndTuples()
    {
        const string code = """
            class C<T>
            {
                C<T>? next;
                (int, string) Pair() => (1, "a");
                T Echo(T value, object o, int i, decimal d) => value;
                static C<T> Make() => new C<T>();
            }
            """;
        Assert.Empty(Source.Type(code, "C").CoupledTypes);
    }

    [Fact]
    public void InstanceMembersCoupleToTheReceiverTypeNotTheDeclaringBase()
    {
        var coupled = Source.Type("class C { string N(System.Exception e) => e.GetType().Name; }", "C").CoupledTypes;
        Assert.Contains("System.Type", coupled);
        Assert.DoesNotContain("System.Reflection.MemberInfo", coupled);
    }

    [Fact]
    public void UnresolvedTypesAreKeptWithAQuestionMark()
    {
        const string code = """
            class C
            {
                Widget? w;
                void M() { var x = Factory.Create(); Assert.True(x); }
            }
            """;
        Assert.Equal(["?Assert", "?Factory", "?Widget"], Source.Type(code, "C").CoupledTypes);
    }

    [Fact]
    public void InheritedMembersOfAnUnresolvedBaseAreNotTakenForTypes()
    {
        const string code = """
            class OrdersController : ControllerBase
            {
                object Get(Widget w) { if (!ModelState.IsValid) return BadRequest(); return Request.Path; }
            }
            """;
        Assert.Equal(["?ControllerBase", "?Widget"], Source.Type(code, "OrdersController").CoupledTypes);
    }

    [Fact]
    public void NullableNameofAndConditionalAccessFollowTheReceiver()
    {
        const string code = """
            class Base { public int Total; }
            class Order : Base { }
            class C
            {
                int A(int? n) => n.HasValue ? n.Value : 0;
                string B() => nameof(System.IO.File);
                int D(Order? o) => o?.Total ?? 0;
            }
            """;
        Assert.Equal(["Order"], Source.Type(code, "C").CoupledTypes);
    }

    [Fact]
    public void TypesFromOtherAnalysedDocumentsResolve()
    {
        var result = CodeAnalyzer.Analyze([
            new SourceDocument("a.cs", "namespace N; public class Repo { public static Repo Open() => new(); }"),
            new SourceDocument("b.cs", "namespace N; class Service { void M() { var r = Repo.Open(); } }")]);
        Assert.Equal(["N.Repo"], result.Types.Single(t => t.Name == "Service").CoupledTypes);
    }

    [Fact]
    public void GenericCollectionInterfacesAndOtherFrameworkTypesCount()
    {
        const string code = """
            using System.Collections.Generic;
            class Order { }
            class C
            {
                IEnumerable<Order> A() => [];
                IReadOnlyList<Order> B() => [];
                void D(IList<int> a, ICollection<int> b, System.IDisposable d, System.DateTime t, int? n, string s, object o) { }
            }
            """;
        Assert.Equal(["Order", "System.Collections.Generic.ICollection<T>", "System.Collections.Generic.IEnumerable<T>",
            "System.Collections.Generic.IList<T>", "System.Collections.Generic.IReadOnlyList<T>", "System.DateTime", "System.IDisposable"],
            Source.Type(code, "C").CoupledTypes);
    }

    [Fact]
    public void AnUnresolvedInterfaceDoesNotDisableTheReceiverHeuristic()
    {
        const string code = "class H : IRequestHandler<int> { void M() { Guard.Check(); } }";
        var type = Source.Type(code, "H");
        Assert.Equal(0, type.DepthOfInheritance);
        Assert.Contains("?Guard", type.CoupledTypes);
    }

    [Fact]
    public void DuplicateDeclarationsInTheContextAreReported()
    {
        var measured = new SourceDocument("b/Options.cs", "namespace N; class Options { }");
        var other = new SourceDocument("a/Options.cs", "namespace N; class Options : Base { } class Base { }");
        var result = CodeAnalyzer.Analyze([measured], [other, measured]);
        Assert.Contains(result.Warnings, warning => warning.Contains("N.Options", StringComparison.Ordinal) && warning.Contains("a/Options.cs", StringComparison.Ordinal));
        Assert.Empty(CodeAnalyzer.Analyze([new SourceDocument("p.cs", "partial class P { } partial class P { }")]).Warnings);
    }
}

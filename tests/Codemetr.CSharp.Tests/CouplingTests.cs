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
            class C : ExternalBase
            {
                Widget? w;
                void M() { var x = Factory.Create(); Assert.True(x); }
            }
            """;
        var coupled = Source.Type(code, "C").CoupledTypes;
        Assert.Equal(["?Assert", "?ExternalBase", "?Factory", "?Widget"], coupled);
    }

    [Fact]
    public void TypesFromOtherAnalysedDocumentsResolve()
    {
        var result = CodeAnalyzer.Analyze([
            new SourceDocument("a.cs", "namespace N; public class Repo { public static Repo Open() => new(); }"),
            new SourceDocument("b.cs", "namespace N; class Service { void M() { var r = Repo.Open(); } }")]);
        Assert.Equal(["N.Repo"], result.Types.Single(t => t.Name == "Service").CoupledTypes);
    }
}

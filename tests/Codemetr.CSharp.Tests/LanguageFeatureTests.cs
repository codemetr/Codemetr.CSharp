namespace Codemetr.CSharp.Tests;

public sealed class LanguageFeatureTests
{
    [Fact]
    public void TopLevelStatementsAreMeasuredAsProgram()
    {
        const string code = """
            var args2 = System.Environment.GetCommandLineArgs();
            if (args2.Length > 1) System.Console.WriteLine(Twice(args2.Length));
            foreach (var a in args2) { if (a == "x") return 1; }
            return 0;
            static int Twice(int n) => n > 0 ? n * 2 : 0;
            """;
        var program = Assert.Single(Source.Analyze(code).Types);
        Assert.Equal(("Program", "top-level"), (program.FullName, program.Kind));
        Assert.Equal(4, program.Members.Single(m => m.Kind == "topLevel").CyclomaticComplexity);
        Assert.Equal(2, program.Members.Single(m => m.Name == "Twice(int)").CyclomaticComplexity);
        Assert.Contains("System.Console", program.CoupledTypes);
    }

    [Fact]
    public void ExtensionBlocksBelongToTheirStaticClassAndCallersCoupleToIt()
    {
        const string code = """
            namespace N;
            public static class StringRules
            {
                extension(string s)
                {
                    public bool Odd() => s.Length % 2 == 1 && s.Length > 0;
                    public int Twice => s.Length * 2;
                }
            }
            class Use { bool M(string x) => x.Odd(); }
            """;
        var types = Source.Analyze(code).Types;
        Assert.Equal(["N.StringRules", "N.Use"], types.Select(t => t.FullName));
        var rules = types[0];
        Assert.Equal(["extension(string).Odd()", "extension(string).Twice.get"], rules.Members.Select(m => m.Name));
        Assert.Equal(1, rules.PublicMethodCount);
        Assert.Equal(["N.StringRules"], types[1].CoupledTypes);
    }

    [Fact]
    public void InterfaceMethodsArePublicUnlessDeclaredOtherwise()
    {
        var type = Source.Type("interface IWide { void A(); void B(); private void C() { } int D => 1; }", "IWide");
        Assert.Equal(2, type.PublicMethodCount);
    }

    [Fact]
    public void ContextDocumentsResolveTypesWithoutBeingMeasured()
    {
        var measured = new SourceDocument("b.cs", "namespace N; class Service : Repo { void M() { var r = Repo.Open(); } }");
        var context = new SourceDocument("a.cs", "namespace N; public class Repo { public static Repo Open() => new(); }");
        var withContext = CodeAnalyzer.Analyze([measured], [context, measured]);
        var service = Assert.Single(withContext.Types);
        Assert.Equal(["N.Repo"], service.CoupledTypes);
        Assert.Equal(1, service.DepthOfInheritance);
        Assert.Equal(["?Repo"], Assert.Single(CodeAnalyzer.Analyze([measured]).Types).CoupledTypes);
    }

    [Fact]
    public void TypeQueryMeasuresOnlyMatchingDeclarations()
    {
        var result = CodeAnalyzer.Analyze([new SourceDocument("a.cs", "namespace A; class Repo<T> { class Inner { } } class Other { }")], typeQuery: "A.Repo");
        Assert.Equal(["A.Repo<T>"], result.Types.Select(t => t.FullName));
        Assert.Equal(["A.Repo<T>.Inner"], CodeAnalyzer.Analyze([new SourceDocument("a.cs", "namespace A; class Repo<T> { class Inner { } }")], typeQuery: "Repo.Inner").Types.Select(t => t.FullName));
    }
}

namespace Codemetr.CSharp.Tests;

public sealed class MemberSizeTests
{
    [Fact]
    public void CountsBodyCodeInsteadOfSignaturesCommentsOrBraces()
    {
        const string code = """
            class C
            {
                int M(
                    int x)
                {
                    // comment
                    int y = x;

                    y++;
                    return y;
                }
            }
            """;
        var member = Source.Member(code, "C", "M");
        Assert.Equal(3, member.CodeLines);
        Assert.Equal(3, member.StatementCount);
        Assert.Equal(9, member.Lines);
    }

    [Theory]
    [InlineData("void M() { }", 0, 0)]
    [InlineData("void M() { ; }", 1, 0)]
    [InlineData("int M() => 42;", 1, 1)]
    [InlineData("void M() { int x = 0; x++; x++; }", 1, 3)]
    [InlineData("void M() { if (true) { M(); } else { M(); } }", 1, 3)]
    [InlineData("void M() { System.Action a = () => { M(); M(); }; }", 1, 3)]
    public void HasHandCalculatedBodyCounts(string declaration, int lines, int statements)
    {
        var member = Source.Member("class C { " + declaration + " }", "C", "M");
        Assert.Equal(lines, member.CodeLines);
        Assert.Equal(statements, member.StatementCount);
    }

    [Fact]
    public void LocalFunctionsHaveTheirOwnBodiesWithoutCountingTheirDeclarationsTwice()
    {
        const string code = """
            class C {
              void M() {
                void Local() {
                  int x = 0;
                  x++;
                }
                Local();
              }
            }
            """;
        var members = Source.Type(code, "C").Members;
        Assert.Equal((1, 1), (members[0].CodeLines, members[0].StatementCount));
        Assert.Equal((2, 2), (members[1].CodeLines, members[1].StatementCount));
    }

    [Fact]
    public void MeasuresAccessorsConstructorsOperatorsAndExtensionBodies()
    {
        const string code = """
            class C {
              C() { System.Console.WriteLine(); }
              ~C() { System.Console.WriteLine(); }
              int P => 1;
              int Q { get { return 1; } set { System.Console.WriteLine(value); } }
              int Auto { get; set; }
              public static C operator +(C a, C b) => a;
            }
            static class E { extension(string s) { int Length() => s.Length; } }
            """;
        var members = Source.Analyze(code).Types.SelectMany(t => t.Members).ToArray();
        Assert.Equal(7, members.Length);
        Assert.All(members, m => Assert.Equal((1, 1), (m.CodeLines, m.StatementCount)));
    }

    [Fact]
    public void TopLevelAndNestedLocalFunctionsRetainTheirOwnCounts()
    {
        const string code = """
            System.Console.WriteLine();
            void Outer() {
              void Inner() { System.Console.WriteLine(); }
              Inner();
            }
            Outer();
            """;
        var members = Assert.Single(Source.Analyze(code).Types).Members;
        Assert.Equal(3, members.Count);
        Assert.Equal((2, 2), (members[0].CodeLines, members[0].StatementCount));
        Assert.All(members.Skip(1), m => Assert.Equal((1, 1), (m.CodeLines, m.StatementCount)));
    }

    [Fact]
    public void TopLevelBlockLocalsAreNotLostAndExternLocalsAreNotExecutable()
    {
        var top = Source.Analyze("if (true) { void Local() { System.Console.WriteLine(); } Local(); }");
        Assert.Equal(2, Assert.Single(top.Types).Members.Count);
        Assert.Equal((1, 1), (top.Types[0].Members[1].CodeLines, top.Types[0].Members[1].StatementCount));
        Assert.Single(Source.Type("class C { void M() { extern void Native(); } }", "C").Members);
    }

    [Fact]
    public void MultilineTokensOccupyAllTheirPhysicalLines()
    {
        var member = Source.Member("class C { string M() => @\"first\n\nlast\"; }", "C", "M");
        Assert.Equal(3, member.CodeLines);
        Assert.Equal(1, member.StatementCount);
    }
}

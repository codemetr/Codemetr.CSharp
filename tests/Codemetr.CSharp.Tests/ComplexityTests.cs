namespace Codemetr.CSharp.Tests;

public sealed class ComplexityTests
{
    [Fact]
    public void StraightLineMethodHasComplexityOne()
    {
        Assert.Equal(1, Source.Member("class C { int M(int a) { var b = a + 1; return b * 2; } }", "C", "M").CyclomaticComplexity);
    }

    [Fact]
    public void EachBranchAndShortCircuitCountsOnce()
    {
        const string code = """
            class C
            {
                int M(int a, string? s, int[] xs, (int, int)[] pairs)
                {
                    if (a > 0 && a < 10) return 1;          // if + &&
                    else if (a < 0 || a > 100) return 2;    // if + ||
                    for (var i = 0; i < a; i++) { }         // for
                    foreach (var x in xs) { }               // foreach
                    foreach (var (p, q) in pairs) { }       // foreach deconstruction
                    while (a-- > 0) { }                     // while
                    do { } while (a > 5);                   // do
                    var n = s?.Length ?? 0;                 // ?. + ??
                    s ??= "x";                              // ??=
                    var t = a > 3 ? 1 : 2;                  // ?:
                    var u = a is 1 or 2;                    // or pattern
                    try { } catch (System.Exception) when (a > 1) { }  // catch + filter
                    return n + t;
                }
            }
            """;
        Assert.Equal(1 + 2 + 2 + 1 + 1 + 1 + 1 + 1 + 2 + 1 + 1 + 1 + 2, Source.Member(code, "C", "M").CyclomaticComplexity);
    }

    [Fact]
    public void SwitchCountsCasesButNotDefaultOrDiscardArm()
    {
        const string code = """
            class C
            {
                int S(int a) { switch (a) { case 1: return 1; case 2: case 3: return 2; case > 9 when a < 20: return 3; default: return 0; } }
                int E(int a) => a switch { 1 => 1, 2 => 2, _ => 0 };
            }
            """;
        Assert.Equal(1 + 3 + 1 + 1, Source.Member(code, "C", "S").CyclomaticComplexity);
        Assert.Equal(1 + 2, Source.Member(code, "C", "E").CyclomaticComplexity);
    }

    [Fact]
    public void LocalFunctionsAreMeasuredSeparatelyAndLambdasStayWithTheirMember()
    {
        const string code = """
            class C
            {
                int M(int a)
                {
                    System.Func<int, int> f = x => x > 0 ? x : -x;
                    return Local(a) + f(a);
                    int Local(int b) { if (b > 1) return b; if (b < -1) return -b; return 0; }
                }
            }
            """;
        var type = Source.Type(code, "C");
        Assert.Equal(2, type.Members.Single(m => m.Name == "M").CyclomaticComplexity);
        Assert.Equal(3, type.Members.Single(m => m.Name == "M.Local").CyclomaticComplexity);
        Assert.Equal("localFunction", type.Members.Single(m => m.Name == "M.Local").Kind);
        Assert.Equal("M.Local", type.MostComplexMember);
    }

    [Fact]
    public void NestingDepthKeepsElseIfChainsFlat()
    {
        const string code = """
            class C
            {
                void Flat(int a) { if (a == 1) { } else if (a == 2) { } else if (a == 3) { } else { } }
                void Deep(int[] xs) { foreach (var x in xs) { if (x > 0) { while (x > 1) { try { } catch { } } } } }
                void None() { System.Console.WriteLine(); }
            }
            """;
        Assert.Equal(1, Source.Member(code, "C", "Flat").NestingDepth);
        Assert.Equal(4, Source.Member(code, "C", "Deep").NestingDepth);
        Assert.Equal(0, Source.Member(code, "C", "None").NestingDepth);
    }
}

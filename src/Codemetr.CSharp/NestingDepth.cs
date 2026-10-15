using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>Deepest control-flow nesting. An else-if chain stays on the level of its first if.</summary>
internal static class NestingDepth
{
    public static int Measure(SyntaxNode root) => Deepest(root, 0);

    static int Deepest(SyntaxNode node, int depth)
    {
        var deepest = depth;
        foreach (var child in node.ChildNodes())
        {
            if (OwnSyntax.IsSeparatelyMeasured(child)) continue;
            var childDepth = depth + (Nests(node, child) ? 1 : 0);
            deepest = Math.Max(deepest, Deepest(child, childDepth));
        }
        return deepest;
    }

    static bool Nests(SyntaxNode parent, SyntaxNode child) => parent switch
    {
        IfStatementSyntax ifStatement => child == ifStatement.Statement,
        ElseClauseSyntax elseClause => child == elseClause.Statement && child is not IfStatementSyntax,
        ForStatementSyntax loop => child == loop.Statement,
        CommonForEachStatementSyntax loop => child == loop.Statement,
        WhileStatementSyntax loop => child == loop.Statement,
        DoStatementSyntax loop => child == loop.Statement,
        SwitchStatementSyntax => child is SwitchSectionSyntax,
        TryStatementSyntax tryStatement => child == tryStatement.Block || child is CatchClauseSyntax or FinallyClauseSyntax,
        _ => false,
    };
}

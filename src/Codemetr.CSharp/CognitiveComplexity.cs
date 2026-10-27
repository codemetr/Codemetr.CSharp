using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>Documented C# heuristic for flow interruption and nesting, not Sonar rule compatibility.</summary>
internal static class CognitiveComplexity
{
    public static int Measure(SyntaxNode root) => Visit(root, 0);

    static int Visit(SyntaxNode node, int depth)
    {
        var score = Increment(node, depth);
        foreach (var child in node.ChildNodes())
        {
            if (OwnSyntax.IsSeparatelyMeasured(child)) continue;
            score += Visit(child, depth + (Nests(node, child) ? 1 : 0));
        }
        return score;
    }

    static int Increment(SyntaxNode node, int depth) => node switch
    {
        IfStatementSyntax { Parent: ElseClauseSyntax } => 1,
        IfStatementSyntax or ForStatementSyntax or CommonForEachStatementSyntax or WhileStatementSyntax
            or DoStatementSyntax or CatchClauseSyntax or SwitchStatementSyntax or SwitchExpressionSyntax
            or ConditionalExpressionSyntax => 1 + depth,
        ElseClauseSyntax { Statement: not IfStatementSyntax } => 1,
        GotoStatementSyntax => 1,
        BinaryExpressionSyntax binary when IsLogical(binary) => BeginsSequence(binary) ? 1 : 0,
        BinaryPatternSyntax pattern => pattern.Parent is BinaryPatternSyntax parent && parent.RawKind == pattern.RawKind ? 0 : 1,
        _ => 0,
    };

    static bool Nests(SyntaxNode parent, SyntaxNode child) => parent switch
    {
        IfStatementSyntax statement => child == statement.Statement,
        ElseClauseSyntax clause => child == clause.Statement && child is not IfStatementSyntax,
        ForStatementSyntax loop => child == loop.Statement,
        CommonForEachStatementSyntax loop => child == loop.Statement,
        WhileStatementSyntax loop => child == loop.Statement,
        DoStatementSyntax loop => child == loop.Statement,
        CatchClauseSyntax clause => child == clause.Block,
        SwitchStatementSyntax => child is SwitchSectionSyntax,
        SwitchExpressionSyntax => child is SwitchExpressionArmSyntax,
        ConditionalExpressionSyntax expression => child != expression.Condition,
        AnonymousFunctionExpressionSyntax => true,
        _ => false,
    };

    static bool IsLogical(BinaryExpressionSyntax binary) => binary.IsKind(SyntaxKind.LogicalAndExpression)
        || binary.IsKind(SyntaxKind.LogicalOrExpression);

    static bool BeginsSequence(BinaryExpressionSyntax binary)
    {
        SyntaxNode outer = binary;
        while (outer.Parent is ParenthesizedExpressionSyntax) outer = outer.Parent;
        return outer.Parent is not BinaryExpressionSyntax parent || !IsLogical(parent) || parent.RawKind != binary.RawKind;
    }
}

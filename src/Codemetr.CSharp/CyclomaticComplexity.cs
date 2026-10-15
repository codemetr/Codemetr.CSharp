using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>McCabe complexity from syntax: 1 plus one per branch or short-circuit decision.</summary>
internal static class CyclomaticComplexity
{
    public static int Measure(SyntaxNode root) => 1 + OwnSyntax.Nodes(root).Count(IsDecision);

    // `_` and `var x` match everything: the fall-through arm, not a decision.
    static bool IsDiscard(PatternSyntax pattern) =>
        pattern is DiscardPatternSyntax or VarPatternSyntax;

    static bool IsDecision(SyntaxNode node) => node switch
    {
        IfStatementSyntax or ForStatementSyntax or CommonForEachStatementSyntax or WhileStatementSyntax
            or DoStatementSyntax or CatchClauseSyntax or CaseSwitchLabelSyntax or CasePatternSwitchLabelSyntax
            or ConditionalExpressionSyntax or ConditionalAccessExpressionSyntax or WhenClauseSyntax
            or CatchFilterClauseSyntax => true,
        // A trailing discard arm is the fall-through path, not an extra decision.
        SwitchExpressionArmSyntax arm => !IsDiscard(arm.Pattern) || arm.WhenClause is not null,
        BinaryExpressionSyntax binary => binary.IsKind(SyntaxKind.LogicalAndExpression)
            || binary.IsKind(SyntaxKind.LogicalOrExpression) || binary.IsKind(SyntaxKind.CoalesceExpression),
        AssignmentExpressionSyntax assignment => assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression),
        BinaryPatternSyntax => true,
        _ => false,
    };
}

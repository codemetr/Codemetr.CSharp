using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>
/// Walks a node without entering nested types or local functions; those are measured on their own.
/// C# 14 extension blocks are part of their enclosing static class.
/// </summary>
internal static class OwnSyntax
{
    public static IEnumerable<SyntaxNode> Nodes(SyntaxNode root) =>
        root.DescendantNodes(node => node == root || !IsSeparatelyMeasured(node))
            .Where(node => !IsSeparatelyMeasured(node));

    public static IEnumerable<SyntaxToken> Tokens(SyntaxNode root) =>
        root.DescendantTokens(node => node == root || !IsSeparatelyMeasured(node));

    public static bool IsSeparatelyMeasured(SyntaxNode node) =>
        node is LocalFunctionStatementSyntax || (node is BaseTypeDeclarationSyntax && node is not ExtensionBlockDeclarationSyntax);
}

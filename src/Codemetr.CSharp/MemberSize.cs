using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>Counts body token lines and syntactic statements with the existing member ownership boundaries.</summary>
internal static class MemberSize
{
    public static (int CodeLines, int StatementCount) Measure(IEnumerable<SyntaxNode> declarations)
    {
        var lines = new HashSet<int>();
        var statements = 0;
        foreach (var declaration in declarations)
        {
            var body = Body(declaration);
            foreach (var token in OwnSyntax.Tokens(body).Where(token => token.Span.Length > 0
                         && !token.IsKind(SyntaxKind.OpenBraceToken) && !token.IsKind(SyntaxKind.CloseBraceToken)))
            {
                var span = token.GetLocation().GetLineSpan();
                for (var line = span.StartLinePosition.Line; line <= span.EndLinePosition.Line; line++) lines.Add(line);
            }
            statements += OwnSyntax.Nodes(body).Prepend(body).Count(node => node is StatementSyntax
                and not BlockSyntax and not EmptyStatementSyntax and not LocalFunctionStatementSyntax);
            if (body is ExpressionSyntax) statements++;
        }
        return (lines.Count, statements);
    }

    static SyntaxNode Body(SyntaxNode declaration) => declaration switch
    {
        BaseMethodDeclarationSyntax method => (SyntaxNode?)method.Body ?? method.ExpressionBody!.Expression,
        LocalFunctionStatementSyntax local => (SyntaxNode?)local.Body ?? local.ExpressionBody!.Expression,
        AccessorDeclarationSyntax accessor => (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody!.Expression,
        PropertyDeclarationSyntax property => property.ExpressionBody!.Expression,
        IndexerDeclarationSyntax indexer => indexer.ExpressionBody!.Expression,
        GlobalStatementSyntax global => global.Statement,
        _ => declaration,
    };
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Codemetr.CSharp;

/// <summary>
/// Halstead volume N * log2(n). Identifiers and literals are operands; keywords and punctuation operators.
/// This is a token-level approximation, the same classification for every type.
/// </summary>
internal static class HalsteadVolume
{
    public static double Measure(IEnumerable<SyntaxToken> tokens)
    {
        var operators = new HashSet<string>(StringComparer.Ordinal);
        var operands = new HashSet<string>(StringComparer.Ordinal);
        long total = 0;
        foreach (var token in tokens)
        {
            if (token.IsKind(SyntaxKind.EndOfFileToken)) continue;
            var isOperand = token.IsKind(SyntaxKind.IdentifierToken) || token.IsKind(SyntaxKind.NumericLiteralToken)
                || token.IsKind(SyntaxKind.StringLiteralToken) || token.IsKind(SyntaxKind.CharacterLiteralToken)
                || token.IsKind(SyntaxKind.InterpolatedStringTextToken);
            (isOperand ? operands : operators).Add(isOperand ? token.ValueText : token.Text);
            total++;
        }
        var vocabulary = operators.Count + operands.Count;
        return vocabulary <= 1 || total == 0 ? 1 : total * Math.Log2(vocabulary);
    }
}

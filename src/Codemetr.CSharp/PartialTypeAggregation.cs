using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>Combines measurable partial parts without changing declaration-level API output.</summary>
internal static class PartialTypeAggregation
{
    public static IReadOnlyList<TypeMetrics> Collect(CSharpCompilation compilation, IReadOnlyList<TypeMetrics> measured,
        IReadOnlyCollection<string> excluded, CancellationToken cancellationToken)
    {
        var result = new List<TypeMetrics>();
        var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var metric in measured)
        {
            var tree = compilation.SyntaxTrees.First(tree => tree.FilePath == metric.File);
            var declaration = tree.GetRoot(cancellationToken).DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
                .FirstOrDefault(node => node.SpanStart == metric.Declarations.FirstOrDefault()?.StartOffset
                    && node.Identifier.ValueText == metric.Name);
            var symbol = declaration is null ? null : compilation.GetSemanticModel(tree).GetDeclaredSymbol(declaration, cancellationToken);
            if (declaration is null || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword) || symbol is null)
            { result.Add(metric); continue; }
            var parts = symbol.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(cancellationToken))
                .OfType<BaseTypeDeclarationSyntax>().ToArray();
            // Invalid duplicate declarations retain their existing diagnostics and independent rows.
            if (parts.Any(part => !part.Modifiers.Any(SyntaxKind.PartialKeyword))) { result.Add(metric); continue; }
            if (!seen.Add(symbol)) continue;
            result.Add(Combine(compilation, metric, parts, excluded, cancellationToken));
        }
        return result.OrderBy(type => type.File, StringComparer.Ordinal).ThenBy(type => type.Line).ToArray();
    }

    static TypeMetrics Combine(CSharpCompilation compilation, TypeMetrics first, IEnumerable<BaseTypeDeclarationSyntax> parts,
        IReadOnlyCollection<string> excluded, CancellationToken cancellationToken)
    {
        var ordered = parts.OrderBy(part => part.SyntaxTree.FilePath, StringComparer.Ordinal).ThenBy(part => part.SpanStart).ToArray();
        var declarations = ordered.Select(part => new MetricDeclaration(part.SyntaxTree.FilePath,
            part.GetLocation().GetLineSpan().StartLinePosition.Line + 1, CanMeasure(part, excluded, cancellationToken), part.SpanStart)).ToArray();
        var eligible = ordered.Where((_, index) => declarations[index].Measured).ToArray();
        if (eligible.Length == 0) return first with { Declarations = declarations };
        var metrics = eligible.Select(part => TypeMeasurer.Measure(part, compilation.GetSemanticModel(part.SyntaxTree), part.SyntaxTree.FilePath)).ToArray();
        var members = metrics.SelectMany(type => type.Members).ToArray();
        var coupled = metrics.SelectMany(type => type.CoupledTypes).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var tokens = eligible.SelectMany(OwnSyntax.Tokens).ToArray();
        var logical = tokens.Where(token => !token.IsKind(SyntaxKind.OpenBraceToken) && !token.IsKind(SyntaxKind.CloseBraceToken))
            .Select(token => (token.SyntaxTree!.FilePath, token.GetLocation().GetLineSpan().StartLinePosition.Line)).Distinct().Count();
        var complexity = members.Sum(member => member.CyclomaticComplexity);
        var mostComplex = members.OrderByDescending(member => member.CyclomaticComplexity).FirstOrDefault();
        return metrics[0] with
        {
            Lines = metrics.Sum(type => type.Lines), LogicalLines = logical,
            MaxCyclomaticComplexity = mostComplex?.CyclomaticComplexity ?? 1,
            MostComplexMember = mostComplex?.Name ?? string.Empty, TotalCyclomaticComplexity = complexity,
            MaintainabilityIndex = MaintainabilityIndex.Calculate(HalsteadVolume.Measure(tokens), complexity, logical),
            ClassCoupling = coupled.Length, CoupledTypes = coupled,
            ConstructorParameterCount = metrics.Max(type => type.ConstructorParameterCount),
            PublicMethodCount = metrics.Sum(type => type.PublicMethodCount) - DuplicatePublicParts(compilation, eligible), InstanceFieldCount = metrics.Sum(type => type.InstanceFieldCount),
            Members = members, Declarations = declarations,
            StrictCoupledTypes = metrics.SelectMany(type => type.StrictCoupledTypes).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
        };
    }

    static int DuplicatePublicParts(CSharpCompilation compilation, IEnumerable<BaseTypeDeclarationSyntax> declarations)
    {
        var methods = declarations.OfType<TypeDeclarationSyntax>().SelectMany(type => type.Members.OfType<MethodDeclarationSyntax>())
            .Select(method => compilation.GetSemanticModel(method.SyntaxTree).GetDeclaredSymbol(method))
            .OfType<IMethodSymbol>().Where(method => method.DeclaredAccessibility == Accessibility.Public)
            .Select(method => method.PartialDefinitionPart ?? method).ToArray();
        return methods.Length - methods.Distinct(SymbolEqualityComparer.Default).Count();
    }

    static bool CanMeasure(BaseTypeDeclarationSyntax part, IReadOnlyCollection<string> excluded, CancellationToken cancellationToken) =>
        !part.SyntaxTree.GetDiagnostics(cancellationToken).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
        && !SourceCollector.IsNeverMeasured(new SourceDocument(part.SyntaxTree.FilePath, part.SyntaxTree.GetText(cancellationToken).ToString()), excluded);
}

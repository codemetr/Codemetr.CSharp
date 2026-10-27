using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>Complements declaration metrics with logical-type and scope-qualified advisory measurements.</summary>
internal static class AnalysisEnrichment
{
    public static AnalysisResult Apply(CSharpCompilation compilation, AnalysisResult result,
        IReadOnlyCollection<string> excluded, string? typeQuery, CancellationToken cancellationToken)
    {
        var whole = PartialTypeAggregation.Collect(compilation, result.Types, excluded, cancellationToken);
        var diagnostics = compilation.GetDiagnostics(cancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        var selected = (result with
        {
            WholeTypes = whole,
            Completeness = new AnalysisCompleteness("selected types plus available measurable partial parts; platform references only",
                diagnostics.Length, diagnostics.Select(diagnostic => diagnostic.Id).Distinct().Order(StringComparer.Ordinal).ToArray(),
                whole.SelectMany(type => type.StrictCoupledTypes).Where(name => name.StartsWith('?')).Distinct().Order(StringComparer.Ordinal).ToArray(),
                whole.SelectMany(type => type.Declarations).Where(site => !site.Measured).Distinct().ToArray(), result.FilesWithInactiveCode)
            { ContextFilesWithLimits = FileLimits(compilation, cancellationToken) },
        }).SelectTypes(typeQuery);
        var sourceTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot(cancellationToken).DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            .Select(node => compilation.GetSemanticModel(node.SyntaxTree).GetDeclaredSymbol(node, cancellationToken)?.OriginalDefinition.ToDisplayString())
            .OfType<string>().ToArray();
        return selected with { Dependencies = DependencyGraph.Build(selected.WholeTypes, sourceTypes),
            Cohesion = CohesionAnalysis.Measure(compilation, selected.WholeTypes) };
    }
    static IReadOnlyList<AnalysisFileLimit> FileLimits(CSharpCompilation compilation, CancellationToken cancellationToken) =>
        compilation.SyntaxTrees.Select(tree => new AnalysisFileLimit(tree.FilePath,
            tree.GetDiagnostics(cancellationToken).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
            tree.GetRoot(cancellationToken).DescendantTrivia().Any(trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia)
                && !string.IsNullOrWhiteSpace(trivia.ToString()))))
            .Where(file => file.HasSyntaxErrors || file.HasInactiveCode).OrderBy(file => file.File, StringComparer.Ordinal).ToArray();

}

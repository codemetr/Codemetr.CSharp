using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>
/// Entry point: parses documents into one compilation against the running .NET platform and measures
/// every type declaration. Documents with syntax errors are reported and not measured.
/// </summary>
public static class CodeAnalyzer
{
    static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    /// <summary>Analyses the documents. Types are ordered by file, then line.</summary>
    /// <param name="documents">Documents to measure.</param>
    /// <param name="context">
    /// Further documents that are compiled but not measured, usually every source below the root. Coupling and
    /// inheritance then resolve the same way whether a run measures one file or the whole tree.
    /// </param>
    /// <param name="typeQuery">When set, only declarations whose simple name can match are measured (see <see cref="AnalysisResult.SelectTypes"/>).</param>
    /// <param name="cancellationToken">Cancels parsing and measuring.</param>
    public static AnalysisResult Analyze(IReadOnlyList<SourceDocument> documents, IReadOnlyList<SourceDocument>? context = null,
        string? typeQuery = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documents);
        var errors = new List<ParseError>();
        var measurable = new List<(SourceDocument Document, SyntaxTree Tree)>();
        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tree = Parse(document, cancellationToken);
            var error = tree.GetDiagnostics(cancellationToken).FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            if (error is null) measurable.Add((document, tree));
            else errors.Add(new ParseError(document.Path, error.Location.GetLineSpan().StartLinePosition.Line + 1, error.Id, error.GetMessage(System.Globalization.CultureInfo.InvariantCulture)));
        }
        var measuredPaths = documents.Select(document => document.Path).ToHashSet(StringComparer.Ordinal);
        var contextTrees = (context ?? []).Where(document => !measuredPaths.Contains(document.Path))
            .Select(document => Parse(document, cancellationToken));

        var usings = CSharpSyntaxTree.ParseText(ImplicitUsings.Source, ParseOptions, "codemetr-implicit-usings.cs", cancellationToken: cancellationToken);
        // Nullable analysis does not change any metric and costs about a fifth of the binding time.
        var compilation = CSharpCompilation.Create("codemetr-analysis",
            measurable.Select(item => item.Tree).Concat(contextTrees).Append(usings),
            PlatformReferences.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Disable));

        var simpleName = SimpleName(typeQuery);
        var types = new ConcurrentBag<TypeMetrics>();
        // Semantic models of one compilation are thread-safe; binding dominates the run time.
        Parallel.ForEach(measurable, new ParallelOptions { CancellationToken = cancellationToken }, item =>
        {
            var model = compilation.GetSemanticModel(item.Tree);
            var root = (CompilationUnitSyntax)item.Tree.GetRoot(cancellationToken);
            var topLevel = root.Members.OfType<GlobalStatementSyntax>().ToArray();
            if (topLevel.Length > 0 && (simpleName is null || simpleName == "Program"))
                types.Add(TypeMeasurer.MeasureTopLevel(topLevel, model, item.Document.Path));
            // Extension blocks belong to their enclosing static class.
            foreach (var declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(d => d is not ExtensionBlockDeclarationSyntax))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (simpleName is null || declaration.Identifier.ValueText == simpleName)
                    types.Add(TypeMeasurer.Measure(declaration, model, item.Document.Path));
            }
        });
        var result = new AnalysisResult(
            types.OrderBy(type => type.File, StringComparer.Ordinal).ThenBy(type => type.Line).ThenBy(type => type.FullName, StringComparer.Ordinal).ToArray(),
            errors, documents.Count);
        return result.SelectTypes(typeQuery);
    }

    static SyntaxTree Parse(SourceDocument document, CancellationToken cancellationToken) =>
        CSharpSyntaxTree.ParseText(document.Text, ParseOptions, document.Path, cancellationToken: cancellationToken);

    // The last dotted segment without type parameters: "Shop.Repo<T>" -> "Repo".
    static string? SimpleName(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        var withoutGenerics = TypeName.WithoutTypeParameters(query);
        return withoutGenerics[(withoutGenerics.LastIndexOf('.') + 1)..];
    }
}

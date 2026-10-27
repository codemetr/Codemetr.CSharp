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
    /// <param name="excluded">Paths excluded from whole-type partial aggregation; retained as visible context only.</param>
    public static AnalysisResult Analyze(IReadOnlyList<SourceDocument> documents, IReadOnlyList<SourceDocument>? context = null,
        string? typeQuery = null, CancellationToken cancellationToken = default, IReadOnlyCollection<string>? excluded = null)
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
            .Select(document => Parse(document, cancellationToken)).ToArray();

        var usings = CSharpSyntaxTree.ParseText(ImplicitUsings.Source, ParseOptions, "codemetr-implicit-usings.cs", cancellationToken: cancellationToken);
        // Nullable analysis does not change any metric and costs about a fifth of the binding time.
        var compilation = CSharpCompilation.Create("codemetr-analysis",
            measurable.Select(item => item.Tree).Concat(contextTrees).Append(usings),
            PlatformReferences.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Disable));

        var simpleName = SimpleName(typeQuery);
        var types = new ConcurrentBag<(int Start, TypeMetrics Metrics)>();
        var duplicates = new ConcurrentBag<string>();
        var inactive = new ConcurrentBag<string>();
        // Semantic models of one compilation are thread-safe; binding dominates the run time.
        Parallel.ForEach(measurable, new ParallelOptions { CancellationToken = cancellationToken }, item =>
        {
            var model = compilation.GetSemanticModel(item.Tree);
            var root = (CompilationUnitSyntax)item.Tree.GetRoot(cancellationToken);
            if (root.DescendantTrivia().Any(trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia) && !string.IsNullOrWhiteSpace(trivia.ToString())))
                inactive.Add(item.Document.Path);
            var topLevel = root.Members.OfType<GlobalStatementSyntax>().ToArray();
            if (topLevel.Length > 0 && (simpleName is null || simpleName == "Program"))
                types.Add((topLevel[0].SpanStart, TypeMeasurer.MeasureTopLevel(topLevel, model, item.Document.Path)));
            // Extension blocks belong to their enclosing static class.
            foreach (var declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(d => d is not ExtensionBlockDeclarationSyntax))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (simpleName is not null && declaration.Identifier.ValueText != simpleName) continue;
                types.Add((declaration.SpanStart, TypeMeasurer.Measure(declaration, model, item.Document.Path)));
                if (DuplicateDeclaration(compilation, model, declaration, cancellationToken) is { } duplicate) duplicates.Add(duplicate);
            }
        });
        foreach (var tree in contextTrees)
            if (tree.GetRoot(cancellationToken).DescendantTrivia().Any(trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia) && !string.IsNullOrWhiteSpace(trivia.ToString())))
                inactive.Add(tree.FilePath);
        var result = new AnalysisResult(
            types.OrderBy(type => type.Metrics.File, StringComparer.Ordinal).ThenBy(type => type.Start).Select(type => type.Metrics).ToArray(),
            errors, documents.Count) { Warnings = duplicates.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Concat(InactiveWarning(inactive)).ToArray(),
            FilesWithInactiveCode = inactive.Order(StringComparer.Ordinal).ToArray() };
        return AnalysisEnrichment.Apply(compilation, result, excluded ?? [], typeQuery, cancellationToken);
    }

    // Roslyn merges a type declared twice (not as partial parts) into one symbol with several declarations,
    // typically when the context spans several projects; members and base class then mix silently.
    static string? DuplicateDeclaration(CSharpCompilation compilation, SemanticModel model, BaseTypeDeclarationSyntax declaration, CancellationToken cancellationToken)
    {
        if (model.GetDeclaredSymbol(declaration, cancellationToken) is not { } symbol) return null;
        var declarations = symbol.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(cancellationToken)).OfType<BaseTypeDeclarationSyntax>().ToArray();
        if (declarations.Length < 2 || declarations.All(d => d.Modifiers.Any(SyntaxKind.PartialKeyword))) return null;
        var others = declarations.Where(d => d != declaration).Select(d => d.SyntaxTree.FilePath).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        return $"{symbol.ToDisplayString()} is declared more than once in the compilation context (also in {string.Join(", ", others)}); " +
            "coupling and inheritance may mix the declarations - narrow 'context' in codemetr.json.";
    }

    const int ListedFiles = 5;

    // No preprocessor symbols are defined, so code in #if branches is invisible to every metric.
    static IEnumerable<string> InactiveWarning(IEnumerable<string> files)
    {
        var sorted = files.Order(StringComparer.Ordinal).ToArray();
        if (sorted.Length == 0) yield break;
        var more = sorted.Length > ListedFiles ? $" and {sorted.Length - ListedFiles} more" : "";
        yield return $"{sorted.Length} file(s) contain inactive #if code that is not measured: {string.Join(", ", sorted.Take(ListedFiles))}{more}.";
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

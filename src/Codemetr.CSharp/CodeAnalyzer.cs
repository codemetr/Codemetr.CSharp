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
    public static AnalysisResult Analyze(IReadOnlyList<SourceDocument> documents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documents);
        var errors = new List<ParseError>();
        var measurable = new List<(SourceDocument Document, SyntaxTree Tree)>();
        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tree = CSharpSyntaxTree.ParseText(document.Text, ParseOptions, document.Path, cancellationToken: cancellationToken);
            var error = tree.GetDiagnostics(cancellationToken).FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            if (error is null) measurable.Add((document, tree));
            else errors.Add(new ParseError(document.Path, error.Location.GetLineSpan().StartLinePosition.Line + 1, error.Id, error.GetMessage(System.Globalization.CultureInfo.InvariantCulture)));
        }

        var usings = CSharpSyntaxTree.ParseText(ImplicitUsings.Source, ParseOptions, "codemetr-implicit-usings.cs", cancellationToken: cancellationToken);
        var compilation = CSharpCompilation.Create("codemetr-analysis",
            measurable.Select(item => item.Tree).Append(usings),
            PlatformReferences.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));

        var types = new List<TypeMetrics>();
        foreach (var (document, tree) in measurable)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var declaration in tree.GetRoot(cancellationToken).DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                types.Add(TypeMeasurer.Measure(declaration, model, document.Path));
            }
        }
        return new AnalysisResult(
            types.OrderBy(type => type.File, StringComparer.Ordinal).ThenBy(type => type.Line).ToArray(),
            errors, documents.Count);
    }
}

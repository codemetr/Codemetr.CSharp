using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>
/// Finds every declaration of a type below a root by syntax alone, including places no run measures
/// (build output folders, generated files, excluded paths). The baseline uses it to tell a fixed
/// violation from one that moved out of sight. Files are read lazily and only when they mention the name.
/// </summary>
public sealed class DeclarationIndex(string root, IReadOnlyCollection<string> exclude)
{
    readonly string root = Path.GetFullPath(root);
    readonly Lazy<string[]> files = new(() => SourceCollector.EveryCSharpFile(Path.GetFullPath(root)));
    readonly ConcurrentDictionary<string, IReadOnlyList<DeclarationSite>> found = new(StringComparer.Ordinal);

    /// <summary>Declarations whose full name (as reported in <see cref="TypeMetrics.FullName"/>) equals <paramref name="fullName"/>.</summary>
    public IReadOnlyList<DeclarationSite> Find(string fullName) => found.GetOrAdd(fullName, Scan);

    IReadOnlyList<DeclarationSite> Scan(string fullName)
    {
        var simple = TypeName.WithoutTypeParameters(fullName);
        simple = simple[(simple.LastIndexOf('.') + 1)..];
        var sites = new List<DeclarationSite>();
        foreach (var file in files.Value)
        {
            if (new FileInfo(file).Length > SourceCollector.MaximumFileBytes) continue;
            var text = File.ReadAllText(file);
            if (!text.Contains(simple, StringComparison.Ordinal)) continue;
            var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            var document = new SourceDocument(relative, text);
            var never = SourceCollector.IsInSkippedDirectory(relative) || SourceCollector.IsNeverMeasured(document, exclude);
            foreach (var declaration in CSharpSyntaxTree.ParseText(text).GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                if (declaration is not ExtensionBlockDeclarationSyntax && SyntaxFullName(declaration) == fullName)
                    sites.Add(new DeclarationSite(relative, never, MemberCollector.Collect(declaration).Select(m => m.Name).ToArray()));
        }
        return sites;
    }

    // Namespaces and containing types outermost first, type parameters as "<T, U>": the reported full-name format.
    static string SyntaxFullName(BaseTypeDeclarationSyntax declaration)
    {
        var parts = new List<string>();
        for (Microsoft.CodeAnalysis.SyntaxNode? node = declaration; node is not null; node = node.Parent)
        {
            switch (node)
            {
                case TypeDeclarationSyntax { TypeParameterList: { } parameters } type and not ExtensionBlockDeclarationSyntax:
                    parts.Add(type.Identifier.ValueText + "<" + string.Join(", ", parameters.Parameters.Select(p => p.Identifier.ValueText)) + ">");
                    break;
                case BaseTypeDeclarationSyntax type and not ExtensionBlockDeclarationSyntax:
                    parts.Add(type.Identifier.ValueText);
                    break;
                case BaseNamespaceDeclarationSyntax ns:
                    parts.Add(ns.Name.ToString());
                    break;
            }
        }
        parts.Reverse();
        return string.Join(".", parts);
    }
}

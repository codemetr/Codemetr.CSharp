using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>
/// Finds every declaration of a type below a root by syntax alone, including places no run measures
/// (build output folders, generated files, excluded paths). The baseline uses it to tell a fixed
/// violation from one that moved out of sight. <see cref="Prepare"/> reads each file once for all names;
/// files are parsed only when they mention one of them.
/// </summary>
public sealed class DeclarationIndex(string root, IReadOnlyCollection<string> exclude)
{
    readonly string root = Path.GetFullPath(root);
    readonly Lazy<string[]> files = new(() => SourceCollector.EveryCSharpFile(Path.GetFullPath(root)));
    readonly Lazy<bool> linked = new(() => SourceCollector.HasLinkedDirectory(Path.GetFullPath(root)));
    readonly ConcurrentDictionary<string, IReadOnlyList<DeclarationSite>> found = new(StringComparer.Ordinal);

    /// <summary>
    /// True when a folder below the root is a symbolic link. Neither measuring nor this index follows such
    /// folders, so a type that disappeared may live there unseen.
    /// </summary>
    public bool HasLinkedDirectory => linked.Value;

    /// <summary>Scans all files once for the given full type names; later <see cref="Find"/> calls are lookups.</summary>
    public void Prepare(IEnumerable<string> fullNames)
    {
        var wanted = fullNames.Where(name => !found.ContainsKey(name)).Distinct(StringComparer.Ordinal).ToArray();
        if (wanted.Length == 0) return;
        var simple = wanted.Select(SimpleName).Distinct(StringComparer.Ordinal).ToArray();
        var sites = wanted.ToDictionary(name => name, _ => new List<DeclarationSite>(), StringComparer.Ordinal);
        foreach (var file in files.Value)
        {
            if (new FileInfo(file).Length > SourceCollector.MaximumFileBytes) continue;
            var text = File.ReadAllText(file);
            if (!simple.Any(name => text.Contains(name, StringComparison.Ordinal))) continue;
            var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            var never = SourceCollector.IsInSkippedDirectory(relative) || SourceCollector.IsNeverMeasured(new SourceDocument(relative, text), exclude);
            foreach (var declaration in CSharpSyntaxTree.ParseText(text).GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                if (declaration is not ExtensionBlockDeclarationSyntax && sites.TryGetValue(SyntaxFullName(declaration), out var list))
                    list.Add(new DeclarationSite(relative, never, MemberCollector.Collect(declaration).Select(m => m.Name).ToArray()));
        }
        foreach (var (name, list) in sites) found.TryAdd(name, list);
    }

    /// <summary>Declarations whose full name (as reported in <see cref="TypeMetrics.FullName"/>) equals <paramref name="fullName"/>.</summary>
    public IReadOnlyList<DeclarationSite> Find(string fullName)
    {
        if (!found.ContainsKey(fullName)) Prepare([fullName]);
        return found[fullName];
    }

    /// <summary>
    /// The member's name without parameters ("M(int).Local(string)" -> "M"); a copy in hidden code may spell
    /// its parameter types differently, so hidden members are matched by this name.
    /// </summary>
    public static string SimpleMemberName(string member) => member.Split('(')[0];

    static string SimpleName(string fullName)
    {
        var withoutGenerics = TypeName.WithoutTypeParameters(fullName);
        return withoutGenerics[(withoutGenerics.LastIndexOf('.') + 1)..];
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

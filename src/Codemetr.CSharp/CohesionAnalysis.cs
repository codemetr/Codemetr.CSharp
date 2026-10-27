using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Codemetr.CSharp;

/// <summary>State-based cohesion over available measurable parts, using exact symbols and this-receivers.</summary>
internal static class CohesionAnalysis
{
    public static IReadOnlyList<CohesionMetrics> Measure(CSharpCompilation compilation, IReadOnlyList<TypeMetrics> types)
    {
        var result = new List<CohesionMetrics>();
        foreach (var type in types)
        {
            var locations = type.Declarations.Where(site => site.Measured).ToArray();
            var declarations = compilation.SyntaxTrees.Where(tree => locations.Any(site => site.File == tree.FilePath))
                .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                .Where(node => locations.Any(site => site.File == node.SyntaxTree.FilePath && site.StartOffset == node.SpanStart))
                .ToArray();
            result.Add(MeasureType(compilation, type, declarations));
        }
        return result;
    }

    static CohesionMetrics MeasureType(CSharpCompilation compilation, TypeMetrics type, TypeDeclarationSyntax[] declarations)
    {
        var methods = declarations.SelectMany(declaration => declaration.Members.OfType<MethodDeclarationSyntax>())
            .Where(method => method.Body is not null || method.ExpressionBody is not null)
            .Select(method => compilation.GetSemanticModel(method.SyntaxTree).GetDeclaredSymbol(method))
            .OfType<IMethodSymbol>().Where(method => !method.IsStatic).Select(MethodIdentity).ToArray();
        var fields = declarations.SelectMany(declaration => declaration.Members.OfType<FieldDeclarationSyntax>())
            .SelectMany(field => field.Declaration.Variables).Select(variable => compilation.GetSemanticModel(variable.SyntaxTree).GetDeclaredSymbol(variable))
            .OfType<IFieldSymbol>().Where(field => !field.IsStatic && !field.IsConst).ToArray();
        if (fields.Length == 0 || methods.Length < 2)
            return new(type.FullName, false, "requires explicit instance fields and at least two implemented instance methods", methods.Length, fields.Length, []);
        if (methods.GroupBy(method => method.ToDisplayString(), StringComparer.Ordinal).Any(group => group.Count() > 1))
            return new(type.FullName, false, "ambiguous duplicate method identities; semantic errors remain advisory", methods.Length, fields.Length, []);
        var references = methods.ToDictionary(method => (ISymbol)method, method => References(compilation, method), SymbolEqualityComparer.Default);
        var edges = methods.ToDictionary(method => method.ToDisplayString(), method => methods.Where(other => !SymbolEqualityComparer.Default.Equals(method, other)
            && Connected(method, other, fields, references)).Select(other => other.ToDisplayString()).Order(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        var groups = ConnectedGroups(edges);
        return new(type.FullName, true, "explicit own fields and direct this-calls; accessors, inherited state and static members excluded", methods.Length, fields.Length, groups);
    }

    static HashSet<ISymbol> References(CSharpCompilation compilation, IMethodSymbol method)
    {
        var references = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var syntax in (method.PartialImplementationPart ?? method).DeclaringSyntaxReferences)
        {
            var root = syntax.GetSyntax();
            var model = compilation.GetSemanticModel(root.SyntaxTree);
            // Local functions and lambdas belong to the enclosing instance method for this metric.
            foreach (var name in root.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (name.Parent is MemberAccessExpressionSyntax access && access.Name == name && access.Expression is not ThisExpressionSyntax) continue;
                if (name.Parent is MemberBindingExpressionSyntax) continue;
                if (IsNameof(name, model)) continue;
                var symbol = model.GetSymbolInfo(name).Symbol;
                if (symbol is IFieldSymbol field && IsThisField(name, model)) references.Add(field.OriginalDefinition);
                if (symbol is IMethodSymbol called && IsThisCall(name, model)) references.Add(MethodIdentity(called.OriginalDefinition));
            }
        }
        return references;
    }

    static IMethodSymbol MethodIdentity(IMethodSymbol method) => method.PartialDefinitionPart ?? method;

    static bool IsThisField(SimpleNameSyntax name, SemanticModel model) =>
        model.GetOperation(name.Parent is MemberAccessExpressionSyntax access ? access : name) is
            IFieldReferenceOperation { Instance: IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance } };

    static bool IsThisCall(SimpleNameSyntax name, SemanticModel model)
    {
        var invocation = name.Parent is MemberAccessExpressionSyntax access ? access.Parent : name.Parent;
        return invocation is InvocationExpressionSyntax && model.GetOperation(invocation) is
            IInvocationOperation { Instance: IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance } };
    }

    static bool IsNameof(SimpleNameSyntax name, SemanticModel model) =>
        name.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault() is
            { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } expression }
        && model.GetSymbolInfo(expression).Symbol is null;

    static bool Connected(IMethodSymbol left, IMethodSymbol right, IFieldSymbol[] fields,
        Dictionary<ISymbol, HashSet<ISymbol>> references) => references[left].Contains(right) || references[right].Contains(left)
        || fields.Any(field => references[left].Contains(field) && references[right].Contains(field));

    static IReadOnlyList<IReadOnlyList<string>> ConnectedGroups(Dictionary<string, string[]> edges)
    {
        var remaining = edges.Keys.ToHashSet(StringComparer.Ordinal);
        var groups = new List<IReadOnlyList<string>>();
        foreach (var start in edges.Keys.Order(StringComparer.Ordinal))
        {
            if (!remaining.Contains(start)) continue;
            var group = new List<string>();
            var pending = new Stack<string>(); pending.Push(start);
            while (pending.TryPop(out var current))
                if (remaining.Remove(current)) { group.Add(current); foreach (var next in edges[current]) pending.Push(next); }
            groups.Add(group.Order(StringComparer.Ordinal).ToArray());
        }
        return groups;
    }
}

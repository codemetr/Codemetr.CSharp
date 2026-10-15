using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>
/// Distinct types a type's own code references: declared types, created types, attributes and the
/// containing types of called or accessed members (including static and extension calls).
/// Built-in types (int, string, object, ...), tuples, type parameters and the type itself do not count.
/// Names the semantic model cannot bind are kept with a leading '?' when they stand in a type position
/// or as the receiver of a member access starting with an upper-case letter.
/// </summary>
internal static class ClassCoupling
{
    static readonly SymbolDisplayFormat NameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public static IReadOnlyList<string> Collect(BaseTypeDeclarationSyntax declaration, SemanticModel model)
    {
        var self = model.GetDeclaredSymbol(declaration);
        var references = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var name in OwnSyntax.Nodes(declaration).OfType<SimpleNameSyntax>())
        {
            var info = model.GetSymbolInfo(name);
            var symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
            // 'var' and similar contextual keywords name an inferred type; when inference fails they are no reference.
            if (symbol is null && name is IdentifierNameSyntax { IsVar: true } or IdentifierNameSyntax { IsNint: true }
                    or IdentifierNameSyntax { IsNuint: true } or IdentifierNameSyntax { IsUnmanaged: true } or IdentifierNameSyntax { IsNotNull: true })
                continue;
            var type = symbol switch
            {
                ITypeSymbol typeSymbol => typeSymbol,
                IMethodSymbol { ReducedFrom: { } extension } => extension.ContainingType,
                IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol => symbol.IsStatic
                    ? symbol.ContainingType
                    : ReceiverType(name, model) ?? symbol.ContainingType,
                _ => null,
            };
            if (type is not null) Add(type, self, references);
            else if (symbol is null && LooksLikeTypeReference(name)) references.Add("?" + name.Identifier.ValueText);
        }
        return references.ToArray();
    }

    static void Add(ITypeSymbol type, INamedTypeSymbol? self, ISet<string> references)
    {
        switch (type)
        {
            case IArrayTypeSymbol array: Add(array.ElementType, self, references); return;
            case IPointerTypeSymbol pointer: Add(pointer.PointedAtType, self, references); return;
            case ITypeParameterSymbol or IDynamicTypeSymbol: return;
            case INamedTypeSymbol named:
                if (named.IsTupleType || named.IsAnonymousType || named.SpecialType != SpecialType.None) return;
                var definition = named.OriginalDefinition;
                if (SymbolEqualityComparer.Default.Equals(definition, self)) return;
                if (named.TypeKind == TypeKind.Error)
                {
                    if (named.Name.Length > 0 && named.Name != "var") references.Add("?" + named.Name);
                    return;
                }
                references.Add(definition.ToDisplayString(NameFormat));
                return;
        }
    }

    // An instance member couples to the type it is reached through, not to the base class that happens to declare it.
    static ITypeSymbol? ReceiverType(SimpleNameSyntax name, SemanticModel model) =>
        name.Parent is MemberAccessExpressionSyntax access && access.Name == name ? model.GetTypeInfo(access.Expression).Type : null;

    static bool LooksLikeTypeReference(SimpleNameSyntax name)
    {
        if (name.Parent is AttributeSyntax || SyntaxFacts.IsInTypeOnlyContext(name)) return true;
        return name.Parent is MemberAccessExpressionSyntax access && access.Expression == name
            && char.IsUpper(name.Identifier.ValueText[0]);
    }
}

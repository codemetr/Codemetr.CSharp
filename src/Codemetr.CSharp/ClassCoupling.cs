using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>
/// Distinct types a type's own code references: declared types, created types, attributes and the
/// containing types of called or accessed members (static and extension calls included); an instance
/// member couples to the type it is reached through. Keyword types (int, string, object, void, ...), Nullable,
/// tuples, type parameters, <c>nameof</c> operands and the type itself do not count.
/// Names the semantic model cannot bind are kept with a leading '?' when they stand in a type position
/// or as an upper-case receiver of a member access; the latter is skipped when the type's own base class
/// is unresolved, because then inherited members would look like types.
/// </summary>
internal static class ClassCoupling
{
    static readonly SymbolDisplayFormat NameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public static IReadOnlyList<string> Collect(IEnumerable<SyntaxNode> roots, INamedTypeSymbol? self, SemanticModel model, bool strictBinding = false)
    {
        var references = new SortedSet<string>(StringComparer.Ordinal);
        var inheritsUnresolved = self?.BaseType is { TypeKind: TypeKind.Error } unresolved && !TypeName.LooksLikeInterface(unresolved.Name);
        foreach (var name in roots.SelectMany(OwnSyntax.Nodes).OfType<SimpleNameSyntax>())
        {
            if (IsNameofOperand(name, model)) continue;
            var info = model.GetSymbolInfo(name);
            var symbol = info.Symbol ?? (strictBinding ? null : info.CandidateSymbols.FirstOrDefault());
            // 'var' and similar contextual keywords name an inferred type; when inference fails they are no reference.
            if (symbol is null && name is IdentifierNameSyntax { IsVar: true } or IdentifierNameSyntax { IsNint: true }
                    or IdentifierNameSyntax { IsNuint: true } or IdentifierNameSyntax { IsUnmanaged: true } or IdentifierNameSyntax { IsNotNull: true })
                continue;
            var type = symbol is null ? null : ReferencedType(symbol, name, model);
            if (type is not null) Add(type, self, references);
            else if (symbol is null && LooksLikeTypeReference(name, inheritsUnresolved)) references.Add("?" + name.Identifier.ValueText);
        }
        return references.ToArray();
    }

    // Only types with a C# keyword are free; IEnumerable<T>, IDisposable, DateTime and the like are real dependencies.
    static readonly HashSet<SpecialType> KeywordTypes =
    [
        SpecialType.System_Object, SpecialType.System_Boolean, SpecialType.System_Char, SpecialType.System_SByte,
        SpecialType.System_Byte, SpecialType.System_Int16, SpecialType.System_UInt16, SpecialType.System_Int32,
        SpecialType.System_UInt32, SpecialType.System_Int64, SpecialType.System_UInt64, SpecialType.System_Decimal,
        SpecialType.System_Single, SpecialType.System_Double, SpecialType.System_String, SpecialType.System_IntPtr,
        SpecialType.System_UIntPtr, SpecialType.System_Void,
    ];

    // The type a referenced symbol couples to: the type itself, the owner of a static or extension member,
    // or the type an instance member is reached through.
    static ITypeSymbol? ReferencedType(ISymbol symbol, SimpleNameSyntax name, SemanticModel model) => symbol switch
    {
        ITypeSymbol typeSymbol => typeSymbol,
        IMethodSymbol { ReducedFrom: { } extension } => extension.ContainingType,
        IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol => symbol.IsStatic || symbol.ContainingType?.IsExtension == true
            ? symbol.ContainingType
            : ReceiverType(name, model) ?? symbol.ContainingType,
        _ => null,
    };

    static void Add(ITypeSymbol type, INamedTypeSymbol? self, ISet<string> references)
    {
        switch (type)
        {
            case IArrayTypeSymbol array: Add(array.ElementType, self, references); return;
            case IPointerTypeSymbol pointer: Add(pointer.PointedAtType, self, references); return;
            case ITypeParameterSymbol or IDynamicTypeSymbol: return;
            case INamedTypeSymbol { IsExtension: true, ContainingType: { } extensionOwner }: Add(extensionOwner, self, references); return;
            case INamedTypeSymbol named:
                var definition = named.OriginalDefinition;
                if (named.IsTupleType || named.IsAnonymousType || KeywordTypes.Contains(named.SpecialType)
                    || definition.SpecialType == SpecialType.System_Nullable_T) return;
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
    static ITypeSymbol? ReceiverType(SimpleNameSyntax name, SemanticModel model)
    {
        if (name.Parent is MemberAccessExpressionSyntax access && access.Name == name) return model.GetTypeInfo(access.Expression).Type;
        if (name.Parent is not MemberBindingExpressionSyntax binding) return null;
        // a?.B: the receiver is the expression of the conditional access whose WhenNotNull starts with this binding.
        var conditional = binding.Ancestors().OfType<ConditionalAccessExpressionSyntax>()
            .FirstOrDefault(candidate => candidate.WhenNotNull.Span.Contains(binding.Span));
        var receiver = conditional is null ? null : model.GetTypeInfo(conditional.Expression).Type;
        return receiver is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            ? nullable.TypeArguments[0] : receiver;
    }

    static bool IsNameofOperand(SimpleNameSyntax name, SemanticModel model) =>
        name.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault() is
            { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } nameof }
        && model.GetSymbolInfo(nameof).Symbol is null;

    static bool LooksLikeTypeReference(SimpleNameSyntax name, bool inheritsUnresolved)
    {
        if (name.Parent is AttributeSyntax || SyntaxFacts.IsInTypeOnlyContext(name)) return true;
        return !inheritsUnresolved && name.Parent is MemberAccessExpressionSyntax access && access.Expression == name
            && char.IsUpper(name.Identifier.ValueText[0]);
    }
}

using Microsoft.CodeAnalysis;

namespace Codemetr.CSharp;

/// <summary>
/// Number of base classes above a class, excluding System.Object. An unresolvable base counts once, unless
/// its name follows the interface convention (I + upper-case letter): Roslyn reports an unresolved first
/// base-list entry as base class even when it is an interface.
/// </summary>
internal static class InheritanceDepth
{
    public static int Measure(INamedTypeSymbol? type)
    {
        if (type is null || type.TypeKind != TypeKind.Class) return 0;
        var depth = 0;
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        for (var current = type.BaseType; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            if (!visited.Add(current)) break;
            if (current.TypeKind == TypeKind.Error)
            {
                if (!TypeName.LooksLikeInterface(current.Name)) depth++;
                break;
            }
            depth++;
        }
        return depth;
    }
}

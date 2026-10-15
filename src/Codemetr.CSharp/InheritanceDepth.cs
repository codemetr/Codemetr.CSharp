using Microsoft.CodeAnalysis;

namespace Codemetr.CSharp;

/// <summary>Number of base classes above a class, excluding System.Object. An unresolvable base counts once.</summary>
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
            depth++;
            if (current.TypeKind == TypeKind.Error) break;
        }
        return depth;
    }
}

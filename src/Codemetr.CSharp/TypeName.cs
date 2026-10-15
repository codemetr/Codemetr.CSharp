namespace Codemetr.CSharp;

/// <summary>Matches a user-written type name; type parameters may be omitted (<c>Shop.Repo</c> matches <c>Shop.Repo&lt;T&gt;</c>).</summary>
internal static class TypeName
{
    public static bool Matches(TypeMetrics type, string query, bool allowSuffix)
    {
        foreach (var candidate in new[] { type.FullName, WithoutTypeParameters(type.FullName) })
            if (candidate == query || (allowSuffix && candidate.EndsWith("." + query, StringComparison.Ordinal)))
                return true;
        return type.Name == query;
    }

    /// <summary>
    /// Convention I + upper-case letter. Used only for names the semantic model cannot bind, where Roslyn
    /// reports the first unresolved base-list entry as base class although it may be an interface.
    /// </summary>
    public static bool LooksLikeInterface(string name) => name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]);

    // Removes every <...> group, including those of containing types: Outer<T>.Inner<U> -> Outer.Inner.
    public static string WithoutTypeParameters(string name)
    {
        var result = new System.Text.StringBuilder(name.Length);
        var depth = 0;
        foreach (var character in name)
        {
            if (character == '<') depth++;
            else if (character == '>') depth--;
            else if (depth == 0) result.Append(character);
        }
        return result.ToString();
    }
}

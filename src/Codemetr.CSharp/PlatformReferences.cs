using Microsoft.CodeAnalysis;

namespace Codemetr.CSharp;

/// <summary>Metadata references of the running .NET platform so that BCL types bind semantically.</summary>
internal static class PlatformReferences
{
    static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(Load);

    public static IReadOnlyList<MetadataReference> All => References.Value;

    static IReadOnlyList<MetadataReference> Load()
    {
        var trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        return trusted.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(path => Path.GetFileName(path).StartsWith("System.", StringComparison.Ordinal)
                || Path.GetFileName(path) is "mscorlib.dll" or "netstandard.dll" or "Microsoft.CSharp.dll" or "Microsoft.Win32.Primitives.dll")
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray();
    }
}

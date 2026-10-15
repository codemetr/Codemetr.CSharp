using System.Text.Json;
using System.Text.Json.Serialization;

namespace Codemetr.CSharp;

/// <summary>JSON settings shared by configuration, baseline and reports: camelCase, strict on unknown members.</summary>
public static class CodemetrJson
{
    /// <summary>Serializer options; unknown members in input files are rejected rather than ignored.</summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}

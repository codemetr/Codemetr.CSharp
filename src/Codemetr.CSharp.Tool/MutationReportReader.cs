using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Codemetr.CSharp.Tool;

/// <summary>Reads mutation-testing-elements schema versions 1 and 2; no mutation runner or source rewriting.</summary>
internal static class MutationReportReader
{
    const int MaximumBytes = 32 * 1024 * 1024;
    static readonly string[] Detected = ["Killed", "Timeout"];
    static readonly string[] Undetected = ["Survived", "NoCoverage"];
    static readonly string[] Excluded = ["CompileError", "RuntimeError", "Ignored", "Pending"];

    public static MutationReport Read(string root, string path)
    {
        var fullPath = Path.GetFullPath(path, root);
        if (new FileInfo(fullPath).Length > MaximumBytes) throw new InvalidDataException("Mutation report exceeds maximum size");
        var bytes = File.ReadAllBytes(fullPath);
        using var document = JsonDocument.Parse(bytes);
        var report = document.RootElement;
        RejectDuplicateProperties(report);
        var version = report.GetProperty("schemaVersion").GetString();
        if (version is null || !Regex.IsMatch(version, @"^[1-2](\.(0|[1-9]\d*)){0,2}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))) throw new InvalidDataException("Unsupported mutation schemaVersion; expected version 1 or 2");
        var counts = Detected.Concat(Undetected).Concat(Excluded).ToDictionary(status => status, _ => 0, StringComparer.Ordinal);
        var files = report.GetProperty("files");
        if (files.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Mutation files must be an object");
        foreach (var file in files.EnumerateObject()) Count(file.Value, counts);
        var detected = Detected.Sum(status => counts[status]);
        var undetected = Undetected.Sum(status => counts[status]);
        return new(path, Convert.ToHexStringLower(SHA256.HashData(bytes)), version, detected, undetected,
            Excluded.Sum(status => counts[status]), detected + undetected == 0 || counts["Pending"] > 0 ? null : 100.0 * detected / (detected + undetected),
            counts, "imported report only; current source revision and execution not verified");
    }

    static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException($"Duplicate mutation report property '{property.Name}'");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) RejectDuplicateProperties(item);
    }

    static void Count(JsonElement file, Dictionary<string, int> counts)
    {
        var mutants = file.GetProperty("mutants");
        if (mutants.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Mutation mutants must be an array");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var mutant in mutants.EnumerateArray())
        {
            var id = mutant.GetProperty("id").GetString();
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new InvalidDataException("Mutation ids must be non-empty and unique per file");
            var status = mutant.GetProperty("status").GetString();
            if (status is null || !counts.ContainsKey(status)) throw new InvalidDataException($"Unsupported mutation status '{status}'");
            counts[status]++;
        }
    }
}

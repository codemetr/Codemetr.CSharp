using Codemetr.CSharp;

namespace Codemetr.CSharp.Tool;

/// <summary>Builds baseline comparison scope from actual measured files and selected logical types.</summary>
internal static class BaselineScopeFactory
{
    public static AnalysisScope Create(string root, CodemetrRequest request, FileSelection selection, AnalysisResult all, CodemetrConfig config)
    {
        var unparsed = all.ParseErrors.Select(e => e.File).ToHashSet(StringComparer.Ordinal);
        var directories = selection.Paths.Select(path => Path.GetFullPath(path, root)).Where(Directory.Exists).Select(path => CodemetrService.Relative(root, path));
        return new AnalysisScope(selection.Documents.Select(d => d.Path).Concat((config.AggregatePartialTypes ? all.WholeTypes.SelectMany(type => type.Declarations).Where(site => site.Measured).Select(site => site.File) : [])).Concat(selection.MissingListed),
            request.Type is null && !config.AggregatePartialTypes ? null : all.Types.Select(t => t.FullName), directories, unparsed)
        {
            Measured = selection.Documents.Select(d => d.Path).Concat((config.AggregatePartialTypes ? all.WholeTypes.SelectMany(type => type.Declarations).Where(site => site.Measured).Select(site => site.File) : [])).Where(path => !unparsed.Contains(path)).ToHashSet(StringComparer.Ordinal),
            Uncertain = all.FilesWithInactiveCode.ToHashSet(StringComparer.Ordinal),
            Declarations = new DeclarationIndex(root, config.Exclude),
        };
    }

}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>Measures the executable members declared directly in a type, plus their local functions.</summary>
internal static class MemberCollector
{
    public static IReadOnlyList<MemberMetrics> Collect(BaseTypeDeclarationSyntax type)
    {
        var members = new List<MemberMetrics>();
        if (type is TypeDeclarationSyntax withMembers) Collect(members, withMembers.Members, prefix: "");
        return members;
    }

    /// <summary>Measures top-level statements as one member; their local functions are members of their own.</summary>
    public static IReadOnlyList<MemberMetrics> CollectTopLevel(IReadOnlyList<GlobalStatementSyntax> statements)
    {
        var members = new List<MemberMetrics>();
        var body = statements.Where(statement => statement.Statement is not LocalFunctionStatementSyntax).ToArray();
        if (body.Length > 0)
        {
            var start = body[0].GetLocation().GetLineSpan().StartLinePosition.Line;
            var end = body[^1].GetLocation().GetLineSpan().EndLinePosition.Line;
            members.Add(new MemberMetrics(TopLevelName, "topLevel", start + 1, end - start + 1,
                1 + body.Sum(statement => CyclomaticComplexity.Measure(statement) - 1), 0, body.Max(NestingDepth.Measure)));
        }
        foreach (var local in statements.Select(statement => statement.Statement).OfType<LocalFunctionStatementSyntax>())
            Add(members, local, local.Identifier.ValueText + Signature(local.ParameterList), "localFunction", local.ParameterList.Parameters.Count);
        return members;
    }

    public const string TopLevelName = "<top-level statements>";

    static void Collect(List<MemberMetrics> members, SyntaxList<MemberDeclarationSyntax> declarations, string prefix)
    {
        foreach (var member in declarations)
        {
            switch (member)
            {
                case BaseMethodDeclarationSyntax method when method.Body is not null || method.ExpressionBody is not null:
                    Add(members, method, prefix + Name(method) + Signature(method.ParameterList), Kind(method), method.ParameterList.Parameters.Count);
                    break;
                case BasePropertyDeclarationSyntax property:
                    AddAccessors(members, property, prefix);
                    break;
                case ExtensionBlockDeclarationSyntax block:
                    Collect(members, block.Members, prefix + "extension" + (block.ParameterList is { } receiver ? Signature(receiver) : "()") + ".");
                    break;
            }
        }
    }

    static void AddAccessors(List<MemberMetrics> members, BasePropertyDeclarationSyntax property, string prefix)
    {
        var name = prefix + property switch
        {
            PropertyDeclarationSyntax p => p.Identifier.ValueText,
            EventDeclarationSyntax e => e.Identifier.ValueText,
            IndexerDeclarationSyntax i => "this" + Signature(i.ParameterList),
            _ => "this[]",
        };
        var parameters = property is IndexerDeclarationSyntax indexer ? indexer.ParameterList.Parameters.Count : 0;
        var expressionBody = property switch
        {
            PropertyDeclarationSyntax p => p.ExpressionBody,
            IndexerDeclarationSyntax i => i.ExpressionBody,
            _ => null,
        };
        if (expressionBody is not null)
        {
            Add(members, property, name + ".get", "accessor", parameters);
            return;
        }
        foreach (var accessor in property.AccessorList?.Accessors ?? default)
            if (accessor.Body is not null || accessor.ExpressionBody is not null)
                Add(members, accessor, name + "." + accessor.Keyword.ValueText, "accessor", parameters);
    }

    static void Add(List<MemberMetrics> members, SyntaxNode node, string name, string kind, int parameters)
    {
        members.Add(Measure(node, name, kind, parameters));
        foreach (var local in node.DescendantNodes(child => child == node || child is not BaseTypeDeclarationSyntax)
                     .OfType<LocalFunctionStatementSyntax>())
            members.Add(Measure(local, name + "." + local.Identifier.ValueText + Signature(local.ParameterList), "localFunction", local.ParameterList.Parameters.Count));
    }

    // Parameter types make overloads distinct, so a member keeps its own identity in reports and baselines.
    static string Signature(BaseParameterListSyntax parameters)
    {
        var types = string.Join(", ", parameters.Parameters.Select(parameter =>
            string.Join(" ", parameter.Modifiers.Select(modifier => modifier.ValueText)
                .Append(parameter.Type?.NormalizeWhitespace().ToString() ?? parameter.Identifier.ValueText))));
        return parameters is BracketedParameterListSyntax ? "[" + types + "]" : "(" + types + ")";
    }

    static MemberMetrics Measure(SyntaxNode node, string name, string kind, int parameters)
    {
        var span = node.GetLocation().GetLineSpan();
        return new MemberMetrics(name, kind, span.StartLinePosition.Line + 1,
            span.EndLinePosition.Line - span.StartLinePosition.Line + 1,
            CyclomaticComplexity.Measure(node), parameters, NestingDepth.Measure(node));
    }

    static string Name(BaseMethodDeclarationSyntax method) => method switch
    {
        MethodDeclarationSyntax m => m.Identifier.ValueText,
        ConstructorDeclarationSyntax c => c.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword) ? ".cctor" : ".ctor",
        DestructorDeclarationSyntax d => "~" + d.Identifier.ValueText,
        OperatorDeclarationSyntax o => "operator " + o.OperatorToken.ValueText,
        ConversionOperatorDeclarationSyntax c => "operator " + c.Type,
        _ => method.Kind().ToString(),
    };

    static string Kind(BaseMethodDeclarationSyntax method) => method switch
    {
        MethodDeclarationSyntax => "method",
        ConstructorDeclarationSyntax => "constructor",
        DestructorDeclarationSyntax => "destructor",
        OperatorDeclarationSyntax => "operator",
        _ => "conversion",
    };
}

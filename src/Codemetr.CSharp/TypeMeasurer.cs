using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codemetr.CSharp;

/// <summary>Computes <see cref="TypeMetrics"/> for one type declaration.</summary>
internal static class TypeMeasurer
{
    static readonly SymbolDisplayFormat FullNameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public static TypeMetrics Measure(BaseTypeDeclarationSyntax declaration, SemanticModel model, string file)
    {
        var symbol = model.GetDeclaredSymbol(declaration);
        var members = MemberCollector.Collect(declaration);
        var mostComplex = members.OrderByDescending(member => member.CyclomaticComplexity)
            .ThenBy(member => member.Line).FirstOrDefault();
        var totalComplexity = members.Sum(member => member.CyclomaticComplexity);
        var tokens = OwnSyntax.Tokens(declaration).ToArray();
        var logicalLines = tokens
            .Where(token => !token.IsKind(SyntaxKind.OpenBraceToken) && !token.IsKind(SyntaxKind.CloseBraceToken))
            .Select(token => token.GetLocation().GetLineSpan().StartLinePosition.Line)
            .Distinct().Count();
        var coupled = ClassCoupling.Collect(declaration, model);
        var span = declaration.GetLocation().GetLineSpan();
        var name = declaration.Identifier.ValueText;
        return new TypeMetrics(
            name,
            symbol?.ToDisplayString(FullNameFormat) ?? name,
            KindName(declaration),
            file,
            span.StartLinePosition.Line + 1,
            span.EndLinePosition.Line - span.StartLinePosition.Line + 1,
            logicalLines,
            mostComplex?.CyclomaticComplexity ?? 1,
            mostComplex?.Name ?? string.Empty,
            totalComplexity,
            MaintainabilityIndex.Calculate(HalsteadVolume.Measure(tokens), totalComplexity, logicalLines),
            coupled.Count,
            coupled,
            InheritanceDepth.Measure(symbol),
            ConstructorArity(declaration),
            OwnMembers(declaration).OfType<MethodDeclarationSyntax>().Count(method => method.Modifiers.Any(SyntaxKind.PublicKeyword)),
            OwnMembers(declaration).OfType<FieldDeclarationSyntax>()
                .Where(field => !field.Modifiers.Any(SyntaxKind.StaticKeyword) && !field.Modifiers.Any(SyntaxKind.ConstKeyword))
                .Sum(field => field.Declaration.Variables.Count),
            members);
    }

    static IEnumerable<MemberDeclarationSyntax> OwnMembers(BaseTypeDeclarationSyntax declaration) =>
        declaration is TypeDeclarationSyntax type ? type.Members : [];

    // Records and structs are data carriers: a wide positional record is out of scope for this ceiling by design.
    static int ConstructorArity(BaseTypeDeclarationSyntax declaration)
    {
        if (declaration is not ClassDeclarationSyntax type) return 0;
        var primary = type.ParameterList?.Parameters.Count ?? 0;
        var explicitMax = type.Members.OfType<ConstructorDeclarationSyntax>()
            .Where(constructor => !constructor.Modifiers.Any(SyntaxKind.StaticKeyword))
            .Select(constructor => constructor.ParameterList.Parameters.Count)
            .DefaultIfEmpty(0).Max();
        return Math.Max(primary, explicitMax);
    }

    static string KindName(BaseTypeDeclarationSyntax declaration) => declaration switch
    {
        RecordDeclarationSyntax record when record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => "record struct",
        RecordDeclarationSyntax => "record",
        ClassDeclarationSyntax => "class",
        StructDeclarationSyntax => "struct",
        InterfaceDeclarationSyntax => "interface",
        EnumDeclarationSyntax => "enum",
        _ => declaration.Kind().ToString(),
    };
}

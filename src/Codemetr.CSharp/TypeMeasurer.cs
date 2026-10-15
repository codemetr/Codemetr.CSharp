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

    /// <summary>The full name reported for a type symbol.</summary>
    public static string FullName(INamedTypeSymbol symbol) => symbol.ToDisplayString(FullNameFormat);

    public static TypeMetrics Measure(BaseTypeDeclarationSyntax declaration, SemanticModel model, string file)
    {
        var symbol = model.GetDeclaredSymbol(declaration);
        var members = MemberCollector.Collect(declaration);
        var mostComplex = members.OrderByDescending(member => member.CyclomaticComplexity)
            .ThenBy(member => member.Line).FirstOrDefault();
        var totalComplexity = members.Sum(member => member.CyclomaticComplexity);
        var tokens = OwnSyntax.Tokens(declaration).ToArray();
        var logicalLines = LogicalLines(tokens);
        var coupled = ClassCoupling.Collect([declaration], symbol, model);
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
            OwnMembers(declaration).OfType<MethodDeclarationSyntax>().Count(method => IsPublic(method, declaration)),
            OwnMembers(declaration).OfType<FieldDeclarationSyntax>()
                .Where(field => !field.Modifiers.Any(SyntaxKind.StaticKeyword) && !field.Modifiers.Any(SyntaxKind.ConstKeyword))
                .Sum(field => field.Declaration.Variables.Count),
            members);
    }

    // Interface members are public unless they say otherwise.
    static bool IsPublic(MethodDeclarationSyntax method, BaseTypeDeclarationSyntax declaration) =>
        method.Modifiers.Any(SyntaxKind.PublicKeyword) || (declaration is InterfaceDeclarationSyntax
            && !method.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword) || m.IsKind(SyntaxKind.InternalKeyword)));

    static IEnumerable<MemberDeclarationSyntax> OwnMembers(BaseTypeDeclarationSyntax declaration) =>
        declaration is TypeDeclarationSyntax type
            ? type.Members.SelectMany(member => member is ExtensionBlockDeclarationSyntax block ? block.Members : [member])
            : [];

    /// <summary>Top-level statements of one file, reported as the compiler-generated <c>Program</c>.</summary>
    public static TypeMetrics MeasureTopLevel(IReadOnlyList<GlobalStatementSyntax> statements, SemanticModel model, string file)
    {
        var members = MemberCollector.CollectTopLevel(statements);
        var tokens = statements.SelectMany(OwnSyntax.Tokens).ToArray();
        var logicalLines = LogicalLines(tokens);
        var mostComplex = members.OrderByDescending(member => member.CyclomaticComplexity).ThenBy(member => member.Line).First();
        var totalComplexity = members.Sum(member => member.CyclomaticComplexity);
        var coupled = ClassCoupling.Collect(statements, null, model);
        var start = statements[0].GetLocation().GetLineSpan().StartLinePosition.Line;
        var end = statements[^1].GetLocation().GetLineSpan().EndLinePosition.Line;
        return new TypeMetrics("Program", "Program", "top-level", file, start + 1, end - start + 1, logicalLines,
            mostComplex.CyclomaticComplexity, mostComplex.Name, totalComplexity,
            MaintainabilityIndex.Calculate(HalsteadVolume.Measure(tokens), totalComplexity, logicalLines),
            coupled.Count, coupled, 0, 0, 0, 0, members);
    }

    static int LogicalLines(IEnumerable<SyntaxToken> tokens) => tokens
        .Where(token => !token.IsKind(SyntaxKind.OpenBraceToken) && !token.IsKind(SyntaxKind.CloseBraceToken))
        .Select(token => token.GetLocation().GetLineSpan().StartLinePosition.Line)
        .Distinct().Count();

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

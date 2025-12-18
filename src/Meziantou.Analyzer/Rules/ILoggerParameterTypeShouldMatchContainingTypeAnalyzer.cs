using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ILoggerParameterTypeShouldMatchContainingTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ILoggerParameterTypeShouldMatchContainingType,
        title: "ILogger type parameter should match containing type",
        messageFormat: "ILogger type parameter should be '{0}' instead of '{1}'",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ILoggerParameterTypeShouldMatchContainingType));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var iloggerSymbol = compilationContext.Compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.ILogger`1");
            if (iloggerSymbol is null)
                return;

            compilationContext.RegisterSymbolAction(context => AnalyzeNamedType(context, iloggerSymbol), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol iloggerSymbol)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        // Skip interfaces, abstract classes, etc. - only check concrete types
        if (namedType.TypeKind != TypeKind.Class || namedType.IsAbstract)
            return;

        // Check all constructors (including primary constructors)
        foreach (var constructor in namedType.Constructors)
        {
            // Skip implicitly declared constructors
            if (constructor.IsImplicitlyDeclared)
                continue;

            foreach (var parameter in constructor.Parameters)
            {
                // Check if parameter type is ILogger<T>
                if (parameter.Type is INamedTypeSymbol { IsGenericType: true } parameterType &&
                    parameterType.OriginalDefinition.IsEqualTo(iloggerSymbol))
                {
                    // Get the type argument of ILogger<T>
                    var typeArgument = parameterType.TypeArguments[0];

                    // Check if it matches the containing type
                    if (!typeArgument.IsEqualTo(namedType))
                    {
                        context.ReportDiagnostic(
                            Rule,
                            parameter.Locations[0],
                            namedType.Name,
                            typeArgument.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                    }
                }
            }
        }
    }
}

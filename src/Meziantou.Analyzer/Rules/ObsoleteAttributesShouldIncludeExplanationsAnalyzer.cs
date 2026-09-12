using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ObsoleteAttributesShouldIncludeExplanationsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ObsoleteAttributesShouldIncludeExplanations,
        title: "Obsolete attributes should include explanations",
        messageFormat: "Obsolete attributes should include explanations",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ObsoleteAttributesShouldIncludeExplanations));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var type = ctx.Compilation.GetBestTypeByMetadataName("System.ObsoleteAttribute");
            if (type is null)
                return;

            // ObsoleteAttribute is valid on classes, structs, enums, interfaces, delegates, constructors,
            // methods, properties, indexers, fields and events, so all those symbol kinds must be analyzed.
            ctx.RegisterSymbolAction(
                symbolContext => AnalyzeSymbol(symbolContext, type),
                SymbolKind.NamedType,
                SymbolKind.Method,
                SymbolKind.Property,
                SymbolKind.Field,
                SymbolKind.Event);
        });
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol obsoleteAttributeTypeSymbol)
    {
        var symbol = context.Symbol;

        // Synthesized symbols such as the backing field of an auto-property cannot carry an attribute of their own
        if (symbol.IsImplicitlyDeclared)
            return;

        // All the symbols of "[Obsolete] int a, b;" share the same attribute, so only report it once
        if (IsSecondaryVariableDeclarator(symbol, context.CancellationToken))
            return;

        foreach (var attribute in symbol.GetAttributes())
        {
            if (!attribute.AttributeClass.IsEqualTo(obsoleteAttributeTypeSymbol))
                continue;

            if (attribute.ConstructorArguments.Length == 0)
            {
                var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
                if (location is not null)
                {
                    context.ReportDiagnostic(Rule, location);
                }
                else
                {
                    context.ReportDiagnostic(Rule, symbol);
                }
            }
        }
    }

    private static bool IsSecondaryVariableDeclarator(ISymbol symbol, CancellationToken cancellationToken)
    {
        if (symbol.Kind is not (SymbolKind.Field or SymbolKind.Event))
            return false;

        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(cancellationToken) is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } declarator
                && declaration.Variables.Count > 1
                && declaration.Variables[0] != declarator)
            {
                return true;
            }
        }

        return false;
    }
}

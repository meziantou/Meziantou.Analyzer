using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ObjectGetTypeOnTypeInstanceAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.ObjectGetTypeOnTypeInstance,
        title: "GetType() should not be used on System.Type instances",
        messageFormat: "GetType() should not be used on System.Type instances",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ObjectGetTypeOnTypeInstance));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var typeSymbol = context.Compilation.GetBestTypeByMetadataName("System.Type");
            if (typeSymbol is null)
                return;

            context.RegisterOperationAction(context =>
            {
                var operation = (IInvocationOperation)context.Operation;
                if (operation.Instance != null && operation.TargetMethod.Name == "GetType" && operation.TargetMethod.ContainingType.IsObject())
                {
                    var instanceType = operation.Instance.GetActualType();
                    if (instanceType == null)
                        return;

                    if (instanceType.IsOrInheritFrom(typeSymbol))
                    {
                        context.ReportDiagnostic(s_rule, operation);
                    }
                }

            }, OperationKind.Invocation);
        });

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (!symbol.IsAbstract)
            return;

        foreach (var ctor in symbol.InstanceConstructors)
        {
            if (ctor.DeclaredAccessibility == Accessibility.Public || ctor.DeclaredAccessibility == Accessibility.Internal)
            {
                context.ReportDiagnostic(s_rule, ctor);
            }
        }
    }
}

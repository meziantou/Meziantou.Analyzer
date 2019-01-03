using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.UsageRules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseArrayEmptyAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.UseArrayEmpty,
            title: "Use Array.Empty<T>()",
            messageFormat: "Use Array.Empty<T>()",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseArrayEmpty));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var typeSymbol = compilationContext.Compilation.GetTypeByMetadataName("System.Array");
                if (typeSymbol == null || typeSymbol.DeclaredAccessibility != Accessibility.Public)
                    return;

                if (typeSymbol.GetMembers("Empty").FirstOrDefault() is IMethodSymbol methodSymbol &&
                    methodSymbol.DeclaredAccessibility == Accessibility.Public &&
                    methodSymbol.IsStatic && methodSymbol.Arity == 1 && methodSymbol.Parameters.Length == 0)
                {
                    compilationContext.RegisterOperationAction(AnalyzeArrayCreationOperation, OperationKind.ArrayCreation);
                }
            });
        }

        private void AnalyzeArrayCreationOperation(OperationAnalysisContext context)
        {
            var operation = (IArrayCreationOperation)context.Operation;
            if (operation.DimensionSizes.Length != 1)
                return;

            var dimensionSize = operation.DimensionSizes[0].ConstantValue;
            if (dimensionSize.HasValue && (int)dimensionSize.Value == 0)
            {
                // Cannot use Array.Empty<T>() is an attribute
                if (operation.Syntax.Ancestors().OfType<AttributeArgumentSyntax>().Any())
                    return;

                context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation()));
            }
        }
    }
}

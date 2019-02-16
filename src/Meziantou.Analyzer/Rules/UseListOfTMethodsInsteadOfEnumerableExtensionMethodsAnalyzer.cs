using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseListOfTMethodsInsteadOfEnumerableExtensionMethodsAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods,
            title: "Use List<T> methods instead of extension methods",
            messageFormat: "Use '{0}' instead of '{1}'",
            RuleCategories.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(Analyze, OperationKind.Invocation);
        }

        private void Analyze(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation == null)
                return;

            var enumerableSymbol = context.Compilation.GetTypeByMetadataName("System.Linq.Enumerable");
            if (enumerableSymbol == null)
                return;

            if (operation.Arguments.Length != 2)
                return;

            var methodName = operation.TargetMethod.Name;
            if (string.Equals(methodName, "FirstOrDefault", System.StringComparison.Ordinal))
            {
                if (!operation.TargetMethod.ContainingType.IsEqualsTo(enumerableSymbol))
                    return;

                var listSymbol = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
                if (GetActualType(operation.Arguments[0]).OriginalDefinition.IsEqualsTo(listSymbol))
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation(), "Find", methodName));
                }
            }
        }

        private static ITypeSymbol GetActualType(IArgumentOperation argument)
        {
            var value = argument.Value;
            if (value is IConversionOperation conversionOperation)
            {
                value = conversionOperation.Operand;
            }

            return value.Type;
        }
    }
}

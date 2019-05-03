using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReplaceEnumToStringWithNameofAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.ReplaceEnumToStringWithNameof,
            title: "Replace with nameof",
            messageFormat: "Replace with nameof",
            RuleCategories.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ReplaceEnumToStringWithNameof));


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(Analyze, OperationKind.Invocation);
        }

        private static void Analyze(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation.TargetMethod.Name != nameof(object.ToString))
                return;

            if (!operation.TargetMethod.ContainingType.IsEqualTo(context.Compilation.GetSpecialType(SpecialType.System_Enum)))
                return;

            var expression = operation.Children.First() as IMemberReferenceOperation;
            if (expression == null)
                return;

            if (expression.Member.ContainingType.EnumUnderlyingType == null)
                return;

            context.ReportDiagnostic(s_rule, operation);
        }
    }
}

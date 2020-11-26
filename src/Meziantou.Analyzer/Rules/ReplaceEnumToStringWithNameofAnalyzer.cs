using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ReplaceEnumToStringWithNameofAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            RuleIdentifiers.ReplaceEnumToStringWithNameof,
            title: "Replace constant Enum.ToString with nameof",
            messageFormat: "Replace constant Enum.ToString with nameof",
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

            if (operation.Children.First() is not IMemberReferenceOperation expression)
                return;

            if (expression.Member.ContainingType.EnumUnderlyingType == null)
                return;

            context.ReportDiagnostic(s_rule, operation);
        }
    }
}

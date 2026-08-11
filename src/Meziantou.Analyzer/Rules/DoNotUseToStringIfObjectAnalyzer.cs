using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseToStringIfObjectAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseToStringIfObject,
        title: "Do not call ToString() when the type falls back to object.ToString()",
        messageFormat: "ToString on '{0}' will use the default object.ToString",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseToStringIfObject));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);

            context.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(analyzerContext.AnalyzeInterpolation, OperationKind.InterpolatedString);
            context.RegisterOperationAction(AnalyzerContext.AnalyzeAdd, OperationKind.Binary);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly CultureSensitiveFormattingContext _cultureSensitiveFormattingContext = new(compilation);

        public IMethodSymbol? ObjectToStringSymbol { get; } = compilation.GetSpecialType(SpecialType.System_Object).GetMembers("ToString").OfType<IMethodSymbol>().FirstOrDefault(member => member.Parameters.Length == 0);
        public IMethodSymbol? ValueTypeToStringSymbol { get; } = compilation.GetSpecialType(SpecialType.System_ValueType).GetMembers("ToString").OfType<IMethodSymbol>().FirstOrDefault(member => member.Parameters.Length == 0);

        public void AnalyzeInterpolation(OperationAnalysisContext context)
        {
            var operation = (IInterpolatedStringOperation)context.Operation;
            foreach (var part in operation.Parts)
            {
                if (part is IInterpolationOperation content)
                {
                    AnalyzeExpression(context, content.Expression);
                }
                else if (part is IInterpolatedStringAppendOperation
                {
                    AppendCall: IInvocationOperation
                    {
                        TargetMethod.ContainingType: var containingType,
                        Arguments: [{ Value: var value }],
                    },
                } && !_cultureSensitiveFormattingContext.IsInterpolatedStringHandlerType(containingType))
                {
                    AnalyzeExpression(context, value);
                }
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation.Instance?.Type?.IsAnonymousType is true)
                return;

            if (!IsDefaultToString(operation.TargetMethod))
                return;

            if (operation.Instance is null)
                return;

            var actualType = operation.Instance.GetActualType();
            if (actualType is null)
                return;

            if (actualType.IsSealed) // Method cannot be overridden
            {
                context.ReportDiagnostic(Rule, operation, actualType.ToDisplayString());
            }
        }

        internal static void AnalyzeAdd(OperationAnalysisContext context)
        {
            var operation = (IBinaryOperation)context.Operation;
            if (!operation.Type.IsString())
                return;

            AnalyzeExpression(context, operation.LeftOperand);
            AnalyzeExpression(context, operation.RightOperand);
        }

        private static void AnalyzeExpression(DiagnosticReporter reporter, IOperation operation)
        {
            var actualType = operation.UnwrapImplicitConversionOperations().Type;
            if (actualType is null)
                return;

            if (actualType.IsAnonymousType)
                return;

            if (CultureSensitiveFormattingContext.UsesObjectToString(actualType))
            {
                reporter.ReportDiagnostic(Rule, operation, [actualType.ToDisplayString()]);
            }
        }

        private bool IsDefaultToString(IMethodSymbol method)
        {
            return method.IsEqualTo(ObjectToStringSymbol) || method.IsEqualTo(ValueTypeToStringSymbol);
        }
    }
}

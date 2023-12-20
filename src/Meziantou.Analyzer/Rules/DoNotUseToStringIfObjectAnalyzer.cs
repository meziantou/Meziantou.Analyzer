using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseToStringIfObjectAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseToStringIfObject,
        title: "Do not call the default object.ToString explicitly",
        messageFormat: "ToString on '{0}' will use the default object.ToString",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
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
            context.RegisterOperationAction(analyzerContext.AnalyzeAdd, OperationKind.Binary);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly ConcurrentDictionary<ITypeSymbol, bool> _overrideToStringCache = new(SymbolEqualityComparer.Default);

        public IMethodSymbol? ObjectToStringSymbol { get; } = compilation.GetSpecialType(SpecialType.System_Object).GetMembers("ToString").OfType<IMethodSymbol>().FirstOrDefault(member => member.Parameters.Length == 0);
        public IMethodSymbol? ValueTypeToStringSymbol { get; } = compilation.GetSpecialType(SpecialType.System_ValueType).GetMembers("ToString").OfType<IMethodSymbol>().FirstOrDefault(member => member.Parameters.Length == 0);

        // StringHandler that format values to string
        public INamedTypeSymbol?[] InterpolatedStringHandlerSymbols { get; } = [
                compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler"),
                compilation.GetBestTypeByMetadataName("System.Text.StringBuilder+AppendInterpolatedStringHandler"),
                compilation.GetBestTypeByMetadataName("System.Diagnostics.Debug+AssertInterpolatedStringHandler"),
                compilation.GetBestTypeByMetadataName("System.Diagnostics.Debug+WriteIfInterpolatedStringHandler"),
                compilation.GetBestTypeByMetadataName("System.MemoryExtensions+TryWriteInterpolatedStringHandler"),
                compilation.GetBestTypeByMetadataName("System.Text.Unicode.Utf8+TryWriteInterpolatedStringHandler"),
            ];

        public void AnalyzeInterpolation(OperationAnalysisContext context)
        {
            var operation = (IInterpolatedStringOperation)context.Operation;
            foreach (var part in operation.Parts)
            {
                if (part is IInterpolationOperation content)
                {
                    AnalyzeExpression(context, content.Expression);
                }
#if CSHARP10_OR_GREATER
                else if (part is IInterpolatedStringAppendOperation { AppendCall: IInvocationOperation { TargetMethod.ContainingType: var containingType, Arguments: [{ Value: var content2 }] } })
                {
                    if (!containingType.IsEqualToAny(InterpolatedStringHandlerSymbols))
                        continue;

                    AnalyzeExpression(context, content2);
                }
#endif
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (IsDefaultToString(operation.TargetMethod))
                return;

            if (operation.Instance is null)
                return;

            var actualType = operation.Instance.GetActualType();
            if (actualType is null)
                return;

            if (actualType.IsSealed)  // Method cannot be overridden
            {
                context.ReportDiagnostic(Rule, operation, actualType.ToDisplayString());
            }
        }

        internal void AnalyzeAdd(OperationAnalysisContext context)
        {
            var operation = (IBinaryOperation)context.Operation;
            if (!operation.Type.IsString())
                return;

            AnalyzeExpression(context, operation.LeftOperand);
            AnalyzeExpression(context, operation.RightOperand);
        }

        private void AnalyzeExpression(DiagnosticReporter reporter, IOperation operation)
        {
            var actualType = operation.UnwrapImplicitConversionOperations().Type;
            if (actualType is null)
                return;

            if (actualType.IsSealed)  // Method cannot be overridden
            {
                if (!OverrideToString(actualType))
                {
                    reporter.ReportDiagnostic(Rule, operation, [actualType.ToDisplayString()]);
                }
            }
        }

        private bool OverrideToString(ITypeSymbol? type)
        {
            if (type is null)
                return false;

            var originalType = type;
            var overrideToString = false;

            while (type is not null)
            {
                if (_overrideToStringCache.TryGetValue(type, out overrideToString))
                    break;

                var method = type.GetMembers("ToString").OfType<IMethodSymbol>().Where(m => IsDefaultToString(m) && m.Override(ObjectToStringSymbol));
                overrideToString = method.Any();
                if (overrideToString)
                    break;

                type = type.BaseType;
            }

            _overrideToStringCache.TryAdd(originalType, overrideToString);
            return overrideToString;
        }

        private bool IsDefaultToString(IMethodSymbol method)
        {
            return !method.IsEqualTo(ObjectToStringSymbol) && !method.IsEqualTo(ValueTypeToStringSymbol);
        }
    }
}

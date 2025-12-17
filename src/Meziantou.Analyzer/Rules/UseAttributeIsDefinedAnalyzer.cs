using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseAttributeIsDefinedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseAttributeIsDefined,
        title: "Use Attribute.IsDefined instead of GetCustomAttribute(s)",
        messageFormat: "Use 'Attribute.IsDefined' instead of '{0}'",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAttributeIsDefined));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var analyzerContext = new AnalyzerContext(compilationContext.Compilation);
            if (analyzerContext.IsValid)
            {
                compilationContext.RegisterOperationAction(analyzerContext.AnalyzeBinary, OperationKind.Binary);
                compilationContext.RegisterOperationAction(analyzerContext.AnalyzeIsPattern, OperationKind.IsPattern);
                compilationContext.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            }
        });
    }

    private sealed class AnalyzerContext
    {
        private readonly INamedTypeSymbol? _attributeSymbol;
        private readonly INamedTypeSymbol? _assemblySymbol;
        private readonly INamedTypeSymbol? _moduleSymbol;
        private readonly INamedTypeSymbol? _memberInfoSymbol;
        private readonly INamedTypeSymbol? _typeSymbol;
        private readonly INamedTypeSymbol? _enumerableSymbol;

        public AnalyzerContext(Compilation compilation)
        {
            _attributeSymbol = compilation.GetBestTypeByMetadataName("System.Attribute");
            _assemblySymbol = compilation.GetBestTypeByMetadataName("System.Reflection.Assembly");
            _moduleSymbol = compilation.GetBestTypeByMetadataName("System.Reflection.Module");
            _memberInfoSymbol = compilation.GetBestTypeByMetadataName("System.Reflection.MemberInfo");
            _typeSymbol = compilation.GetBestTypeByMetadataName("System.Type");
            _enumerableSymbol = compilation.GetBestTypeByMetadataName("System.Linq.Enumerable");
        }

        public bool IsValid => _attributeSymbol is not null;

        public void AnalyzeBinary(OperationAnalysisContext context)
        {
            var operation = (IBinaryOperation)context.Operation;
            if (operation.OperatorKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals))
                return;

            if (!IsGetCustomAttributeComparison(operation.LeftOperand, operation.RightOperand, out var invocation) &&
                !IsGetCustomAttributeComparison(operation.RightOperand, operation.LeftOperand, out invocation))
                return;

            context.ReportDiagnostic(Rule, operation, invocation!.TargetMethod.Name);
        }

        public void AnalyzeIsPattern(OperationAnalysisContext context)
        {
            var operation = (IIsPatternOperation)context.Operation;

            if (operation.Pattern is not (IConstantPatternOperation or INegatedPatternOperation))
                return;

            if (!IsGetCustomAttributeInvocation(operation.Value, out var invocation))
                return;

            context.ReportDiagnostic(Rule, operation, invocation!.TargetMethod.Name);
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;

            if (operation.TargetMethod.Name != "Any")
                return;

            if (!operation.TargetMethod.IsExtensionMethod)
                return;

            if (!SymbolEqualityComparer.Default.Equals(operation.TargetMethod.ContainingType, _enumerableSymbol))
                return;

            if (operation.Arguments.Length != 1)
                return;

            var instance = operation.Arguments[0].Value;
            if (!IsGetCustomAttributesInvocation(instance, out _))
                return;

            context.ReportDiagnostic(Rule, operation, "GetCustomAttributes().Any()");
        }

        private bool IsGetCustomAttributeComparison(IOperation left, IOperation right, out IInvocationOperation? invocation)
        {
            invocation = null;

            if (!IsNull(right))
                return false;

            return IsGetCustomAttributeInvocation(left, out invocation);
        }

        private static bool IsNull(IOperation operation)
        {
            return operation.ConstantValue is { HasValue: true, Value: null };
        }

        private bool IsGetCustomAttributeInvocation(IOperation operation, out IInvocationOperation? invocation)
        {
            invocation = operation as IInvocationOperation;
            if (invocation is null)
                return false;

            if (invocation.TargetMethod.Name != "GetCustomAttribute")
                return false;

            var instance = invocation.Instance;
            if (instance is null)
                return false;

            return IsValidInstanceType(instance.Type);
        }

        private bool IsGetCustomAttributesInvocation(IOperation operation, out IInvocationOperation? invocation)
        {
            invocation = operation as IInvocationOperation;
            if (invocation is null)
                return false;

            if (invocation.TargetMethod.Name != "GetCustomAttributes")
                return false;

            var instance = invocation.Instance;
            if (instance is null)
                return false;

            return IsValidInstanceType(instance.Type);
        }

        private bool IsValidInstanceType(ITypeSymbol? type)
        {
            if (type is null)
                return false;

            return type.IsOrInheritFrom(_assemblySymbol) ||
                   type.IsOrInheritFrom(_moduleSymbol) ||
                   type.IsOrInheritFrom(_memberInfoSymbol) ||
                   type.IsOrInheritFrom(_typeSymbol);
        }
    }
}

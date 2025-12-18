using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseAttributeIsDefinedAnalyzer : DiagnosticAnalyzer
{
    private const string EnumerableAnyMethodDocId = "M:System.Linq.Enumerable.Any``1(System.Collections.Generic.IEnumerable{``0})";
    private const string EnumerableCountMethodDocId = "M:System.Linq.Enumerable.Count``1(System.Collections.Generic.IEnumerable{``0})";

    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseAttributeIsDefined,
        title: "Use Attribute.IsDefined instead of GetCustomAttribute(s)",
        messageFormat: "Use 'Attribute.IsDefined' instead of '{0}'",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Detects inefficient attribute existence checks that can be replaced with Attribute.IsDefined for better performance.",
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

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly INamedTypeSymbol? _attributeSymbol = compilation.GetBestTypeByMetadataName("System.Attribute");
        private readonly INamedTypeSymbol? _assemblySymbol = compilation.GetBestTypeByMetadataName("System.Reflection.Assembly");
        private readonly INamedTypeSymbol? _moduleSymbol = compilation.GetBestTypeByMetadataName("System.Reflection.Module");
        private readonly INamedTypeSymbol? _memberInfoSymbol = compilation.GetBestTypeByMetadataName("System.Reflection.MemberInfo");
        private readonly INamedTypeSymbol? _parameterInfoSymbol = compilation.GetBestTypeByMetadataName("System.Reflection.ParameterInfo");
        private readonly INamedTypeSymbol? _typeSymbol = compilation.GetBestTypeByMetadataName("System.Type");
        private readonly INamedTypeSymbol? _customAttributeExtensionsSymbol = compilation.GetBestTypeByMetadataName("System.Reflection.CustomAttributeExtensions");
        private readonly IMethodSymbol? _enumerableAnyMethod = DocumentationCommentId.GetFirstSymbolForDeclarationId(EnumerableAnyMethodDocId, compilation) as IMethodSymbol;
        private readonly IMethodSymbol? _enumerableCountMethod = DocumentationCommentId.GetFirstSymbolForDeclarationId(EnumerableCountMethodDocId, compilation) as IMethodSymbol;

        public bool IsValid => _attributeSymbol is not null;

        public void AnalyzeBinary(OperationAnalysisContext context)
        {
            var operation = (IBinaryOperation)context.Operation;
            if (operation.OperatorKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.LessThan or BinaryOperatorKind.GreaterThanOrEqual or BinaryOperatorKind.LessThanOrEqual))
                return;

            if (IsGetCustomAttributeComparison(operation.LeftOperand, operation.RightOperand, out var invocation))
            {
                context.ReportDiagnostic(Rule, operation, invocation!.TargetMethod.Name);
                return;
            }

            if (IsGetCustomAttributesLengthComparison(operation, operation.LeftOperand, operation.RightOperand, out _))
            {
                context.ReportDiagnostic(Rule, operation, "GetCustomAttributes().Length");
            }
            else if (IsGetCustomAttributesCountComparison(operation, operation.LeftOperand, operation.RightOperand, out _))
            {
                context.ReportDiagnostic(Rule, operation, "GetCustomAttributes().Count()");
            }
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

            // Check if this is the specific Any<T>(IEnumerable<T>) method
            if (operation.TargetMethod.OriginalDefinition.IsEqualTo(_enumerableAnyMethod))
            {
                if (operation.Arguments.Length != 1)
                    return;

                var instance = operation.Arguments[0].Value;
                if (!IsGetCustomAttributesInvocation(instance, out _))
                    return;

                context.ReportDiagnostic(Rule, operation, "GetCustomAttributes().Any()");
            }
        }

        private bool IsGetCustomAttributeComparison(IOperation left, IOperation right, out IInvocationOperation? invocation)
        {
            if (right.IsNull())
                return IsGetCustomAttributeInvocation(left, out invocation);

            if (left.IsNull())
                return IsGetCustomAttributeInvocation(right, out invocation);

            invocation = null;
            return false;
        }

        private bool IsGetCustomAttributesLengthComparison(IBinaryOperation binaryOp, IOperation left, IOperation right, out IInvocationOperation? invocation)
        {
            invocation = null;

            if (left is not IPropertyReferenceOperation propertyReference)
                return false;

            if (propertyReference.Property.Name is not "Length")
                return false;

            if (propertyReference.Instance is null)
                return false;

            if (!IsGetCustomAttributesInvocation(propertyReference.Instance, out invocation))
                return false;

            // Only allow clear-cut patterns that unambiguously check for existence
            if (right.ConstantValue is not { HasValue: true, Value: int value })
                return false;

            // Validate that the operator + value combination makes sense
            return IsValidLengthComparisonPattern(binaryOp.OperatorKind, value, lengthIsOnLeft: true);
        }

        private static bool IsValidLengthComparisonPattern(BinaryOperatorKind operatorKind, int value, bool lengthIsOnLeft)
        {
            if (lengthIsOnLeft)
            {
                return (operatorKind, value) switch
                {
                    (BinaryOperatorKind.Equals, 0) => true,                           // length == 0
                    (BinaryOperatorKind.NotEquals, 0) => true,                        // length != 0
                    (BinaryOperatorKind.GreaterThan, 0) => true,                      // length > 0
                    (BinaryOperatorKind.GreaterThanOrEqual, 1) => true,               // length >= 1
                    (BinaryOperatorKind.LessThan, 1) => true,                         // length < 1
                    (BinaryOperatorKind.LessThanOrEqual, 0) => true,                  // length <= 0
                    _ => false,
                };
            }
            else
            {
                return (operatorKind, value) switch
                {
                    (BinaryOperatorKind.Equals, 0) => true,                           // 0 == length
                    (BinaryOperatorKind.NotEquals, 0) => true,                        // 0 != length
                    (BinaryOperatorKind.LessThan, 0) => true,                         // 0 < length (length > 0)
                    (BinaryOperatorKind.LessThanOrEqual, 1) => true,                  // 1 <= length (length >= 1)
                    (BinaryOperatorKind.GreaterThan, 1) => true,                      // 1 > length (length < 1)
                    (BinaryOperatorKind.GreaterThanOrEqual, 0) => true,               // 0 >= length (length <= 0)
                    _ => false,
                };
            }
        }

        private bool IsGetCustomAttributesCountComparison(IBinaryOperation binaryOp, IOperation left, IOperation right, out IInvocationOperation? invocation)
        {
            invocation = null;

            if (left is not IInvocationOperation countInvocation)
                return false;

            // Check if this is the specific Count<T>(IEnumerable<T>) method
            if (_enumerableCountMethod is null ||
                !SymbolEqualityComparer.Default.Equals(countInvocation.TargetMethod.OriginalDefinition, _enumerableCountMethod))
                return false;

            // Only detect Count() without predicate (1 argument = the collection itself)
            if (countInvocation.Arguments.Length != 1)
                return false;

            var instance = countInvocation.Arguments[0].Value;
            if (!IsGetCustomAttributesInvocation(instance, out invocation))
                return false;

            // Only allow clear-cut patterns that unambiguously check for existence
            if (right.ConstantValue is not { HasValue: true, Value: int value })
                return false;

            // Use the same validation as Length (Count and Length have the same semantics)
            return IsValidLengthComparisonPattern(binaryOp.OperatorKind, value, lengthIsOnLeft: true);
        }

        private bool IsGetCustomAttributeInvocation(IOperation operation, out IInvocationOperation? invocation)
        {
            invocation = operation.UnwrapConversionOperations() as IInvocationOperation;
            if (invocation is null)
                return false;

            if (invocation.TargetMethod.Name != "GetCustomAttribute")
                return false;

            // For extension methods, the instance is in the first argument
            var instance = invocation.Instance;
            if (instance is null && invocation.TargetMethod.IsExtensionMethod && invocation.Arguments.Length > 0)
            {
                instance = invocation.Arguments[0].Value;
            }
            else if (instance is null && invocation.TargetMethod.IsStatic && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, _attributeSymbol))
            {
                if (invocation.Arguments.Length > 0)
                {
                    instance = invocation.Arguments[0].Value;
                }
            }

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

            if (!IsMethodFromReflectionTypes(invocation.TargetMethod))
                return false;

            // For extension methods, the instance is in the first argument
            var instance = invocation.Instance;
            if (instance is null && invocation.TargetMethod.IsExtensionMethod && invocation.Arguments.Length > 0)
            {
                instance = invocation.Arguments[0].Value;
            }
            else if (instance is null && invocation.TargetMethod.IsStatic && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, _attributeSymbol))
            {
                if (invocation.Arguments.Length > 0)
                {
                    instance = invocation.Arguments[0].Value;
                }
            }

            if (instance is null)
                return false;

            return IsValidInstanceType(instance.Type);
        }

        private bool IsMethodFromReflectionTypes(IMethodSymbol method)
        {
            if (SymbolEqualityComparer.Default.Equals(method.ContainingType, _customAttributeExtensionsSymbol))
                return true;

            if (SymbolEqualityComparer.Default.Equals(method.ContainingType, _attributeSymbol))
                return true;

            // Check for extension methods on reflection types
            if (method.Name is "GetCustomAttribute" or "GetCustomAttributes")
            {
                if (method.Parameters.Length > 0)
                {
                    var firstParamType = method.Parameters[0].Type;
                    if (IsReflectionType(firstParamType) || IsParameterInfo(firstParamType))
                        return true;
                }
            }

            return false;
        }

        private bool IsParameterInfo(ITypeSymbol type) => type.IsEqualTo(_parameterInfoSymbol);
        private bool IsReflectionType(ITypeSymbol type) => type.IsEqualToAny(_assemblySymbol, _moduleSymbol, _memberInfoSymbol, _typeSymbol);

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

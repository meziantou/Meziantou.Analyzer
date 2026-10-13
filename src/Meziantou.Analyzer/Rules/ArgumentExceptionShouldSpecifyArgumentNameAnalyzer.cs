using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class ArgumentExceptionShouldSpecifyArgumentNameAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ArgumentExceptionShouldSpecifyArgumentName,
        title: "Specify the parameter name in ArgumentException",
        messageFormat: "{0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ArgumentExceptionShouldSpecifyArgumentName));

    private static readonly DiagnosticDescriptor NameofRule = new(
        RuleIdentifiers.UseNameofOperator,
        title: "Use nameof operator in ArgumentException",
        messageFormat: "Use nameof operator",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseNameofOperator));

    private static readonly ConfigurationDefinition<bool> ConsiderMemberAccessAsParameterConfiguration = new(RuleIdentifiers.ArgumentExceptionShouldSpecifyArgumentName + ".consider_member_access_as_parameter", defaultValue: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule, NameofRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);
            if (!analyzerContext.IsValid)
                return;

            context.RegisterOperationAction(analyzerContext.AnalyzeObjectCreation, OperationKind.ObjectCreation);

            if (analyzerContext.CallerArgumentExpressionAttribute is not null)
            {
                context.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            }
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        public INamedTypeSymbol ArgumentExceptionType { get; } = compilation.GetTypeByMetadataName("System.ArgumentException")!;
        public INamedTypeSymbol ArgumentNullExceptionType { get; } = compilation.GetTypeByMetadataName("System.ArgumentNullException")!;
        public INamedTypeSymbol? ArgumentOutOfRangeExceptionType { get; } = compilation.GetTypeByMetadataName("System.ArgumentOutOfRangeException");
        public INamedTypeSymbol? InvalidEnumArgumentExceptionType { get; } = compilation.GetTypeByMetadataName("System.ComponentModel.InvalidEnumArgumentException");
        public INamedTypeSymbol? CallerArgumentExpressionAttribute { get; } = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.CallerArgumentExpressionAttribute");

        public bool IsValid => ArgumentExceptionType is not null && ArgumentNullExceptionType is not null;

        // Validate throw new ArgumentException("message", "paramName");
        public void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            var op = (IObjectCreationOperation)context.Operation;
            if (op is null)
                return;

            var type = op.Type;
            if (type is null)
                return;

            if (!type.IsOrInheritsFrom(ArgumentExceptionType))
                return;

            var parameterName = "paramName";
            if (type.IsEqualTo(InvalidEnumArgumentExceptionType))
            {
                parameterName = "argumentName";
            }

            foreach (var argument in op.Arguments)
            {
                if (argument.Parameter is null || !string.Equals(argument.Parameter.Name, parameterName, StringComparison.Ordinal))
                    continue;

                if (argument.Value.ConstantValue.HasValue)
                {
                    if (argument.Value.ConstantValue.Value is string value)
                    {
                        if (IsNameOfParameterOfContainingFunction(argument.Value, context.ContainingSymbol))
                            return;

                        if (IsParameterName(op, value, context.CancellationToken))
                        {
                            if (argument.Value is not INameOfOperation)
                            {
                                var properties = ImmutableDictionary<string, string?>.Empty.Add(ArgumentExceptionShouldSpecifyArgumentNameAnalyzerCommon.ArgumentNameKey, value);
                                context.ReportDiagnostic(NameofRule, properties, argument.Value);
                            }

                            return;
                        }

                        var considerMemberAccessAsParameter = ConsiderMemberAccessAsParameter(context, argument.Value);
                        if (considerMemberAccessAsParameter)
                        {
                            var dotIndex = value.IndexOf('.', StringComparison.Ordinal);
                            if (dotIndex > 0 && IsParameterName(op, value[..dotIndex], context.CancellationToken))
                                return;
                        }

                        if (argument.Syntax is ArgumentSyntax argumentSyntax)
                        {
                            context.ReportDiagnostic(Rule, argumentSyntax.Expression, $"'{value}' is not a valid parameter name");
                        }
                        else
                        {
                            context.ReportDiagnostic(Rule, argument, $"'{value}' is not a valid parameter name");
                        }

                        return;
                    }
                }

                // Cannot determine the value of the argument
                return;
            }

            if (type is not INamedTypeSymbol namedType)
                return;

            foreach (var ctor in namedType.InstanceConstructors)
            {
                foreach (var parameter in ctor.Parameters)
                {
                    if (parameter.Name is "paramName" or "argumentName" && parameter.Type.IsString())
                    {
                        context.ReportDiagnostic(Rule, op, $"Use an overload of '{type.ToDisplayString()}' with the parameter name");
                        return;
                    }
                }
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var op = (IInvocationOperation)context.Operation;
            if (op is null)
                return;

            var method = op.TargetMethod;
            if (method is null || !method.IsStatic)
                return;

            // Check if the method name starts with "ThrowIf"
            if (!method.Name.StartsWith("ThrowIf", StringComparison.Ordinal))
                return;

            // There must be at least one argument
            if (op.Arguments.Length == 0)
                return;

            // Check if this is a ThrowIfXxx method on ArgumentException, ArgumentNullException, or ArgumentOutOfRangeException
            var containingType = method.ContainingType;
            if (containingType is null)
                return;

            if (!containingType.IsEqualToAny(ArgumentExceptionType, ArgumentNullExceptionType, ArgumentOutOfRangeExceptionType))
                return;

            // Find the parameter with CallerArgumentExpressionAttribute
            foreach (var parameter in method.Parameters)
            {
                if (!parameter.Type.IsString())
                    continue;

                var attribute = parameter.GetFirstAttribute(CallerArgumentExpressionAttribute);
                if (attribute is null)
                    continue;

                if (attribute.ConstructorArguments.Length == 0)
                    continue;

                // Get the parameter name referenced by the CallerArgumentExpressionAttribute
                var referencedParameterName = attribute.ConstructorArguments[0].Value as string;
                if (string.IsNullOrEmpty(referencedParameterName))
                    continue;

                // Find the parameter being referenced
                var referencedParameter = method.Parameters.FirstOrDefault(p => p.Name == referencedParameterName);
                if (referencedParameter is null)
                    continue;

                // Find the argument for the paramName parameter
                var paramNameArgument = op.Arguments.FirstOrDefault(arg => arg.Parameter is not null && arg.Parameter.IsEqualTo(parameter));
                if (paramNameArgument is not null && paramNameArgument.ArgumentKind is ArgumentKind.Explicit && paramNameArgument.Value is not null)
                {
                    ValidateParamNameArgument(context, paramNameArgument);
                    return;
                }

                // Find the argument for the referenced parameter (the one being validated)
                var referencedArgument = op.Arguments.FirstOrDefault(arg => arg.Parameter is not null && arg.Parameter.IsEqualTo(referencedParameter));
                if (referencedArgument is not null)
                {
                    ValidateExpression(context, referencedArgument);
                    return;
                }
            }
        }

        private static bool ConsiderMemberAccessAsParameter(OperationAnalysisContext context, IOperation operation)
            => context.Options.GetConfigurationValue(operation, ConsiderMemberAccessAsParameterConfiguration);

        private static void ValidateParamNameArgument(OperationAnalysisContext context, IArgumentOperation paramNameArgument)
        {
            // Check if the argument is a constant string value
            if (!paramNameArgument.Value.ConstantValue.HasValue || paramNameArgument.Value.ConstantValue.Value is not string paramNameValue)
                return;

            if (IsNameOfParameterOfContainingFunction(paramNameArgument.Value, context.ContainingSymbol))
                return;

            if (IsParameterName(paramNameArgument, paramNameValue, context.CancellationToken))
            {
                if (paramNameArgument.Value is not INameOfOperation)
                {
                    var properties = ImmutableDictionary<string, string?>.Empty.Add(ArgumentExceptionShouldSpecifyArgumentNameAnalyzerCommon.ArgumentNameKey, paramNameValue);
                    context.ReportDiagnostic(NameofRule, properties, paramNameArgument.Value);
                }

                return;
            }

            var considerMemberAccessAsParameter = ConsiderMemberAccessAsParameter(context, paramNameArgument.Value);
            if (considerMemberAccessAsParameter)
            {
                var dotIndex = paramNameValue.IndexOf('.', StringComparison.Ordinal);
                if (dotIndex > 0 && IsParameterName(paramNameArgument, paramNameValue[..dotIndex], context.CancellationToken))
                    return;
            }

            context.ReportDiagnostic(Rule, paramNameArgument, $"'{paramNameValue}' is not a valid parameter name");
        }

        private static void ValidateExpression(OperationAnalysisContext context, IArgumentOperation argument)
        {
            if (argument.Value is null)
                return;

            var unwrappedValue = argument.Value.UnwrapImplicitConversions();
            if (unwrappedValue is IParameterReferenceOperation)
            {
                // Parameter references are always valid - no need to validate the name
                return;
            }

            var considerMemberAccessAsParameter = ConsiderMemberAccessAsParameter(context, argument.Value);
            if (considerMemberAccessAsParameter && IsRootParameterReference(unwrappedValue))
                return;

            context.ReportDiagnostic(Rule, argument, "The expression does not match a parameter");
        }

        private static bool IsRootParameterReference(IOperation operation)
        {
            var current = operation;
            while (current is IMemberReferenceOperation memberRef)
            {
                // A null instance means this is a static member access (no receiver),
                // which cannot be rooted in a parameter reference.
                if (memberRef.Instance is null)
                    return false;

                current = memberRef.Instance.UnwrapImplicitConversions();
            }

            return current is IParameterReferenceOperation;
        }

        // nameof(parameter) is the most common value, and it is valid without looking up any symbol when the parameter
        // is declared by the function that contains the operation. The parameters of the other functions need the lookup,
        // as they are not valid when the operation is in a static local function or in a static lambda.
        private static bool IsNameOfParameterOfContainingFunction(IOperation operation, ISymbol containingSymbol)
        {
            if (operation is not INameOfOperation { Argument: IParameterReferenceOperation { Parameter.ContainingSymbol: var parameterOwner } })
                return false;

            for (var current = operation.Parent; current is not null; current = current.Parent)
            {
                switch (current)
                {
                    case IAnonymousFunctionOperation anonymousFunction:
                        return anonymousFunction.Symbol.IsEqualTo(parameterOwner);

                    case ILocalFunctionOperation localFunction:
                        return localFunction.Symbol.IsEqualTo(parameterOwner);
                }
            }

            return containingSymbol.IsEqualTo(parameterOwner);
        }

        // Only look up the symbols named after the value: enumerating all the symbols in scope binds every name
        // of the usings and of the containing namespaces, which is far more expensive.
        private static bool IsParameterName(IOperation operation, string name, CancellationToken cancellationToken)
        {
            if (!SyntaxFacts.IsValidIdentifier(name))
                return false;

            foreach (var symbol in operation.LookupAvailableSymbols(name, cancellationToken))
            {
                if (symbol is IParameterSymbol)
                    return true;
            }

            return false;
        }
    }
}

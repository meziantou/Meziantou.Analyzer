using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, NameofRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var argumentExceptionType = context.Compilation.GetBestTypeByMetadataName("System.ArgumentException");
            var argumentNullExceptionType = context.Compilation.GetBestTypeByMetadataName("System.ArgumentNullException");
            var argumentOutOfRangeExceptionType = context.Compilation.GetBestTypeByMetadataName("System.ArgumentOutOfRangeException");
            var callerArgumentExpressionAttribute = context.Compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.CallerArgumentExpressionAttribute");

            if (argumentExceptionType is null || argumentNullExceptionType is null)
                return;

            context.RegisterOperationAction(Analyze, OperationKind.ObjectCreation);

            if (callerArgumentExpressionAttribute is not null)
            {
                context.RegisterOperationAction(ctx => AnalyzeInvocation(ctx, argumentExceptionType, argumentNullExceptionType, argumentOutOfRangeExceptionType, callerArgumentExpressionAttribute), OperationKind.Invocation);
            }
        });
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        var op = (IObjectCreationOperation)context.Operation;
        if (op is null)
            return;

        var type = op.Type;
        if (type is null)
            return;

        var exceptionType = context.Compilation.GetBestTypeByMetadataName("System.ArgumentException");
        if (exceptionType is null)
            return;

        if (!type.IsOrInheritFrom(exceptionType))
            return;

        var parameterName = "paramName";
        if (type.IsEqualTo(context.Compilation.GetBestTypeByMetadataName("System.ComponentModel.InvalidEnumArgumentException")))
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
                    var parameterNames = GetParameterNames(op, context.CancellationToken);
                    if (parameterNames.Contains(value, StringComparer.Ordinal))
                    {
                        if (argument.Value is not INameOfOperation)
                        {
                            var properties = ImmutableDictionary<string, string?>.Empty.Add(ArgumentExceptionShouldSpecifyArgumentNameAnalyzerCommon.ArgumentNameKey, value);
                            context.ReportDiagnostic(NameofRule, properties, argument.Value);
                        }

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

        var ctors = type.GetMembers(".ctor").OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Constructor);
        foreach (var ctor in ctors)
        {
            if (ctor.Parameters.Any(p => p.Name is "paramName" or "argumentName" && p.Type.IsString()))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, op.Syntax.GetLocation(), $"Use an overload of '{type.ToDisplayString()}' with the parameter name"));
                return;
            }
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol argumentExceptionType, INamedTypeSymbol argumentNullExceptionType, INamedTypeSymbol? argumentOutOfRangeExceptionType, INamedTypeSymbol callerArgumentExpressionAttribute)
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

        // Check if this is a ThrowIfXxx method on ArgumentException, ArgumentNullException, or ArgumentOutOfRangeException
        var containingType = method.ContainingType;
        if (containingType is null)
            return;

        if (!containingType.IsEqualToAny(argumentExceptionType, argumentNullExceptionType, argumentOutOfRangeExceptionType))
            return;

        // The first parameter is the argument being validated
        if (op.Arguments.Length == 0)
            return;

        // Find the parameter with CallerArgumentExpressionAttribute
        foreach (var parameter in method.Parameters)
        {
            if (!parameter.Type.IsString())
                continue;

            var attribute = parameter.GetAttribute(callerArgumentExpressionAttribute);
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
            if (paramNameArgument is not null && paramNameArgument.Value is not null)
            {
                ValidateParamNameArgument(context, op, paramNameArgument);
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

    private static void ValidateParamNameArgument(OperationAnalysisContext context, IInvocationOperation op, IArgumentOperation paramNameArgument)
    {
        // Check if the argument is a constant string value
        if (!paramNameArgument.Value.ConstantValue.HasValue || paramNameArgument.Value.ConstantValue.Value is not string paramNameValue)
            return;

        var availableParameterNames = GetParameterNames(op, context.CancellationToken);
        if (availableParameterNames.Contains(paramNameValue, StringComparer.Ordinal))
        {
            if (paramNameArgument.Value is not INameOfOperation)
            {
                var properties = ImmutableDictionary<string, string?>.Empty.Add(ArgumentExceptionShouldSpecifyArgumentNameAnalyzerCommon.ArgumentNameKey, paramNameValue);
                context.ReportDiagnostic(NameofRule, properties, paramNameArgument.Value);
            }

            return;
        }

        context.ReportDiagnostic(Rule, paramNameArgument, $"'{paramNameValue}' is not a valid parameter name");
    }

    private static void ValidateExpression(OperationAnalysisContext context, IArgumentOperation argument)
    {
        if (argument.Value is null)
            return;

        var unwrappedValue = argument.Value.UnwrapImplicitConversionOperations();

        // Check if the argument is a parameter reference
        if (unwrappedValue is IParameterReferenceOperation)
        {
            // Parameter references are always valid - no need to validate the name
            return;
        }

        // Check if the argument is a member access to a property/field
        if (unwrappedValue is IMemberReferenceOperation memberRef)
        {
            var memberName = memberRef.Member.Name;
            var availableParameterNames = GetParameterNames(argument, context.CancellationToken);
            if (!availableParameterNames.Contains(memberName, StringComparer.Ordinal))
            {
                context.ReportDiagnostic(Rule, argument, $"'{memberName}' is not a valid parameter name");
            }
            return;
        }

        // If the expression is not a parameter or member reference, report an error
        // as it cannot be matched to a parameter name
        context.ReportDiagnostic(Rule, argument, "The expression does not match a parameter");
    }

    private static IEnumerable<string> GetParameterNames(IOperation operation, CancellationToken cancellationToken)
    {
        var symbols = operation.LookupAvailableSymbols(cancellationToken);
        foreach (var symbol in symbols)
        {
            switch (symbol)
            {
                case IParameterSymbol parameterSymbol:
                    yield return parameterSymbol.Name;
                    break;
            }
        }
    }
}

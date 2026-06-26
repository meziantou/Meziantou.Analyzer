using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseInKeywordForInParameterAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor RuleUseInForInParameter = new(
        RuleIdentifiers.UseInKeywordForInParameter,
        title: "Use in keyword for in parameter",
        messageFormat: "Use in keyword for in parameter",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseInKeywordForInParameter));

    private static readonly DiagnosticDescriptor RuleUseInToSelectInOverload = new(
        RuleIdentifiers.UseInKeywordToSelectInOverload,
        title: "Use in keyword to call the in overload",
        messageFormat: "Use in keyword to call the in overload",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseInKeywordToSelectInOverload));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleUseInForInParameter, RuleUseInToSelectInOverload);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var overloadFinder = new OverloadFinder(context.Compilation);
            context.RegisterOperationAction(context => AnalyzeArgument(context, overloadFinder), OperationKind.Argument);
        });
    }

    private static void AnalyzeArgument(OperationAnalysisContext context, OverloadFinder overloadFinder)
    {
        var operation = (IArgumentOperation)context.Operation;
        if (operation.Parameter is null)
            return;

        if (operation.Syntax is not ArgumentSyntax argumentSyntax)
            return;

        if (!argumentSyntax.RefKindKeyword.IsKind(SyntaxKind.None))
            return;

        if (!CanUseInAtCallSite(operation))
            return;

        if (operation.Parameter.RefKind is RefKind.In)
        {
            context.ReportDiagnostic(RuleUseInForInParameter, argumentSyntax);
            return;
        }

        if (operation.Parameter.RefKind is not RefKind.None)
            return;

        if (!TryGetInvocationAndArgumentIndex(operation, out var invocationOperation, out var argumentIndex))
            return;

        if (HasInOverloadWithEquivalentParameters(invocationOperation, argumentIndex, overloadFinder))
        {
            context.ReportDiagnostic(RuleUseInToSelectInOverload, argumentSyntax);
        }
    }

    private static bool CanUseInAtCallSite(IArgumentOperation operation)
    {
        if (operation.ArgumentKind is ArgumentKind.ParamArray)
            return false;

        if (HasNonIdentityConversion(operation.Value))
            return false;

        return IsVariableReference(operation.Value);
    }

    private static bool HasNonIdentityConversion(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            if (!conversion.Conversion.IsIdentity)
                return true;

            operation = conversion.Operand;
        }

        return false;
    }

    private static bool IsVariableReference(IOperation operation)
    {
        operation = operation.UnwrapConversionOperations();

        return operation.Kind switch
        {
            OperationKind.LocalReference => true,
            OperationKind.ParameterReference => true,
            OperationKind.FieldReference => true,
            OperationKind.ArrayElementReference => true,
            OperationKind.InstanceReference => true,
            _ => false,
        };
    }

    private static bool TryGetInvocationAndArgumentIndex(IArgumentOperation operation, out IInvocationOperation invocationOperation, out int argumentIndex)
    {
        invocationOperation = null!;
        argumentIndex = -1;

        if (operation.Parent is not IInvocationOperation invocation)
            return false;

        for (var i = 0; i < invocation.Arguments.Length; i++)
        {
            if (invocation.Arguments[i] == operation)
            {
                invocationOperation = invocation;
                argumentIndex = i;
                return true;
            }
        }

        return false;
    }

    private static bool HasInOverloadWithEquivalentParameters(IInvocationOperation invocationOperation, int argumentIndex, OverloadFinder overloadFinder)
    {
        var targetMethod = invocationOperation.TargetMethod;
        if (targetMethod.ContainingType is null)
            return false;

        var currentParameter = targetMethod.Parameters[argumentIndex];
        if (currentParameter.RefKind is not RefKind.None)
            return false;

        var options = new OverloadOptions(
            IncludeObsoleteMembers: false,
            IncludeExperimentalMembers: false,
            AllowOptionalParameters: false,
            SyntaxNode: invocationOperation.Syntax,
            AllowNumericConversion: false,
            AllowParamsToNonParamsCompatibility: false,
            AllowInModifierCompatibility: true,
            AllowInterfaceConversions: false,
            ShouldCheckMethod: method =>
            {
                if (method.Parameters.Length != targetMethod.Parameters.Length)
                    return false;

                if (method.Parameters[argumentIndex].RefKind is not RefKind.In)
                    return false;

                return HasEquivalentParameterList(targetMethod.Parameters, method.Parameters, argumentIndex);
            });

        return !overloadFinder.FindSimilarMethods(targetMethod, options, targetMethod.Name, additionalParameterTypes: default).IsEmpty;
    }

    private static bool HasEquivalentParameterList(ImmutableArray<IParameterSymbol> currentParameters, ImmutableArray<IParameterSymbol> candidateParameters, int argumentIndex)
    {
        if (currentParameters.Length != candidateParameters.Length)
            return false;

        for (var i = 0; i < currentParameters.Length; i++)
        {
            var current = currentParameters[i];
            var candidate = candidateParameters[i];
            if (i == argumentIndex)
            {
                if (current.RefKind is not (RefKind.None or RefKind.In))
                    return false;

                if (!SymbolEqualityComparer.Default.Equals(current.Type, candidate.Type))
                    return false;

                if (current.IsParams != candidate.IsParams)
                    return false;
            }
            else
            {
                if (current.RefKind != candidate.RefKind)
                    return false;

                if (!SymbolEqualityComparer.Default.Equals(current.Type, candidate.Type))
                    return false;

                if (current.IsParams != candidate.IsParams)
                    return false;
            }
        }

        return true;
    }
}

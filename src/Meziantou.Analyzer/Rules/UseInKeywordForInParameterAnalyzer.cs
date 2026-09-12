using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    private static readonly ConfigurationDefinition<bool> IncludeExtensionMethodsFromNotImportedNamespacesConfiguration = new(RuleIdentifiers.UseInKeywordToSelectInOverload + ".include_extension_methods_from_not_imported_namespaces", defaultValue: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleUseInForInParameter, RuleUseInToSelectInOverload);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

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

        if (operation.Parent is not IInvocationOperation invocationOperation)
            return;

        // The arguments are in source order, which differs from the parameter order when named arguments are reordered
        var includeExtensionMethodsFromNotImportedNamespaces = context.Options.GetConfigurationValue(operation, IncludeExtensionMethodsFromNotImportedNamespacesConfiguration);
        if (FindInOverloadWithEquivalentParameters(invocationOperation, operation.Parameter.Ordinal, overloadFinder, includeExtensionMethodsFromNotImportedNamespaces) is { } overload)
        {
            var properties = ImmutableDictionary<string, string?>.Empty;
            if (includeExtensionMethodsFromNotImportedNamespaces && overloadFinder.GetNamespaceToImport(overload, invocationOperation.Syntax) is { } namespaceToImport)
            {
                properties = properties.Add(OverloadFinder.NamespaceToImportPropertyName, namespaceToImport);
            }

            context.ReportDiagnostic(RuleUseInToSelectInOverload, properties, argumentSyntax);
        }
    }

    private static bool CanUseInAtCallSite(IArgumentOperation operation)
    {
        if (operation.ArgumentKind is ArgumentKind.ParamArray)
            return false;

        return UseInKeywordForInParameterCommon.CanBePassedByReference(operation.Value);
    }

    private static IMethodSymbol? FindInOverloadWithEquivalentParameters(IInvocationOperation invocationOperation, int parameterIndex, OverloadFinder overloadFinder, bool includeExtensionMethodsFromNotImportedNamespaces)
    {
        var targetMethod = invocationOperation.TargetMethod;
        if (targetMethod.ContainingType is null)
            return null;

        if (parameterIndex >= targetMethod.Parameters.Length)
            return null;

        var currentParameter = targetMethod.Parameters[parameterIndex];
        if (currentParameter.RefKind is not RefKind.None)
            return null;

        // Named arguments are bound by name, so the overload must use the same names to bind the arguments to the same parameters
        var compareParameterNames = HasNamedArguments(invocationOperation);

        var options = new OverloadOptions(
            IncludeObsoleteMembers: false,
            IncludeExperimentalMembers: false,
            AllowOptionalParameters: false,
            SyntaxNode: invocationOperation.Syntax,
            AllowNumericConversion: false,
            AllowParamsToNonParamsCompatibility: false,
            AllowInModifierCompatibility: true,
            AllowInterfaceConversions: false,
            IncludeExtensionMethodsFromNotImportedNamespaces: includeExtensionMethodsFromNotImportedNamespaces,
            ShouldCheckMethod: method =>
            {
                if (method.Parameters.Length != targetMethod.Parameters.Length)
                    return false;

                if (method.Parameters[parameterIndex].RefKind is not RefKind.In)
                    return false;

                return HasEquivalentParameterList(targetMethod.Parameters, method.Parameters, parameterIndex, compareParameterNames);
            });

        return overloadFinder.FindFirstSimilarMethod(targetMethod, options, targetMethod.Name, additionalParameterTypes: default);
    }

    private static bool HasNamedArguments(IInvocationOperation invocationOperation)
    {
        foreach (var argument in invocationOperation.Arguments)
        {
            if (argument.Syntax is ArgumentSyntax { NameColon: not null })
                return true;
        }

        return false;
    }

    private static bool HasEquivalentParameterList(ImmutableArray<IParameterSymbol> currentParameters, ImmutableArray<IParameterSymbol> candidateParameters, int parameterIndex, bool compareParameterNames)
    {
        if (currentParameters.Length != candidateParameters.Length)
            return false;

        for (var i = 0; i < currentParameters.Length; i++)
        {
            var current = currentParameters[i];
            var candidate = candidateParameters[i];
            if (compareParameterNames && !string.Equals(current.Name, candidate.Name, StringComparison.Ordinal))
                return false;

            if (i == parameterIndex)
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

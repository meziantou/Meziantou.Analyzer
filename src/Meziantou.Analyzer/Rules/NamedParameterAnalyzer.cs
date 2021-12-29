using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NamedParameterAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.UseNamedParameter,
        title: "Add argument name to improve readability",
        messageFormat: "Name the parameter to improve the readability of the code",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseNamedParameter));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var attributeTokenType = compilationContext.Compilation.GetTypeByMetadataName("SkipNamedAttribute");

            var objectType = compilationContext.Compilation.GetSpecialType(SpecialType.System_Object);
            var taskTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskGenericTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var valueTaskTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
            var valueTaskGenericTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
            var taskCompletionSourceType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.TaskCompletionSource`1");
            var methodBaseTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Reflection.MethodBase");
            var fieldInfoTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Reflection.FieldInfo");
            var propertyInfoTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Reflection.PropertyInfo");
            var msTestAssertTokenType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.Assert");
            var nunitAssertTokenType = compilationContext.Compilation.GetTypeByMetadataName("NUnit.Framework.Assert");
            var xunitAssertTokenType = compilationContext.Compilation.GetTypeByMetadataName("Xunit.Assert");
            var keyValuePairTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Collection.Generic.KeyValuePair`2");
            var propertyBuilderType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder`1");
            var syntaxNodeType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.SyntaxNode");

            compilationContext.RegisterSyntaxNodeAction(syntaxContext =>
            {
                var argument = (ArgumentSyntax)syntaxContext.Node;
                if (argument.NameColon != null)
                    return;

                if (argument.Expression == null)
                    return;

                var expression = argument.Expression;
                if (expression.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    if (!GetConfiguration(syntaxContext.Options, expression).HasFlag(ArgumentExpressionKinds.Null))
                        return;
                }
                else if (expression.IsKind(SyntaxKind.NumericLiteralExpression))
                {
                    if (!GetConfiguration(syntaxContext.Options, expression).HasFlag(ArgumentExpressionKinds.Numeric))
                        return;
                }
                else if (IsBooleanExpression(expression))
                {
                    if (!GetConfiguration(syntaxContext.Options, expression).HasFlag(ArgumentExpressionKinds.Boolean))
                        return;
                }
                else if (IsStringExpression(expression))
                {
                    if (!GetConfiguration(syntaxContext.Options, expression).HasFlag(ArgumentExpressionKinds.String))
                        return;
                }
                else
                {
                    return;
                }

                if (argument.Parent.IsKind(SyntaxKind.TupleExpression))
                    return; // Don't consider tuple

                // Exclude in some methods such as ConfigureAwait(false)
                var invocationExpression = argument.FirstAncestorOrSelf<ExpressionSyntax>(t => t.IsKind(SyntaxKind.InvocationExpression) || t.IsKind(SyntaxKind.ObjectCreationExpression) || t.IsKind(SyntaxKind.ElementAccessExpression));
                if (invocationExpression != null)
                {
                    BaseArgumentListSyntax? argumentList = invocationExpression switch
                    {
                        InvocationExpressionSyntax invocationExpressionSyntax => invocationExpressionSyntax.ArgumentList,
                        ObjectCreationExpressionSyntax objectCreationExpressionSyntax => objectCreationExpressionSyntax.ArgumentList,
                        ElementAccessExpressionSyntax elementAccessExpressionSyntax => elementAccessExpressionSyntax.ArgumentList,
                        _ => null,
                    };

                    if (argumentList is null)
                        return;

                    var invokedMethodSymbol = syntaxContext.SemanticModel.GetSymbolInfo(invocationExpression).Symbol;
                    if (invokedMethodSymbol != null)
                    {
                        var invokedMethodParameters = invokedMethodSymbol switch
                        {
                            IMethodSymbol methodSymbol => methodSymbol.Parameters,
                            IPropertySymbol propertySymbol => propertySymbol.Parameters,
                            _ => ImmutableArray<IParameterSymbol>.Empty,
                        };

                        var argumentIndex = ArgumentIndex(argument);

                        bool IsParams(SyntaxNode node)
                        {
                            if (argumentIndex > invokedMethodParameters.Length - 1)
                                return true;

                            if (invokedMethodParameters.Length == 0)
                                return false;

                            var lastParameter = invokedMethodParameters[invokedMethodParameters.Length - 1];
                            if (argumentIndex == invokedMethodParameters.Length - 1 && lastParameter.IsParams)
                            {
                                if (argumentList.Arguments.Count > invokedMethodParameters.Length)
                                    return true;

                                if (expression.IsKind(SyntaxKind.NullLiteralExpression))
                                    return false;

                                var type = syntaxContext.SemanticModel.GetTypeInfo(node).ConvertedType;
                                return !type.IsEqualTo(lastParameter.Type);
                            }

                            return false;
                        };

                        if (IsParams(argument))
                            return;

                        if (invokedMethodParameters.Length == 1 && invokedMethodSymbol.Name.StartsWith("Is", StringComparison.Ordinal))
                            return;

                        if (invokedMethodParameters.Length == 1 && invokedMethodSymbol.Name.StartsWith("Enable", StringComparison.Ordinal))
                            return;

                        if (invokedMethodParameters.Length == 1 && invokedMethodSymbol.Name == nameof(Task.ConfigureAwait))
                            return;

                        if (IsMethod(invokedMethodSymbol, objectType, nameof(object.Equals)))
                            return;

                        if (IsMethod(invokedMethodSymbol, objectType, nameof(object.ReferenceEquals)))
                            return;

                        if (IsMethod(invokedMethodSymbol, taskTokenType, nameof(Task.FromResult)))
                            return;

                        if (IsMethod(invokedMethodSymbol, taskCompletionSourceType, nameof(TaskCompletionSource<object>.SetResult)))
                            return;

                        if (IsMethod(invokedMethodSymbol, taskCompletionSourceType, nameof(TaskCompletionSource<object>.TrySetResult)))
                            return;

                        if (IsMethod(invokedMethodSymbol, methodBaseTokenType, nameof(MethodBase.Invoke)) && argumentIndex == 0)
                            return;

                        if (IsMethod(invokedMethodSymbol, fieldInfoTokenType, nameof(FieldInfo.SetValue)) && argumentIndex == 0)
                            return;

                        if (IsMethod(invokedMethodSymbol, fieldInfoTokenType, nameof(FieldInfo.GetValue)) && argumentIndex == 0)
                            return;

                        if (IsMethod(invokedMethodSymbol, propertyInfoTokenType, nameof(PropertyInfo.SetValue)) && argumentIndex == 0)
                            return;

                        if (IsMethod(invokedMethodSymbol, propertyInfoTokenType, nameof(PropertyInfo.GetValue)) && argumentIndex == 0)
                            return;

                        if (IsMethod(invokedMethodSymbol, msTestAssertTokenType, "*"))
                            return;

                        if (IsMethod(invokedMethodSymbol, nunitAssertTokenType, "*"))
                            return;

                        if (IsMethod(invokedMethodSymbol, xunitAssertTokenType, "*"))
                            return;

                        if ((string.Equals(invokedMethodSymbol.Name, "Parse", StringComparison.Ordinal) || string.Equals(invokedMethodSymbol.Name, "TryParse", StringComparison.Ordinal)) && argumentIndex == 0)
                            return;

                        // Indexer with only 1 argument
                        if (invocationExpression is ElementAccessExpressionSyntax && invokedMethodParameters.Length == 1)
                            return;

                        // e.g. SyntaxNode.WithElse
                        if (invokedMethodSymbol.Name.StartsWith("With", StringComparison.Ordinal) && invokedMethodSymbol.ContainingType.IsOrInheritFrom(syntaxNodeType))
                            return;

                        var operation = syntaxContext.SemanticModel.GetOperation(argument, syntaxContext.CancellationToken);
                        if (operation != null && operation.IsInExpressionArgument())
                            return;

                        if (syntaxContext.Options.TryGetConfigurationValue(expression.SyntaxTree, RuleIdentifiers.UseNamedParameter + ".excluded_methods_regex", out var excludedMethodsRegex))
                        {
                            var declarationId = DocumentationCommentId.CreateDeclarationId(invokedMethodSymbol);
                            if (Regex.IsMatch(declarationId, excludedMethodsRegex))
                                return;
                        }

                        if (syntaxContext.Options.TryGetConfigurationValue(expression.SyntaxTree, RuleIdentifiers.UseNamedParameter + ".excluded_methods", out var excludedMethods))
                        {
                            var types = excludedMethods.Split('|');
                            foreach (var type in types)
                            {
                                var declarationId = DocumentationCommentId.CreateDeclarationId(invokedMethodSymbol);
                                if (type == declarationId)
                                    return;
                            }
                        }
                    }
                }

                syntaxContext.ReportDiagnostic(s_rule, syntaxContext.Node);

                static bool IsBooleanExpression(SyntaxNode node) => node.IsKind(SyntaxKind.TrueLiteralExpression) || node.IsKind(SyntaxKind.FalseLiteralExpression);
                static bool IsStringExpression(SyntaxNode node) => node.IsKind(SyntaxKind.StringLiteralExpression) || node.IsKind(SyntaxKind.InterpolatedStringExpression);
            }, SyntaxKind.Argument);
        });
    }

    private static bool IsMethod(ISymbol? method, ITypeSymbol? containingType, string methodName)
    {
        if (containingType == null || method == null || method.ContainingType == null)
            return false;

        if (!string.Equals(methodName, "*", StringComparison.Ordinal) && !string.Equals(method.Name, methodName, StringComparison.Ordinal))
            return false;

        if (!containingType.IsEqualTo(method.ContainingType.OriginalDefinition))
            return false;

        return true;
    }

    internal static int ArgumentIndex(ArgumentSyntax argument)
    {
        var argumentListExpression = argument.FirstAncestorOrSelf<ArgumentListSyntax>();
        if (argumentListExpression == null)
            return -1;

        for (var i = 0; i < argumentListExpression.Arguments.Count; i++)
        {
            if (argumentListExpression.Arguments[i] == argument)
                return i;
        }

        return -1;
    }

    private static ArgumentExpressionKinds GetConfiguration(AnalyzerOptions analyzerOptions, SyntaxNode node)
    {
        var options = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree);
        if (options.TryGetValue(RuleIdentifiers.UseNamedParameter + ".expression_kinds", out var value))
        {
            if (Enum.TryParse<ArgumentExpressionKinds>(value, ignoreCase: true, out var result))
                return result;
        }

        return ArgumentExpressionKinds.Default;
    }

    [Flags]
    private enum ArgumentExpressionKinds
    {
        None = 0,
        Null = 1,
        Boolean = 2,
        Numeric = 4,
        String = 8,
        Default = Null | Boolean,
    }
}

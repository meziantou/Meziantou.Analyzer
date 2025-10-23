using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Meziantou.Analyzer.Configurations;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class NamedParameterAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseNamedParameter,
        title: "Add parameter name to improve readability",
        messageFormat: "Name the parameter to improve code readability",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseNamedParameter));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var callerMustUseNamedArgumentType = context.Compilation.GetBestTypeByMetadataName("Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute");

            var objectType = context.Compilation.GetSpecialType(SpecialType.System_Object);
            var taskTokenType = context.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
            var taskGenericTokenType = context.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");
            var valueTaskTokenType = context.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask");
            var valueTaskGenericTokenType = context.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
            var taskCompletionSourceType = context.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.TaskCompletionSource`1");
            var methodBaseTokenType = context.Compilation.GetBestTypeByMetadataName("System.Reflection.MethodBase");
            var fieldInfoTokenType = context.Compilation.GetBestTypeByMetadataName("System.Reflection.FieldInfo");
            var propertyInfoTokenType = context.Compilation.GetBestTypeByMetadataName("System.Reflection.PropertyInfo");
            var msTestAssertTokenType = context.Compilation.GetBestTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.Assert");
            var nunitAssertTokenType = context.Compilation.GetBestTypeByMetadataName("NUnit.Framework.Assert");
            var xunitAssertTokenType = context.Compilation.GetBestTypeByMetadataName("Xunit.Assert");
            var keyValuePairTokenType = context.Compilation.GetBestTypeByMetadataName("System.Collection.Generic.KeyValuePair`2");
            var propertyBuilderType = context.Compilation.GetBestTypeByMetadataName("Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder`1");
            var syntaxNodeType = context.Compilation.GetBestTypeByMetadataName("Microsoft.CodeAnalysis.SyntaxNode");
            var expressionType = context.Compilation.GetBestTypeByMetadataName("System.Linq.Expressions.Expression");
            var operationUtilities = new OperationUtilities(context.Compilation);

            context.RegisterSyntaxNodeAction(syntaxContext =>
            {
                var argument = (ArgumentSyntax)syntaxContext.Node;
                if (argument.NameColon is not null)
                    return;

                if (argument.Expression is null)
                    return;

                if (callerMustUseNamedArgumentType is not null)
                {
                    if (IsCallerMustUseNamedArgumentAttribute(syntaxContext, argument, callerMustUseNamedArgumentType))
                    {
                        syntaxContext.ReportDiagnostic(Diagnostic.Create(Rule, syntaxContext.Node.GetLocation(), effectiveSeverity: DiagnosticSeverity.Warning, additionalLocations: null, properties: null));
                        return;
                    }

                    static bool IsCallerMustUseNamedArgumentAttribute(SyntaxNodeAnalysisContext context, SyntaxNode argument, INamedTypeSymbol callerMustUseNamedArgumentType)
                    {
                        var operation = context.SemanticModel.GetOperation(argument, context.CancellationToken) as IArgumentOperation;
                        if ((operation?.Parameter) is not null)
                        {
                            var attribute = operation.Parameter.GetAttribute(callerMustUseNamedArgumentType);
                            if (attribute is not null)
                            {
                                var requireNamedArgument = attribute.ConstructorArguments.Length == 0 || attribute.ConstructorArguments[0].Value is true;
                                if (requireNamedArgument)
                                    return true;
                            }
                        }

                        return false;
                    }
                }

                var expression = argument.Expression;
                if (expression.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    if (!MustCheckExpressionKind(syntaxContext, expression, ArgumentExpressionKinds.Null))
                        return;
                }
                else if (expression.IsKind(SyntaxKind.NumericLiteralExpression))
                {
                    if (!MustCheckExpressionKind(syntaxContext, expression, ArgumentExpressionKinds.Numeric))
                        return;
                }
                else if (IsBooleanExpression(expression))
                {
                    if (!MustCheckExpressionKind(syntaxContext, expression, ArgumentExpressionKinds.Boolean))
                        return;
                }
                else if (IsStringExpression(expression))
                {
                    if (!MustCheckExpressionKind(syntaxContext, expression, ArgumentExpressionKinds.String))
                        return;
                }
                else
                {
                    return;
                }

                if (argument.Parent.IsKind(SyntaxKind.TupleExpression))
                    return; // Don't consider tuple


                var operation = syntaxContext.SemanticModel.GetOperation(argument, syntaxContext.CancellationToken) as IArgumentOperation;
                if (operation?.Parameter is not null)
                {
                    var parameterName = operation.Parameter.Name;
                    if (!IsMeaningfulParameterName(parameterName))
                        return;
                }

                // Exclude in some methods such as ConfigureAwait(false)
                var invocationExpression = argument.FirstAncestorOrSelf<ExpressionSyntax>(t => t.IsKind(SyntaxKind.InvocationExpression) || t.IsKind(SyntaxKind.ObjectCreationExpression) || t.IsKind(SyntaxKind.ElementAccessExpression));
                if (invocationExpression is not null)
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

                    var invokedMethodSymbol = syntaxContext.SemanticModel.GetSymbolInfo(invocationExpression, syntaxContext.CancellationToken).Symbol;
                    if (invokedMethodSymbol is null && invocationExpression.IsKind(SyntaxKind.ElementAccessExpression))
                        return; // Skip Array[index]

                    if (invokedMethodSymbol is not null)
                    {
                        var invokedMethodParameters = invokedMethodSymbol switch
                        {
                            IMethodSymbol methodSymbol => methodSymbol.Parameters,
                            IPropertySymbol propertySymbol => propertySymbol.Parameters,
                            _ => ImmutableArray<IParameterSymbol>.Empty,
                        };

                        if (invokedMethodParameters.Length < GetMinimumMethodArgumentsConfiguration(syntaxContext.Options, expression))
                            return;

                        var argumentIndex = NamedParameterAnalyzerCommon.ArgumentIndex(argument);

                        bool IsParams(SyntaxNode node)
                        {
                            if (argumentIndex > invokedMethodParameters.Length - 1)
                                return true;

                            if (invokedMethodParameters.Length == 0)
                                return false;

                            var lastParameter = invokedMethodParameters[^1];
                            if (argumentIndex == invokedMethodParameters.Length - 1 && lastParameter.IsParams)
                            {
                                if (argumentList.Arguments.Count > invokedMethodParameters.Length)
                                    return true;

                                if (expression.IsKind(SyntaxKind.NullLiteralExpression))
                                    return false;

                                var type = syntaxContext.SemanticModel.GetTypeInfo(node, syntaxContext.CancellationToken).ConvertedType;
                                return !type.IsEqualTo(lastParameter.Type);
                            }

                            return false;
                        }

                        if (IsParams(argument))
                            return;

                        if (invokedMethodParameters.Length == 1)
                        {
                            if (invokedMethodSymbol.Name.StartsWith("Is", StringComparison.Ordinal) ||
                                invokedMethodSymbol.Name.StartsWith("Enable", StringComparison.Ordinal) ||
                                invokedMethodSymbol.Name.StartsWith("Add", StringComparison.Ordinal) ||
                                invokedMethodSymbol.Name.StartsWith("Remove", StringComparison.Ordinal) ||
                                invokedMethodSymbol.Name.StartsWith("Contains", StringComparison.Ordinal) ||
                                invokedMethodSymbol.Name.StartsWith("With", StringComparison.Ordinal) ||
                                invokedMethodSymbol.Name == "IndexOf" ||
                                invokedMethodSymbol.Name == "IndexOfAny" ||
                                invokedMethodSymbol.Name == "LastIndexOf" ||
                                invokedMethodSymbol.Name == nameof(Task.ConfigureAwait))
                            {
                                return;
                            }
                        }

                        if (IsMethod(invokedMethodSymbol, objectType, nameof(object.Equals)))
                            return;

                        if (IsMethod(invokedMethodSymbol, objectType, nameof(object.ReferenceEquals)))
                            return;

                        if (IsMethod(invokedMethodSymbol, taskTokenType, nameof(Task.FromResult)))
                            return;

                        if (IsMethod(invokedMethodSymbol, valueTaskTokenType, nameof(Task.FromResult)))
                            return;

                        if (IsMethod(invokedMethodSymbol, taskCompletionSourceType, nameof(TaskCompletionSource<>.SetResult)))
                            return;

                        if (IsMethod(invokedMethodSymbol, taskCompletionSourceType, nameof(TaskCompletionSource<>.TrySetResult)))
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

                        if (IsMethod(invokedMethodSymbol, expressionType, nameof(Expression.Constant)))
                            return;

                        if ((string.Equals(invokedMethodSymbol.Name, "Parse", StringComparison.Ordinal) || string.Equals(invokedMethodSymbol.Name, "TryParse", StringComparison.Ordinal)) && argumentIndex == 0)
                            return;

                        // Indexer with only 1 argument
                        if (invocationExpression is ElementAccessExpressionSyntax && invokedMethodParameters.Length == 1)
                            return;

                        // e.g. SyntaxNode.WithElse
                        if (invokedMethodSymbol.Name.StartsWith("With", StringComparison.Ordinal) && invokedMethodSymbol.ContainingType.IsOrInheritFrom(syntaxNodeType))
                            return;

                        if (operation is not null && operationUtilities.IsInExpressionContext(operation))
                            return;

                        if (syntaxContext.Options.TryGetConfigurationValue(expression.SyntaxTree, RuleIdentifiers.UseNamedParameter + ".excluded_methods_regex", out var excludedMethodsRegex))
                        {
                            var declarationId = DocumentationCommentId.CreateDeclarationId(invokedMethodSymbol);
                            if (declarationId is not null && Regex.IsMatch(declarationId, excludedMethodsRegex, RegexOptions.None, Timeout.InfiniteTimeSpan))
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

                syntaxContext.ReportDiagnostic(Rule, syntaxContext.Node);

                static bool IsBooleanExpression(SyntaxNode node) => node.IsKind(SyntaxKind.TrueLiteralExpression) || node.IsKind(SyntaxKind.FalseLiteralExpression);
                static bool IsStringExpression(SyntaxNode node) => node.IsKind(SyntaxKind.StringLiteralExpression) || node.IsKind(SyntaxKind.InterpolatedStringExpression);
            }, SyntaxKind.Argument);
        });
    }

    private static bool IsMethod(ISymbol? method, ITypeSymbol? containingType, string methodName)
    {
        if (containingType is null || method is null || method.ContainingType is null)
            return false;

        if (!string.Equals(methodName, "*", StringComparison.Ordinal) && !string.Equals(method.Name, methodName, StringComparison.Ordinal))
            return false;

        if (!containingType.IsEqualTo(method.ContainingType.OriginalDefinition))
            return false;

        return true;
    }

    private static int GetMinimumMethodArgumentsConfiguration(AnalyzerOptions analyzerOptions, SyntaxNode node)
    {
        var options = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree);
        if (options.TryGetValue(RuleIdentifiers.UseNamedParameter + ".minimum_method_parameters", out var value))
        {
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
                return result;
        }

        return 1;
    }

    private static ArgumentExpressionKinds GetExpressionKindsConfiguration(AnalyzerOptions analyzerOptions, SyntaxNode node)
    {
        var options = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree);
        if (options.TryGetValue(RuleIdentifiers.UseNamedParameter + ".expression_kinds", out var value))
        {
            if (Enum.TryParse<ArgumentExpressionKinds>(value, ignoreCase: true, out var result))
                return result;
        }

        return ArgumentExpressionKinds.Default;
    }

    private static bool MustCheckExpressionKind(SyntaxNodeAnalysisContext context, SyntaxNode expression, ArgumentExpressionKinds kind)
    {
        var options = GetExpressionKindsConfiguration(context.Options, expression);
        return (options & kind) == kind;
    }

    private static bool IsMeaningfulParameterName(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return false;

        if (parameterName is "obj")
            return false;

        // arg, arg1, arg2, etc. are not meaningful
        if (parameterName.StartsWith("arg", StringComparison.OrdinalIgnoreCase) && IsAllDigit(parameterName.AsSpan(3)))
            return false;

        return true;

        static bool IsAllDigit(ReadOnlySpan<char> span)
        {
            for (var i = 0; i < span.Length; i++)
            {
                if (!char.IsDigit(span[i]))
                    return false;
            }

            return true;
        }
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

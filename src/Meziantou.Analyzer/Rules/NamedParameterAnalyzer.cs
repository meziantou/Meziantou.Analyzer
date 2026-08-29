using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class NamedParameterAnalyzer : DiagnosticAnalyzer
{
    private const ArgumentExpressionKinds DefaultExpressionKinds = ArgumentExpressionKinds.Null | ArgumentExpressionKinds.Boolean;

    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseNamedParameter,
        title: "Add parameter name to improve readability",
        messageFormat: "Name the parameter to improve code readability",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseNamedParameter));

    internal static readonly ConfigurationDefinition<string> ExcludedMethodsRegexConfiguration = new(RuleIdentifiers.UseNamedParameter + ".excluded_methods_regex");
    private static readonly ConfigurationDefinition<string> ExcludedMethodsConfiguration = new(RuleIdentifiers.UseNamedParameter + ".excluded_methods");
    private static readonly ConfigurationDefinition<string> MinimumMethodParametersConfiguration = new(RuleIdentifiers.UseNamedParameter + ".minimum_method_parameters", defaultValue: string.Empty);
    private static readonly ConfigurationDefinition<string> ExpressionKindsConfiguration = new(RuleIdentifiers.UseNamedParameter + ".expression_kinds", defaultValue: string.Empty);
    private static readonly ConfigurationDefinition<bool> IgnoreArgumentsMatchingParameterNameConfiguration = new(RuleIdentifiers.UseNamedParameter + ".ignore_arguments_matching_parameter_name", defaultValue: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var objectType = context.Compilation.GetSpecialType(SpecialType.System_Object);
            var taskTokenType = context.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
            var taskGenericTokenType = context.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");
            var valueTaskTokenType = context.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask");
            var valueTaskGenericTokenType = context.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
            var taskCompletionSourceType = context.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.TaskCompletionSource`1");
            var volatileType = context.Compilation.GetBestTypeByMetadataName("System.Threading.Volatile");
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

                var argumentOperation = syntaxContext.SemanticModel.GetOperation(argument, syntaxContext.CancellationToken) as IArgumentOperation;

                // Naming an argument that already carries the name of the parameter doesn't improve readability
                if (argumentOperation is not null && HasSameNameAsParameter(syntaxContext.Options, argumentOperation))
                    return;

                if (IsCallerMustUseNamedArgumentAttribute(argumentOperation))
                {
                    syntaxContext.ReportDiagnostic(Diagnostic.Create(Rule, syntaxContext.Node.GetLocation(), effectiveSeverity: DiagnosticSeverity.Warning, additionalLocations: null, properties: null));
                    return;
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
                else if (expression.IsKind(SyntaxKind.DefaultLiteralExpression))
                {
                    if (!MustCheckExpressionKind(syntaxContext, expression, ArgumentExpressionKinds.Default))
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


                if (argumentOperation?.Parameter is not null)
                {
                    var parameterName = argumentOperation.Parameter.Name;
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

                        if (IsMethod(invokedMethodSymbol, volatileType, nameof(System.Threading.Volatile.Read)))
                            return;

                        if (IsMethod(invokedMethodSymbol, volatileType, nameof(System.Threading.Volatile.Write)))
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
                        if (invokedMethodSymbol.Name.StartsWith("With", StringComparison.Ordinal) && invokedMethodSymbol.ContainingType.IsOrInheritsFrom(syntaxNodeType))
                            return;

                        if (argumentOperation is not null && !argumentOperation.GetCSharpLanguageVersion().IsCSharp14OrGreater() && operationUtilities.IsInExpressionContext(argumentOperation))
                            return;

                        if (syntaxContext.Options.TryGetConfigurationValue(expression.SyntaxTree, ExcludedMethodsRegexConfiguration, out var excludedMethodsRegex))
                        {
                            var declarationId = DocumentationCommentId.CreateDeclarationId(invokedMethodSymbol);
                            if (declarationId is not null && RegexCache.IsMatch(excludedMethodsRegex, RegexOptions.None, declarationId, defaultValue: false))
                                return;
                        }

                        if (syntaxContext.Options.TryGetConfigurationValue(expression.SyntaxTree, ExcludedMethodsConfiguration, out var excludedMethods))
                        {
                            var declarationId = DocumentationCommentId.CreateDeclarationId(invokedMethodSymbol);
                            if (declarationId is not null)
                            {
                                var types = excludedMethods.Split('|');
                                foreach (var type in types)
                                {
                                    if (type == declarationId)
                                        return;
                                }
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
        var value = analyzerOptions.GetConfigurationValue(node.SyntaxTree, MinimumMethodParametersConfiguration);
        if (!string.IsNullOrEmpty(value))
        {
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
                return result;
        }

        return 1;
    }

    private static ArgumentExpressionKinds GetExpressionKindsConfiguration(AnalyzerOptions analyzerOptions, SyntaxNode node)
    {
        var value = analyzerOptions.GetConfigurationValue(node.SyntaxTree, ExpressionKindsConfiguration);
        if (!string.IsNullOrEmpty(value))
        {
            var result = ArgumentExpressionKinds.None;
            foreach (var rawExpressionKind in value.Split([',', '|'], StringSplitOptions.RemoveEmptyEntries))
            {
                var expressionKind = rawExpressionKind.Trim();
                if (Enum.TryParse<ArgumentExpressionKinds>(expressionKind, ignoreCase: true, out var parsedExpressionKind))
                {
                    result |= parsedExpressionKind;
                    continue;
                }

                return DefaultExpressionKinds;
            }

            return result;
        }

        return DefaultExpressionKinds;
    }

    private static bool MustCheckExpressionKind(SyntaxNodeAnalysisContext context, SyntaxNode expression, ArgumentExpressionKinds kind)
    {
        var options = GetExpressionKindsConfiguration(context.Options, expression);
        return (options & kind) == kind;
    }

    private static bool IsCallerMustUseNamedArgumentAttribute(IArgumentOperation? operation)
    {
        if (operation?.Parameter is not { } parameter)
            return false;

        // The receiver of an extension method cannot be named when the method is called using the instance syntax (value.Method())
        if (IsExtensionMethodReceiver(parameter))
            return false;

        foreach (var attribute in parameter.GetAttributes())
        {
            if (!AnnotationAttributes.IsRequireNamedArgumentAttributeSymbol(attribute.AttributeClass))
                continue;

            var requireNamedArgument = attribute.ConstructorArguments.Length == 0 || attribute.ConstructorArguments[0].Value is true;
            if (requireNamedArgument)
                return true;
        }

        return false;
    }

    private static bool IsExtensionMethodReceiver(IParameterSymbol parameter)
    {
        return parameter is { Ordinal: 0, ContainingSymbol: IMethodSymbol { IsExtensionMethod: true, ReducedFrom: null } };
    }

    private static bool HasSameNameAsParameter(AnalyzerOptions options, IArgumentOperation operation)
    {
        if (operation.Parameter is not { } parameter)
            return false;

        var value = operation.Value.UnwrapImplicitConversions();
        var name = value switch
        {
            ILocalReferenceOperation localReference => localReference.Local.Name,
            IParameterReferenceOperation parameterReference => parameterReference.Parameter.Name,
            IPropertyReferenceOperation propertyReference => propertyReference.Property.Name,
            IFieldReferenceOperation fieldReference => fieldReference.Field.Name,
            _ => null,
        };

        if (name is null)
            return false;

        if (!string.Equals(name, parameter.Name, StringComparison.OrdinalIgnoreCase))
        {
            // Fields are commonly prefixed with '_' or 's_'
            if (value is not IFieldReferenceOperation || !string.Equals(TrimFieldNamePrefix(name), parameter.Name, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return options.GetConfigurationValue(operation.Syntax.SyntaxTree, IgnoreArgumentsMatchingParameterNameConfiguration);
    }

    private static string TrimFieldNamePrefix(string name)
    {
        if (name.StartsWith("s_", StringComparison.Ordinal))
            return name[2..];

        if (name.StartsWith("_", StringComparison.Ordinal))
            return name[1..];

        return name;
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
        Default = 16,
    }
}

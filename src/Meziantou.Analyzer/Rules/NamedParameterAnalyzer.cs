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

    internal static readonly ConfigurationDefinition<string> ExcludedMethodsRegexConfiguration = new(RuleIdentifiers.UseNamedParameter + ".excluded_methods_regex") { RegexOptions = RegexOptions.None };
    private static readonly ConfigurationDefinition<string> ExcludedMethodsConfiguration = new(RuleIdentifiers.UseNamedParameter + ".excluded_methods");
    private static readonly ConfigurationDefinition<string> MinimumMethodParametersConfiguration = new(RuleIdentifiers.UseNamedParameter + ".minimum_method_parameters", defaultValue: string.Empty);
    private static readonly ConfigurationDefinition<string> ExpressionKindsConfiguration = new(RuleIdentifiers.UseNamedParameter + ".expression_kinds", defaultValue: string.Empty);
    private static readonly ConfigurationDefinition<bool> IgnoreArgumentsMatchingParameterNameConfiguration = new(RuleIdentifiers.UseNamedParameter + ".ignore_arguments_matching_parameter_name", defaultValue: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var objectType = context.Compilation.GetSpecialType(SpecialType.System_Object);
            var taskTokenType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskGenericTokenType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var valueTaskTokenType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
            var valueTaskGenericTokenType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
            var taskCompletionSourceType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.TaskCompletionSource`1");
            var volatileType = context.Compilation.GetTypeByMetadataName("System.Threading.Volatile");
            var methodBaseTokenType = context.Compilation.GetTypeByMetadataName("System.Reflection.MethodBase");
            var fieldInfoTokenType = context.Compilation.GetTypeByMetadataName("System.Reflection.FieldInfo");
            var propertyInfoTokenType = context.Compilation.GetTypeByMetadataName("System.Reflection.PropertyInfo");
            var msTestAssertTokenType = context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.Assert");
            var nunitAssertTokenType = context.Compilation.GetTypeByMetadataName("NUnit.Framework.Assert");
            var xunitAssertTokenType = context.Compilation.GetTypeByMetadataName("Xunit.Assert");
            var keyValuePairTokenType = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.KeyValuePair`2");
            var propertyBuilderType = context.Compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder`1");
            var syntaxNodeType = context.Compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.SyntaxNode");
            var expressionType = context.Compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression");
            var operationUtilities = new OperationUtilities(context.Compilation);

            // The attribute can be defined in multiple assemblies, so all the types with that name are considered.
            // When the compilation doesn't contain the attribute, no parameter can be annotated with it,
            // so the arguments don't need to be bound to look for it.
            var hasRequireNamedArgumentAttribute = !context.Compilation.GetTypesByMetadataName("Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute").IsEmpty;

            // The rule needs the argument bound to its parameter. Registering on the operation gives the bound
            // argument directly, whereas calling GetOperation from a syntax node action is much more expensive.
            context.RegisterOperationAction(operationContext =>
            {
                var argumentOperation = (IArgumentOperation)operationContext.Operation;

                // The default value of an optional parameter is an implicit argument whose syntax is the invocation,
                // so there is nothing to name at that location.
                if (argumentOperation.Syntax is not ArgumentSyntax argument)
                    return;

                if (argument.NameColon is not null)
                    return;

                if (argument.Expression is null)
                    return;

                if (argument.Parent.IsKind(SyntaxKind.TupleExpression))
                    return; // Don't consider tuple

                var expression = argument.Expression;
                var expressionKind = GetExpressionKind(expression);

                // The kinds of expression the rule considers are configurable, and the default value excludes the
                // numeric and string literals, which are very common, so the configured kinds are checked before
                // the more expensive work.
                var mustCheckExpressionKind = expressionKind is not ArgumentExpressionKinds.None
                    && MustCheckExpressionKind(operationContext.Options, expression, expressionKind);

                if (!mustCheckExpressionKind && !hasRequireNamedArgumentAttribute)
                    return;

                // Naming an argument that already carries the name of the parameter doesn't improve readability
                if (HasSameNameAsParameter(operationContext.Options, argumentOperation))
                    return;

                if (hasRequireNamedArgumentAttribute && IsCallerMustUseNamedArgumentAttribute(argumentOperation))
                {
                    DiagnosticReporter reporter = operationContext;
                    reporter.ReportDiagnostic(Diagnostic.Create(Rule, argument.GetLocation(), effectiveSeverity: DiagnosticSeverity.Warning, additionalLocations: null, properties: null));
                    return;
                }

                if (!mustCheckExpressionKind)
                    return;

                if (argumentOperation.Parameter is not null)
                {
                    var parameterName = argumentOperation.Parameter.Name;
                    if (!IsMeaningfulParameterName(parameterName))
                        return;
                }

                // Exclude in some methods such as ConfigureAwait(false)
                var invokedMethodSymbol = GetInvokedSymbol(argumentOperation);
                if (invokedMethodSymbol is not null)
                {
                    var invokedMethodParameters = invokedMethodSymbol switch
                    {
                        IMethodSymbol methodSymbol => methodSymbol.Parameters,
                        IPropertySymbol propertySymbol => propertySymbol.Parameters,
                        _ => ImmutableArray<IParameterSymbol>.Empty,
                    };

                    if (invokedMethodParameters.Length < GetMinimumMethodArgumentsConfiguration(operationContext.Options, expression))
                        return;

                    // The argument list is an ArgumentListSyntax for the invocations and a BracketedArgumentListSyntax for the indexers
                    var argumentList = argument.Parent as BaseArgumentListSyntax;
                    var argumentIndex = argumentList?.Arguments.IndexOf(argument) ?? -1;

                    bool IsParams(SyntaxNode node)
                    {
                        if (argumentIndex > invokedMethodParameters.Length - 1)
                            return true;

                        if (invokedMethodParameters.Length == 0)
                            return false;

                        var lastParameter = invokedMethodParameters[^1];
                        if (argumentIndex == invokedMethodParameters.Length - 1 && lastParameter.IsParams)
                        {
                            if (argumentList is not null && argumentList.Arguments.Count > invokedMethodParameters.Length)
                                return true;

                            if (expression.IsKind(SyntaxKind.NullLiteralExpression))
                                return false;

                            var type = argumentOperation.SemanticModel!.GetTypeInfo(node, operationContext.CancellationToken).ConvertedType;
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

                    // new KeyValuePair<TKey, TValue>(key, value)
                    if (IsMethod(invokedMethodSymbol, keyValuePairTokenType, WellKnownMemberNames.InstanceConstructorName))
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
                    if (invokedMethodSymbol is IPropertySymbol && invokedMethodParameters.Length == 1)
                        return;

                    // e.g. SyntaxNode.WithElse
                    if (invokedMethodSymbol.Name.StartsWith("With", StringComparison.Ordinal) && invokedMethodSymbol.ContainingType.IsOrInheritsFrom(syntaxNodeType))
                        return;

                    if (!argumentOperation.GetCSharpLanguageVersion().IsCSharp14OrGreater() && operationUtilities.IsInExpressionContext(argumentOperation))
                        return;

                    // Building the declaration id of a method is expensive, so it is only built once for
                    // the two options that use it, and only when one of them is configured.
                    operationContext.Options.TryGetConfigurationRegex(expression.SyntaxTree, ExcludedMethodsRegexConfiguration, out var excludedMethodsRegex);
                    operationContext.Options.TryGetConfigurationValue(expression.SyntaxTree, ExcludedMethodsConfiguration, out var excludedMethods);

                    string? declarationId = null;
                    if (excludedMethodsRegex is not null || excludedMethods is not null)
                    {
                        declarationId = DocumentationCommentId.CreateDeclarationId(invokedMethodSymbol);
                    }

                    if (excludedMethodsRegex is not null)
                    {
                        if (declarationId is not null && RegexCache.IsMatch(excludedMethodsRegex, declarationId, defaultValue: false))
                            return;
                    }

                    if (excludedMethods is not null)
                    {
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

                operationContext.ReportDiagnostic(Rule, argument);
            }, OperationKind.Argument);
        });
    }

    private static ArgumentExpressionKinds GetExpressionKind(ExpressionSyntax expression) => expression.Kind() switch
    {
        SyntaxKind.NullLiteralExpression => ArgumentExpressionKinds.Null,
        SyntaxKind.NumericLiteralExpression => ArgumentExpressionKinds.Numeric,
        SyntaxKind.DefaultLiteralExpression => ArgumentExpressionKinds.Default,
        SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression => ArgumentExpressionKinds.Boolean,
        SyntaxKind.StringLiteralExpression or SyntaxKind.InterpolatedStringExpression => ArgumentExpressionKinds.String,
        _ => ArgumentExpressionKinds.None,
    };

    private static ISymbol? GetInvokedSymbol(IArgumentOperation argument)
    {
        return argument.Parent switch
        {
            IInvocationOperation invocation => GetInvokedMethod(invocation),
            IObjectCreationOperation objectCreation => objectCreation.Constructor,
            IPropertyReferenceOperation propertyReference => propertyReference.Property,
            _ => null,
        };

        // An extension method called as an instance method (value.Method()) is bound to the method that takes the receiver
        // as its first parameter, whereas the arguments written in the code match the parameters of the reduced method
        static IMethodSymbol GetInvokedMethod(IInvocationOperation invocation)
        {
            var method = invocation.TargetMethod;
            if (method is { IsExtensionMethod: true, ReducedFrom: null })
            {
                foreach (var argument in invocation.Arguments)
                {
                    if (argument.Parameter?.Ordinal is 0)
                    {
                        if (argument.Syntax is not ArgumentSyntax && argument.Value.Type is { } receiverType)
                            return method.ReduceExtensionMethod(receiverType) ?? method;

                        break;
                    }
                }
            }

            return method;
        }
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

    private static bool MustCheckExpressionKind(AnalyzerOptions analyzerOptions, SyntaxNode expression, ArgumentExpressionKinds kind)
    {
        var options = GetExpressionKindsConfiguration(analyzerOptions, expression);
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

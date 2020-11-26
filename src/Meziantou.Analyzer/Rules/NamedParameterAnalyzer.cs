using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
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

                    var kind = argument.Expression.Kind();
                    if (kind == SyntaxKind.TrueLiteralExpression ||
                        kind == SyntaxKind.FalseLiteralExpression ||
                        kind == SyntaxKind.NullLiteralExpression)
                    {
                        // Exclude in some methods such as ConfigureAwait(false)
                        var invocationExpression = argument.FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression != null)
                        {
                            var methodSymbol = (IMethodSymbol?)syntaxContext.SemanticModel.GetSymbolInfo(invocationExpression).Symbol;
                            if (methodSymbol != null)
                            {
                                var argumentIndex = ArgumentIndex(argument);

                                if (methodSymbol.Parameters.Length == 1 && methodSymbol.Name.StartsWith("Is", StringComparison.Ordinal))
                                    return;

                                if (methodSymbol.Parameters.Length == 1 && methodSymbol.Name.StartsWith("Enable", StringComparison.Ordinal))
                                    return;

                                if (methodSymbol.Parameters.Length == 1 && methodSymbol.Name == nameof(Task.ConfigureAwait))
                                    return;

                                if (IsMethod(methodSymbol, objectType, nameof(object.Equals)))
                                    return;

                                if (IsMethod(methodSymbol, objectType, nameof(object.ReferenceEquals)))
                                    return;

                                if (IsMethod(methodSymbol, taskTokenType, nameof(Task.FromResult)))
                                    return;

                                if (IsMethod(methodSymbol, taskCompletionSourceType, nameof(TaskCompletionSource<object>.SetResult)))
                                    return;

                                if (IsMethod(methodSymbol, taskCompletionSourceType, nameof(TaskCompletionSource<object>.TrySetResult)))
                                    return;

                                if (IsMethod(methodSymbol, methodBaseTokenType, nameof(MethodBase.Invoke)) && argumentIndex == 0)
                                    return;

                                if (IsMethod(methodSymbol, fieldInfoTokenType, nameof(FieldInfo.SetValue)) && argumentIndex == 0)
                                    return;

                                if (IsMethod(methodSymbol, fieldInfoTokenType, nameof(FieldInfo.GetValue)) && argumentIndex == 0)
                                    return;

                                if (IsMethod(methodSymbol, propertyInfoTokenType, nameof(PropertyInfo.SetValue)) && argumentIndex == 0)
                                    return;

                                if (IsMethod(methodSymbol, propertyInfoTokenType, nameof(PropertyInfo.GetValue)) && argumentIndex == 0)
                                    return;

                                if (IsMethod(methodSymbol, msTestAssertTokenType, "*"))
                                    return;

                                if (IsMethod(methodSymbol, nunitAssertTokenType, "*"))
                                    return;

                                if (IsMethod(methodSymbol, xunitAssertTokenType, "*"))
                                    return;

                                if ((string.Equals(methodSymbol.Name, "Parse", StringComparison.Ordinal) || string.Equals(methodSymbol.Name, "TryParse", StringComparison.Ordinal)) && argumentIndex == 0)
                                    return;

                                // e.g. SyntaxNode.WithElse
                                if (methodSymbol.Name.StartsWith("With", StringComparison.Ordinal) && methodSymbol.ContainingType.IsOrInheritFrom(syntaxNodeType))
                                    return;

                                var operation = syntaxContext.SemanticModel.GetOperation(argument, syntaxContext.CancellationToken);
                                if (operation != null && operation.IsInExpressionArgument())
                                    return;
                            }
                        }

                        syntaxContext.ReportDiagnostic(s_rule, syntaxContext.Node);
                    }
                }, SyntaxKind.Argument);
            });
        }

        private static bool IsMethod(IMethodSymbol? method, ITypeSymbol? type, string name)
        {
            if (type == null || method == null || method.ContainingType == null)
                return false;

            if (!string.Equals(name, "*", StringComparison.Ordinal) && !string.Equals(method.Name, name, StringComparison.Ordinal))
                return false;

            if (!type.IsEqualTo(method.ContainingType.OriginalDefinition))
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
    }
}

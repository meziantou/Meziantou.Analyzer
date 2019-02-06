﻿using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NamedParameterAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.UseNamedParameter,
            title: "Name parameter",
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
                var methodBaseTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Reflection.MethodBase");
                var fieldInfoTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Reflection.FieldInfo");
                var propertyInfoTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Reflection.PropertyInfo");
                var msTestAssertTokenType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.Assert");
                var nunitAssertTokenType = compilationContext.Compilation.GetTypeByMetadataName("NUnit.Framework.Assert");
                var xunitAssertTokenType = compilationContext.Compilation.GetTypeByMetadataName("Xunit.Assert");
                var keyValuePairTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Collection.Generic.KeyValuePair`2");

                compilationContext.RegisterSyntaxNodeAction(symbolContext =>
                {
                    var argument = (ArgumentSyntax)symbolContext.Node;
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
                            var methodSymbol = (IMethodSymbol)symbolContext.SemanticModel.GetSymbolInfo(invocationExpression).Symbol;
                            if (methodSymbol != null)
                            {
                                var argumentIndex = ArgumentIndex(argument);

                                if (MustSkip(symbolContext, attributeTokenType, methodSymbol))
                                    return;

                                if (IsMethod(methodSymbol, objectType, nameof(object.Equals)))
                                    return;

                                if (IsMethod(methodSymbol, objectType, nameof(object.ReferenceEquals)))
                                    return;

                                if (IsMethod(methodSymbol, taskTokenType, nameof(Task.ConfigureAwait)))
                                    return;

                                if (IsMethod(methodSymbol, taskGenericTokenType, nameof(Task.ConfigureAwait)))
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
                            }
                        }

                        symbolContext.ReportDiagnostic(Diagnostic.Create(s_rule, symbolContext.Node.GetLocation()));
                    }
                }, SyntaxKind.Argument);
            });
        }

        private static bool MustSkip(SyntaxNodeAnalysisContext context, ITypeSymbol attributeType, IMethodSymbol methodSymbol)
        {
            if (attributeType == null)
                return false;

            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                var root = syntaxTree.GetRoot();
                if (root == null)
                    continue;

                foreach (var list in root.DescendantNodesAndSelf().OfType<AttributeListSyntax>())
                {
                    if (list.Target != null && list.Target?.Identifier.IsKind(SyntaxKind.AssemblyKeyword) == true)
                    {
                        foreach (var attribute in list.Attributes)
                        {
                            if (attribute.ArgumentList?.Arguments.Count != 2)
                                continue;

                            var attr = context.SemanticModel.GetSymbolInfo(attribute).Symbol;
                            if (attr != null && attributeType.Equals(attr.ContainingType))
                            {
                                var a = GetStringValue(attribute.ArgumentList.Arguments[0]);
                                var b = GetStringValue(attribute.ArgumentList.Arguments[1]);

                                var type = context.Compilation.GetTypeByMetadataName(a);
                                return IsMethod(methodSymbol, type, b);
                            }
                        }
                    }
                }

                var model = context.Compilation.GetSemanticModel(syntaxTree);
            }

            return false;

            string GetStringValue(AttributeArgumentSyntax argument)
            {
                var expression = argument.Expression;
                if (expression.IsKind(SyntaxKind.StringLiteralExpression))
                    return ((LiteralExpressionSyntax)expression).Token.ValueText;

                return null;
            }
        }

        private static bool IsMethod(IMethodSymbol method, ITypeSymbol type, string name)
        {
            if (type == null || method == null || method.ContainingType == null)
                return false;

            if (!string.Equals(name, "*", StringComparison.Ordinal) && !string.Equals(method.Name, name, StringComparison.Ordinal))
                return false;

            if (!type.Equals(method.ContainingType.OriginalDefinition))
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

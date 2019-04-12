using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ArgumentExceptionShouldSpecifyArgumentNameAnalyzer : DiagnosticAnalyzer
    {
        internal const string ArgumentNameKey = "ArgumentName";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.ArgumentExceptionShouldSpecifyArgumentName,
            title: "Specify the parameter name",
            messageFormat: "{0}",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ArgumentExceptionShouldSpecifyArgumentName));

        private static readonly DiagnosticDescriptor s_nameofRule = new DiagnosticDescriptor(
            RuleIdentifiers.UseNameofOperator,
            title: "Use nameof operator",
            messageFormat: "Use nameof operator",
            RuleCategories.Usage,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseNameofOperator));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule, s_nameofRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(Analyze, OperationKind.ObjectCreation);
        }

        private static void Analyze(OperationAnalysisContext context)
        {
            var op = (IObjectCreationOperation)context.Operation;
            if (op == null)
                return;

            var type = op.Type;
            if (type == null)
                return;

            var exceptionType = context.Compilation.GetTypeByMetadataName("System.ArgumentException");
            if (exceptionType == null)
                return;

            if (!type.IsEqualsTo(exceptionType) && !type.InheritsFrom(exceptionType))
                return;

            var parameterName = "paramName";
            if (type.IsEqualsTo(context.Compilation.GetTypeByMetadataName("System.ComponentModel.InvalidEnumArgumentException")))
            {
                parameterName = "argumentName";
            }

            foreach (var argument in op.Arguments)
            {
                if (!string.Equals(argument.Parameter.Name, parameterName, StringComparison.Ordinal))
                    continue;

                if (argument.Value.ConstantValue.HasValue)
                {
                    if (argument.Value.ConstantValue.Value is string value)
                    {
                        var parameterNames = GetParameterNames(op);
                        if (parameterNames.Contains(value, StringComparer.Ordinal))
                        {
                            if (!(argument.Value is INameOfOperation))
                            {
                                var properties = ImmutableDictionary<string, string>.Empty.Add(ArgumentNameKey, value);
                                context.ReportDiagnostic(s_nameofRule, argument.Value, properties);
                            }

                            return;
                        }

                        context.ReportDiagnostic(s_rule, op, $"'{value}' is not a valid parameter name");
                        return;
                    }
                    else
                    {
                        // Cannot determine the value of the argument
                        return;
                    }
                }
                else
                {
                    // Cannot determine the value of the argument
                    return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(s_rule, op.Syntax.GetLocation(), $"Use an overload of '{type.ToDisplayString()}' with the parameter name"));
        }

        private static IEnumerable<string> GetParameterNames(IOperation operation)
        {
            var semanticModel = operation.SemanticModel;
            var node = operation.Syntax;
            while (node != null)
            {
                if (node is AccessorDeclarationSyntax accessor)
                {
                    if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                    {
                        yield return "value";
                    }
                }
                else if (node is PropertyDeclarationSyntax)
                {
                    yield break;
                }
                else if (node is IndexerDeclarationSyntax indexerDeclarationSyntax)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(indexerDeclarationSyntax);
                    foreach (var parameter in symbol.Parameters)
                        yield return parameter.Name;

                    yield break;
                }
                else if (node is MethodDeclarationSyntax methodDeclaration)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
                    foreach (var parameter in symbol.Parameters)
                        yield return parameter.Name;

                    yield break;
                }
                else if (node is LocalFunctionStatementSyntax localFunctionStatement)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(localFunctionStatement) as IMethodSymbol;
                    foreach (var parameter in symbol.Parameters)
                        yield return parameter.Name;
                }
                else if (node is ConstructorDeclarationSyntax constructorDeclaration)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(constructorDeclaration);
                    foreach (var parameter in symbol.Parameters)
                        yield return parameter.Name;

                    yield break;
                }

                node = node.Parent;
            }
        }
    }
}

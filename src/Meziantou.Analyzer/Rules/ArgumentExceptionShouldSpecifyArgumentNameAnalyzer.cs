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
        private static readonly IEnumerable<string> s_setterArgumentNames = new[] { "value" };

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.ArgumentExceptionShouldSpecifyArgumentName,
            title: "Should specify the parameter name",
            messageFormat: "{0}",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ArgumentExceptionShouldSpecifyArgumentName));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(Analyze, OperationKind.ObjectCreation);
        }

        private void Analyze(OperationAnalysisContext context)
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
            if(type.IsEqualsTo(context.Compilation.GetTypeByMetadataName("System.ComponentModel.InvalidEnumArgumentException")))
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
                            return;

                        context.ReportDiagnostic(Diagnostic.Create(s_rule, op.Syntax.GetLocation(), $"'{value}' is not a valid parameter name"));
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
                        return s_setterArgumentNames;

                    return Enumerable.Empty<string>();
                }
                else if (node is MethodDeclarationSyntax methodDeclaration)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
                    return symbol.Parameters.Select(p => p.Name);
                }
                else if (node is ConstructorDeclarationSyntax constructorDeclaration)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(constructorDeclaration);
                    return symbol.Parameters.Select(p => p.Name);
                }

                node = node.Parent;
            }

            return Enumerable.Empty<string>();
        }
    }
}

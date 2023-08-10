﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ArgumentExceptionShouldSpecifyArgumentNameAnalyzer : DiagnosticAnalyzer
{
    internal const string ArgumentNameKey = "ArgumentName";

    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.ArgumentExceptionShouldSpecifyArgumentName,
        title: "Specify the parameter name in ArgumentException",
        messageFormat: "{0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ArgumentExceptionShouldSpecifyArgumentName));

    private static readonly DiagnosticDescriptor s_nameofRule = new(
        RuleIdentifiers.UseNameofOperator,
        title: "Use nameof operator in ArgumentException",
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

        var exceptionType = context.Compilation.GetBestTypeByMetadataName("System.ArgumentException");
        if (exceptionType == null)
            return;

        if (!type.IsOrInheritFrom(exceptionType))
            return;

        var parameterName = "paramName";
        if (type.IsEqualTo(context.Compilation.GetBestTypeByMetadataName("System.ComponentModel.InvalidEnumArgumentException")))
        {
            parameterName = "argumentName";
        }

        foreach (var argument in op.Arguments)
        {
            if (argument.Parameter == null || !string.Equals(argument.Parameter.Name, parameterName, StringComparison.Ordinal))
                continue;

            if (argument.Value.ConstantValue.HasValue)
            {
                if (argument.Value.ConstantValue.Value is string value)
                {
                    var parameterNames = GetParameterNames(op, context.CancellationToken);
                    if (parameterNames.Contains(value, StringComparer.Ordinal))
                    {
                        if (argument.Value is not INameOfOperation)
                        {
                            var properties = ImmutableDictionary<string, string?>.Empty.Add(ArgumentNameKey, value);
                            context.ReportDiagnostic(s_nameofRule, properties, argument.Value);
                        }

                        return;
                    }

                    if (argument.Syntax is ArgumentSyntax argumentSyntax)
                    {
                        context.ReportDiagnostic(s_rule, argumentSyntax.Expression, $"'{value}' is not a valid parameter name");
                    }
                    else
                    {
                        context.ReportDiagnostic(s_rule, argument, $"'{value}' is not a valid parameter name");
                    }

                    return;
                }
            }

            // Cannot determine the value of the argument
            return;
        }

        var ctors = type.GetMembers(".ctor").OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Constructor);
        foreach (var ctor in ctors)
        {
            if (ctor.Parameters.Any(p => p.Name is "paramName" or "argumentName" && p.Type.IsString()))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, op.Syntax.GetLocation(), $"Use an overload of '{type.ToDisplayString()}' with the parameter name"));
                return;
            }
        }
    }

    private static IEnumerable<string> GetParameterNames(IOperation operation, CancellationToken cancellationToken)
    {
        var symbols = operation.LookupAvailableSymbols(cancellationToken);
        foreach (var symbol in symbols)
        {
            switch (symbol)
            {
                case IParameterSymbol parameterSymbol:
                    yield return parameterSymbol.Name;
                    break;
            }
        }
    }
}

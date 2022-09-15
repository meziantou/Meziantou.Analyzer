﻿using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LocalVariablesShouldNotHideSymbolsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.LocalVariablesShouldNotHideSymbols,
        title: "Local variable should not hide other symbols",
        messageFormat: "Local variable '{0}' should not hide {1}",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.LocalVariablesShouldNotHideSymbols));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeVariableDeclaration, OperationKind.VariableDeclarator);
    }

    private static void AnalyzeVariableDeclaration(OperationAnalysisContext context)
    {
        var operation = (IVariableDeclaratorOperation)context.Operation;
        var semanticModel = operation.SemanticModel!;
        var localSymbol = operation.Symbol;
        if (localSymbol.IsImplicitlyDeclared || !localSymbol.CanBeReferencedByName)
            return;

        var containingType = localSymbol.ContainingType;
        if (containingType == null)
            return;

        foreach (var member in GetSymbols(containingType, localSymbol.Name))
        {
            if (!semanticModel.IsAccessible(operation.Syntax.SpanStart, member))
                continue;

            if (member is IFieldSymbol)
            {
                ReportDiagnostic("field");
                return;
            }

            if (member is IPropertySymbol)
            {
                ReportDiagnostic("property");
                return;
            }
        }

        void ReportDiagnostic(string type)
        {
            if (operation.Syntax is VariableDeclaratorSyntax declarator)
            {
                context.ReportDiagnostic(s_rule, declarator.Identifier, localSymbol.Name, type);
            }
            else
            {
                context.ReportDiagnostic(s_rule, operation, localSymbol.Name, type);
            }
        }
    }

    private static IEnumerable<ISymbol> GetSymbols(INamedTypeSymbol? type, string name)
    {
        while (type != null)
        {
            var members = type.GetMembers(name);
            foreach (var member in members)
            {
                yield return member;
            }

            type = type.BaseType;
        }
    }
}

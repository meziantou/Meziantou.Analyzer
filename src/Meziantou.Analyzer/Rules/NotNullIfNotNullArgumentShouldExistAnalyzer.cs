﻿using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NotNullIfNotNullArgumentShouldExistAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.NotNullIfNotNullArgumentShouldExist,
            title: "Invalid parameter name",
            messageFormat: "Parameter '{0}' does not exist",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.NotNullIfNotNullArgumentShouldExist));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(ctx =>
            {
                var type = ctx.Compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute");
                if (type == null)
                    return;

                ctx.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, type), SymbolKind.Method);
            });
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol notNullIfNotNullAttributeTypeSymbol)
        {
            var method = (IMethodSymbol)context.Symbol;
            foreach (var attribute in method.GetAttributes())
            {
                if (!attribute.AttributeClass.IsEqualTo(notNullIfNotNullAttributeTypeSymbol))
                    continue;

                if (attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is string parameterName)
                {
                    if (!method.Parameters.Any(p => p.Name == parameterName))
                    {
                        var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
                        if (location != null)
                        {
                            context.ReportDiagnostic(s_rule, location, parameterName);
                        }
                        else
                        {
                            context.ReportDiagnostic(s_rule, method, parameterName);
                        }
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

// https://learn.microsoft.com/en-us/dotnet/api/system.windows.markup.constructorargumentattribute?view=netcore-3.1
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConstructorArgumentParametersShouldExistInConstructorsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ConstructorArgumentParametersShouldExistInConstructors,
        title: "ConstructorArgument parameters should exist in constructors",
        messageFormat: "No constructor found with a parameter named '{0}'",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ConstructorArgumentParametersShouldExistInConstructors));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.IsValid)
            {
                ctx.RegisterSymbolAction(analyzerContext.AnalyzeProperty, SymbolKind.Property);
            }
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        public ISymbol? ConstructorArgumentSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Windows.Markup.ConstructorArgumentAttribute");

        public bool IsValid => ConstructorArgumentSymbol is not null;

        public void AnalyzeProperty(SymbolAnalysisContext context)
        {
            var property = (IPropertySymbol)context.Symbol;
            foreach (var attribute in property.GetAttributes())
            {
                if (!attribute.AttributeClass.IsEqualTo(ConstructorArgumentSymbol))
                    continue;

                if (attribute.ConstructorArguments.Length == 0)
                    continue;

                if (attribute.ConstructorArguments[0].Value is not string name)
                    continue;

                if (HasConstructorMatchingAttribute(property.ContainingType, name))
                    continue;

                if (attribute.ApplicationSyntaxReference is not null)
                {
                    context.ReportDiagnostic(Rule, attribute.ApplicationSyntaxReference, name);
                }
                else
                {
                    context.ReportDiagnostic(Rule, property, name);
                }
            }
        }

        private static bool HasConstructorMatchingAttribute(INamedTypeSymbol type, string expectedParameterName)
        {
            if (string.IsNullOrEmpty(expectedParameterName))
                return false;

            foreach (var constructor in type.InstanceConstructors)
            {
                if (constructor.Parameters.Length != 1)
                    continue;

                var constructorParameterName = constructor.Parameters[0].Name;
                if (string.Equals(constructorParameterName, expectedParameterName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}

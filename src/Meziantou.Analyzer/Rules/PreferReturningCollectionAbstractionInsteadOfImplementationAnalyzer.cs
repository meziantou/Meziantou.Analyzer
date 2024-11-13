using System.Collections.Generic;
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferReturningCollectionAbstractionInsteadOfImplementationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.PreferReturningCollectionAbstractionInsteadOfImplementation,
        title: "Prefer using collection abstraction instead of implementation",
        messageFormat: "Prefer using collection abstraction instead of implementation",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.PreferReturningCollectionAbstractionInsteadOfImplementation));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);

            ctx.RegisterSymbolAction(c => analyzerContext.AnalyzeSymbol(c), SymbolKind.Method, SymbolKind.Field, SymbolKind.Property, SymbolKind.NamedType);
        });
    }

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            ConcreteCollectionSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Collections.Generic.List`1"));
            ConcreteCollectionSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Collections.Generic.HashSet`1"));
            ConcreteCollectionSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Collections.Generic.Dictionary`2"));
            ConcreteCollectionSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Collections.ObjectModel.Collection`1"));

            TaskSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1"));
            TaskSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask`1"));

            XmlIgnoreAttributeSymbol = compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlIgnoreAttribute");

            XmlClassAttributeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlTypeAttribute"));
            XmlClassAttributeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlRootAttribute"));

            XmlPropertyAttributeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlElementAttribute"));
            XmlPropertyAttributeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlArrayAttribute"));
            XmlPropertyAttributeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlAnyAttributeAttribute"));
            XmlPropertyAttributeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlAnyElementAttribute"));
            XmlPropertyAttributeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlArrayItemAttribute"));
            XmlPropertyAttributeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Xml.Serialization.XmlTextAttribute"));
        }

        public List<ITypeSymbol> ConcreteCollectionSymbols { get; } = [];
        public List<ITypeSymbol> TaskSymbols { get; } = [];

        public ITypeSymbol? XmlIgnoreAttributeSymbol { get; set; }
        public List<ITypeSymbol> XmlClassAttributeSymbols { get; } = [];
        public List<ITypeSymbol> XmlPropertyAttributeSymbols { get; } = [];

        private bool IsValidType(ITypeSymbol? symbol)
        {
            if (symbol is null)
                return true;

            var originalDefinition = symbol.OriginalDefinition;
            if (ConcreteCollectionSymbols.Exists(t => t.IsEqualTo(originalDefinition)))
                return false;

            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (TaskSymbols.Exists(t => t.IsEqualTo(symbol.OriginalDefinition)))
                {
                    return IsValidType(namedTypeSymbol.TypeArguments[0]);
                }
            }

            return true;
        }

        private bool IsXmlSerializableProperty(IPropertySymbol property)
        {
            if (property.HasAttribute(XmlIgnoreAttributeSymbol))
                return false;

            foreach (var attribute in XmlPropertyAttributeSymbols)
            {
                if (property.HasAttribute(attribute))
                    return true;
            }

            foreach (var attribute in XmlClassAttributeSymbols)
            {
                if (property.ContainingType.HasAttribute(attribute))
                    return true;
            }

            return false;
        }

        public void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            if (!context.Symbol.IsVisibleOutsideOfAssembly())
                return;

            if (context.Symbol.IsOverrideOrInterfaceImplementation())
                return;

            switch (context.Symbol)
            {
                case INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: not null and var methodSymbol }:
                    if (!IsValidType(methodSymbol.ReturnType))
                    {
                        context.ReportDiagnostic(Rule, methodSymbol, DiagnosticMethodReportOptions.ReportOnReturnType);
                    }

                    AnalyzeParameter(context, methodSymbol.Parameters);
                    break;

                case IFieldSymbol fieldSymbol:
                    if (!IsValidType(fieldSymbol.Type))
                    {
                        context.ReportDiagnostic(Rule, fieldSymbol, DiagnosticFieldReportOptions.ReportOnReturnType);
                    }

                    break;

                case IPropertySymbol propertySymbol:
                    if (IsXmlSerializableProperty(propertySymbol))
                        break;

                    if (!IsValidType(propertySymbol.Type))
                    {
                        context.ReportDiagnostic(Rule, propertySymbol, DiagnosticPropertyReportOptions.ReportOnReturnType);
                    }

                    AnalyzeParameter(context, propertySymbol.Parameters);
                    break;

                case IMethodSymbol methodSymbol when methodSymbol.MethodKind is not MethodKind.PropertyGet and not MethodKind.PropertySet:
                    if (!IsValidType(methodSymbol.ReturnType))
                    {
                        context.ReportDiagnostic(Rule, methodSymbol, DiagnosticMethodReportOptions.ReportOnReturnType);
                    }

                    AnalyzeParameter(context, methodSymbol.Parameters);
                    break;
            }

            void AnalyzeParameter(SymbolAnalysisContext context, ImmutableArray<IParameterSymbol> parameters)
            {
                foreach (var parameter in parameters)
                {
                    if (!IsValidType(parameter.Type))
                    {
                        context.ReportDiagnostic(Rule, parameter, DiagnosticParameterReportOptions.ReportOnType);
                    }
                }
            }
        }
    }
}

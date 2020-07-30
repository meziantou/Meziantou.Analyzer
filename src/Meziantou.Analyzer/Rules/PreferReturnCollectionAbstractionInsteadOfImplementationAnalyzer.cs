using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PreferReturnCollectionAbstractionInsteadOfImplementationAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.PreferReturnCollectionAbstractionInsteadOfImplementation,
            title: "Prefer return collection abstraction instead of implementation",
            messageFormat: "Prefer return collection abstraction instead of implementation",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.PreferReturnCollectionAbstractionInsteadOfImplementation));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var analyzerContext = new AnalyzerContext(ctx.Compilation);

                ctx.RegisterSyntaxNodeAction(c => analyzerContext.AnalyzeDelegate(c), SyntaxKind.DelegateDeclaration);
                ctx.RegisterSyntaxNodeAction(c => analyzerContext.AnalyzeField(c), SyntaxKind.FieldDeclaration);
                ctx.RegisterSyntaxNodeAction(c => analyzerContext.AnalyzeIndexer(c), SyntaxKind.IndexerDeclaration);
                ctx.RegisterSyntaxNodeAction(c => analyzerContext.AnalyzeMethod(c), SyntaxKind.MethodDeclaration);
                ctx.RegisterSyntaxNodeAction(c => analyzerContext.AnalyzeProperty(c), SyntaxKind.PropertyDeclaration);
            });
        }

        private sealed class AnalyzerContext
        {
            public AnalyzerContext(Compilation compilation)
            {
                ConcreteCollectionSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.List`1"));
                ConcreteCollectionSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1"));
                ConcreteCollectionSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2"));
                ConcreteCollectionSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.ObjectModel`1"));

                TaskSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1"));
                TaskSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1"));

                XmlIgnoreAttributeSymbol = compilation.GetTypeByMetadataName("System.Xml.Serialization.XmlIgnoreAttribute");

                XmlClassAttributeSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Xml.Serialization.XmlTypeAttribute"));
                XmlClassAttributeSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Xml.Serialization.XmlRootAttribute"));

                XmlPropertyAttributeSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Xml.Serialization.XmlElementAttribute"));
                XmlPropertyAttributeSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Xml.Serialization.XmlArrayAttribute"));
                XmlPropertyAttributeSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Xml.Serialization.XmlAnyAttributeAttribute"));
                XmlPropertyAttributeSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Xml.Serialization.XmlAnyElementAttribute"));
                XmlPropertyAttributeSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Xml.Serialization.XmlArrayItemAttribute"));
                XmlPropertyAttributeSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Xml.Serialization.XmlTextAttribute"));
            }

            public List<ITypeSymbol> ConcreteCollectionSymbols { get; } = new List<ITypeSymbol>();
            public List<ITypeSymbol> TaskSymbols { get; } = new List<ITypeSymbol>();

            public ITypeSymbol? XmlIgnoreAttributeSymbol { get; set; }
            public List<ITypeSymbol> XmlClassAttributeSymbols { get; } = new List<ITypeSymbol>();
            public List<ITypeSymbol> XmlPropertyAttributeSymbols { get; } = new List<ITypeSymbol>();

            public void AnalyzeField(SyntaxNodeAnalysisContext context)
            {
                var node = (FieldDeclarationSyntax)context.Node;
                if (node == null || node.Declaration == null)
                    return;

                var firstVariable = node.Declaration.Variables.FirstOrDefault();
                if (firstVariable == null)
                    return;

                var symbol = context.SemanticModel.GetDeclaredSymbol(firstVariable, context.CancellationToken) as IFieldSymbol;
                if (symbol == null)
                    return;

                if (!symbol.IsVisibleOutsideOfAssembly())
                    return;

                if (IsValidType(symbol.Type))
                    return;

                context.ReportDiagnostic(s_rule, node.Declaration.Type);
            }

            public void AnalyzeDelegate(SyntaxNodeAnalysisContext context)
            {
                var node = (DelegateDeclarationSyntax)context.Node;
                if (node == null)
                    return;

                var symbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken);
                if (!symbol.IsVisibleOutsideOfAssembly())
                    return;

                var type = node.ReturnType;
                if (type != null && !IsValidType(context.SemanticModel.GetTypeInfo(type, context.CancellationToken).Type))
                {
                    context.ReportDiagnostic(s_rule, type);
                }

                AnalyzeParameters(context, node.ParameterList?.Parameters);
            }

            public void AnalyzeIndexer(SyntaxNodeAnalysisContext context)
            {
                var node = (IndexerDeclarationSyntax)context.Node;
                if (node == null)
                    return;

                var symbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken);
                if (!symbol.IsVisibleOutsideOfAssembly() || symbol.IsOverrideOrInterfaceImplementation())
                    return;

                var type = node.Type;
                if (type != null && !IsValidType(context.SemanticModel.GetTypeInfo(type, context.CancellationToken).Type))
                {
                    context.ReportDiagnostic(s_rule, type);
                }

                AnalyzeParameters(context, node.ParameterList?.Parameters);
            }

            public void AnalyzeProperty(SyntaxNodeAnalysisContext context)
            {
                var node = (PropertyDeclarationSyntax)context.Node;
                if (node == null)
                    return;

                var symbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken);
                if (!symbol.IsVisibleOutsideOfAssembly() || symbol.IsOverrideOrInterfaceImplementation())
                    return;

                var type = node.Type;
                if (type == null || IsValidType(context.SemanticModel.GetTypeInfo(type, context.CancellationToken).Type))
                    return;

                if (IsXmlSerializableProperty(symbol))
                    return;

                context.ReportDiagnostic(s_rule, type);
            }

            public void AnalyzeMethod(SyntaxNodeAnalysisContext context)
            {
                var node = (MethodDeclarationSyntax)context.Node;
                if (node == null)
                    return;

                var symbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken);
                if (!symbol.IsVisibleOutsideOfAssembly() || symbol.IsOverrideOrInterfaceImplementation())
                    return;

                var type = node.ReturnType;
                if (type != null && !IsValidType(context.SemanticModel.GetTypeInfo(type, context.CancellationToken).Type))
                {
                    context.ReportDiagnostic(s_rule, type);
                }

                AnalyzeParameters(context, node.ParameterList?.Parameters);
            }

            public void AnalyzeParameters(SyntaxNodeAnalysisContext context, IEnumerable<ParameterSyntax>? parameters)
            {
                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        AnalyzeParameter(context, parameter);
                    }
                }
            }

            public void AnalyzeParameter(SyntaxNodeAnalysisContext context, ParameterSyntax parameter)
            {
                var type = parameter.Type;
                if (type != null && !IsValidType(context.SemanticModel.GetTypeInfo(type, context.CancellationToken).Type))
                {
                    context.ReportDiagnostic(s_rule, parameter);
                }
            }

            private bool IsValidType(ITypeSymbol? symbol)
            {
                if (symbol == null)
                    return true;

                var originalDefinition = symbol.OriginalDefinition;
                if (ConcreteCollectionSymbols.Any(t => t.IsEqualTo(originalDefinition)))
                    return false;

                var namedTypeSymbol = symbol as INamedTypeSymbol;
                if (namedTypeSymbol != null)
                {
                    if (TaskSymbols.Any(t => t.IsEqualTo(symbol.OriginalDefinition)))
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
        }
    }
}

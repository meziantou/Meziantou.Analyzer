using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PreferReturnCollectionAbstractionInsteadOfImplementationAnalyzer : DiagnosticAnalyzer
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

            context.RegisterSyntaxNodeAction(AnalyzeDelegate, SyntaxKind.DelegateDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeIndexer, SyntaxKind.IndexerDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
        }

        private void AnalyzeField(SyntaxNodeAnalysisContext context)
        {
            var node = (FieldDeclarationSyntax)context.Node;
            if (node == null)
                return;

            var firstVariable = node.Declaration?.Variables.FirstOrDefault();
            if (firstVariable == null)
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(firstVariable, context.CancellationToken) as IFieldSymbol;
            if (!symbol.IsVisible())
                return;

            if (IsValidType(context.Compilation, symbol.Type))
                return;

            context.ReportDiagnostic(Diagnostic.Create(s_rule, node.GetLocation()));
        }

        private void AnalyzeDelegate(SyntaxNodeAnalysisContext context)
        {
            var node = (DelegateDeclarationSyntax)context.Node;
            if (node == null)
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(node);
            if (!symbol.IsVisible())
                return;

            var type = node.ReturnType;
            if (type != null && !IsValidType(context.Compilation, context.SemanticModel.GetTypeInfo(type).Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, node.GetLocation()));
            }

            AnalyzeParameters(context, node.ParameterList?.Parameters);
        }

        private void AnalyzeIndexer(SyntaxNodeAnalysisContext context)
        {
            var node = (IndexerDeclarationSyntax)context.Node;
            if (node == null)
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(node);
            if (!symbol.IsVisible())
                return;

            var type = node.Type;
            if (type != null && !IsValidType(context.Compilation, context.SemanticModel.GetTypeInfo(type).Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, node.GetLocation()));
            }

            AnalyzeParameters(context, node.ParameterList?.Parameters);
        }

        private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
        {
            var node = (PropertyDeclarationSyntax)context.Node;
            if (node == null)
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(node);
            if (!symbol.IsVisible())
                return;

            var type = node.Type;
            if (type == null || IsValidType(context.Compilation, context.SemanticModel.GetTypeInfo(type).Type))
                return;

            context.ReportDiagnostic(Diagnostic.Create(s_rule, node.GetLocation()));
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var node = (MethodDeclarationSyntax)context.Node;
            if (node == null)
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(node);
            if (!symbol.IsVisible())
                return;

            var type = node.ReturnType;
            if (type != null && !IsValidType(context.Compilation, context.SemanticModel.GetTypeInfo(type).Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, node.GetLocation()));
            }

            AnalyzeParameters(context, node.ParameterList?.Parameters);
        }

        private void AnalyzeParameters(SyntaxNodeAnalysisContext context, IEnumerable<ParameterSyntax> parameters)
        {
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    AnalyzeParameter(context, parameter);
                }
            }
        }

        private void AnalyzeParameter(SyntaxNodeAnalysisContext context, ParameterSyntax parameter)
        {
            var type = parameter.Type;
            if (type != null && !IsValidType(context.Compilation, context.SemanticModel.GetTypeInfo(type).Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, parameter.GetLocation()));
            }
        }

        private bool IsValidType(Compilation compilation, ITypeSymbol symbol)
        {
            if (symbol == null)
                return true;

            var list = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            var hashset = compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1");
            var dictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
            var collection = compilation.GetTypeByMetadataName("System.Collections.ObjectModel`1");
            var readonlyCollection = compilation.GetTypeByMetadataName("System.Collections.ObjectModel`1");

            var originalDefinition = symbol.OriginalDefinition;
            if (originalDefinition.IsEqualsTo(list) ||
                originalDefinition.IsEqualsTo(dictionary) ||
                originalDefinition.IsEqualsTo(hashset) ||
                originalDefinition.IsEqualsTo(collection) ||
                originalDefinition.IsEqualsTo(readonlyCollection))
            {
                return false;
            }

            return true;
        }
    }
}

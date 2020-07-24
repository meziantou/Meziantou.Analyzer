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
                var analyzerContext = AnalyzerContext.Create(ctx.Compilation);

                ctx.RegisterSyntaxNodeAction(c => AnalyzeDelegate(c, analyzerContext), SyntaxKind.DelegateDeclaration);
                ctx.RegisterSyntaxNodeAction(c => AnalyzeField(c, analyzerContext), SyntaxKind.FieldDeclaration);
                ctx.RegisterSyntaxNodeAction(c => AnalyzeIndexer(c, analyzerContext), SyntaxKind.IndexerDeclaration);
                ctx.RegisterSyntaxNodeAction(c => AnalyzeMethod(c, analyzerContext), SyntaxKind.MethodDeclaration);
                ctx.RegisterSyntaxNodeAction(c => AnalyzeProperty(c, analyzerContext), SyntaxKind.PropertyDeclaration);
            });
        }

        private void AnalyzeField(SyntaxNodeAnalysisContext context, AnalyzerContext analyzerContext)
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

            if (IsValidType(analyzerContext, symbol.Type))
                return;

            context.ReportDiagnostic(s_rule, node.Declaration.Type);
        }

        private void AnalyzeDelegate(SyntaxNodeAnalysisContext context, AnalyzerContext analyzerContext)
        {
            var node = (DelegateDeclarationSyntax)context.Node;
            if (node == null)
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken);
            if (!symbol.IsVisibleOutsideOfAssembly())
                return;

            var type = node.ReturnType;
            if (type != null && !IsValidType(analyzerContext, context.SemanticModel.GetTypeInfo(type, context.CancellationToken).Type))
            {
                context.ReportDiagnostic(s_rule, type);
            }

            AnalyzeParameters(context, analyzerContext, node.ParameterList?.Parameters);
        }

        private void AnalyzeIndexer(SyntaxNodeAnalysisContext context, AnalyzerContext analyzerContext)
        {
            var node = (IndexerDeclarationSyntax)context.Node;
            if (node == null)
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken);
            if (!symbol.IsVisibleOutsideOfAssembly() || symbol.IsOverrideOrInterfaceImplementation())
                return;

            var type = node.Type;
            if (type != null && !IsValidType(analyzerContext, context.SemanticModel.GetTypeInfo(type, context.CancellationToken).Type))
            {
                context.ReportDiagnostic(s_rule, type);
            }

            AnalyzeParameters(context, analyzerContext, node.ParameterList?.Parameters);
        }

        private void AnalyzeProperty(SyntaxNodeAnalysisContext context, AnalyzerContext analyzerContext)
        {
            var node = (PropertyDeclarationSyntax)context.Node;
            if (node == null)
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken);
            if (!symbol.IsVisibleOutsideOfAssembly() || symbol.IsOverrideOrInterfaceImplementation())
                return;

            var type = node.Type;
            if (type == null || IsValidType(analyzerContext, context.SemanticModel.GetTypeInfo(type, context.CancellationToken).Type))
                return;

            context.ReportDiagnostic(s_rule, type);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context, AnalyzerContext analyzerContext)
        {
            var node = (MethodDeclarationSyntax)context.Node;
            if (node == null)
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken);
            if (!symbol.IsVisibleOutsideOfAssembly() || symbol.IsOverrideOrInterfaceImplementation())
                return;

            var type = node.ReturnType;
            if (type != null && !IsValidType(analyzerContext, context.SemanticModel.GetTypeInfo(type, context.CancellationToken).Type))
            {
                context.ReportDiagnostic(s_rule, type);
            }

            AnalyzeParameters(context, analyzerContext, node.ParameterList?.Parameters);
        }

        private void AnalyzeParameters(SyntaxNodeAnalysisContext context, AnalyzerContext analyzerContext, IEnumerable<ParameterSyntax>? parameters)
        {
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    AnalyzeParameter(context, analyzerContext, parameter);
                }
            }
        }

        private void AnalyzeParameter(SyntaxNodeAnalysisContext context, AnalyzerContext analyzerContext, ParameterSyntax parameter)
        {
            var type = parameter.Type;
            if (type != null && !IsValidType(analyzerContext, context.SemanticModel.GetTypeInfo(type, context.CancellationToken).Type))
            {
                context.ReportDiagnostic(s_rule, parameter);
            }
        }

        private bool IsValidType(AnalyzerContext analyzerContext, ITypeSymbol? symbol)
        {
            if (symbol == null)
                return true;

            var originalDefinition = symbol.OriginalDefinition;
            if (analyzerContext.ConcreteCollectionSymbols.Any(t => t.IsEqualTo(originalDefinition)))
                return false;

            var namedTypeSymbol = symbol as INamedTypeSymbol;
            if (namedTypeSymbol != null)
            {
                if (analyzerContext.TaskSymbols.Any(t => t.IsEqualTo(symbol.OriginalDefinition)))
                {
                    return IsValidType(analyzerContext, namedTypeSymbol.TypeArguments[0]);
                }
            }

            return true;
        }

        private sealed class AnalyzerContext
        {
            public static AnalyzerContext Create(Compilation compilation)
            {
                var context = new AnalyzerContext();

                context.ConcreteCollectionSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.List`1"));
                context.ConcreteCollectionSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1"));
                context.ConcreteCollectionSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2"));
                context.ConcreteCollectionSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.ObjectModel`1"));

                context.TaskSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1"));
                context.TaskSymbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1"));
                return context;
            }

            public List<ITypeSymbol> ConcreteCollectionSymbols { get; } = new List<ITypeSymbol>();
            public List<ITypeSymbol> TaskSymbols { get; } = new List<ITypeSymbol>();
        }
    }
}

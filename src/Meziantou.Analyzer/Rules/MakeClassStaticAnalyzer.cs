using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeClassStaticAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.MakeClassStaticAnalyzer,
            title: "Make class static",
            messageFormat: "Make class static",
            RuleCategories.Design,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AbstractTypesShouldNotHaveConstructors));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(s =>
            {
                var potentialClasses = new List<(ClassDeclarationSyntax, ITypeSymbol)>();
                var parentClasses = new List<ITypeSymbol>();

                s.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
                s.RegisterCompilationEndAction(context => End());
            });
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            var node = (ClassDeclarationSyntax)context.Node;
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken);
            if (classSymbol == null || classSymbol.IsStatic)
                return;

            if (classSymbol.GetMembers().All(member => member.IsStatic))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, node.Identifier.GetLocation()));
            }
        }
    }
}

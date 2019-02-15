using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotDeclareStaticMembersOnGenericTypes : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotDeclareStaticMembersOnGenericTypes,
            title: "Do not declare static members on generic types",
            messageFormat: "Do not declare static members on generic types",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotDeclareStaticMembersOnGenericTypes));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            var node = (ClassDeclarationSyntax)context.Node;
            if (node == null)
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(node);
            if (symbol == null || !symbol.IsGenericType)
                return;

            foreach (var member in symbol.GetMembers())
            {
                if (member.IsStatic)
                {
                    // skip properties
                    if (member is IMethodSymbol method && (method.MethodKind == MethodKind.PropertyGet || method.MethodKind == MethodKind.PropertySet))
                        continue;

                    var syntax = member.DeclaringSyntaxReferences.FirstOrDefault();
                    if (syntax != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_rule, Location.Create(syntax.SyntaxTree, syntax.Span)));
                    }
                }
            }
        }
    }
}

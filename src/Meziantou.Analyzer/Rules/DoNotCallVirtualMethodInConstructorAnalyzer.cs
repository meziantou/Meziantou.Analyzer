using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotCallVirtualMethodInConstructorAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotCallVirtualMethodInConstructor,
            title: "Do not call virtual method in constructor",
            messageFormat: "Do not call virtual method in constructor",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotCallVirtualMethodInConstructor));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeConstructorSyntax, SyntaxKind.ConstructorDeclaration);
        }

        private static void AnalyzeConstructorSyntax(SyntaxNodeAnalysisContext context)
        {
            var node = (ConstructorDeclarationSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken) as IMethodSymbol;
            if (methodSymbol == null || methodSymbol.MethodKind != MethodKind.Constructor)
                return;

            // A sealed class cannot contains virtual methods
            if (methodSymbol.ContainingType.IsSealed)
                return;

            var body = (SyntaxNode)node.Body ?? node.ExpressionBody;
            if (body == null)
                return;

            var invocationSyntaxes = body.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocationSyntax in invocationSyntaxes)
            {
                var operation = context.SemanticModel.GetOperation(invocationSyntax, context.CancellationToken) as IInvocationOperation;
                if (operation == null)
                    continue;

                if (operation.TargetMethod.IsVirtual && IsThis(operation))
                {
                    context.ReportDiagnostic(s_rule, invocationSyntax);
                }
            }
        }

        private static bool IsThis(IInvocationOperation operation)
        {
            return operation.Instance is IInstanceReferenceOperation i && i.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance;
        }
    }
}

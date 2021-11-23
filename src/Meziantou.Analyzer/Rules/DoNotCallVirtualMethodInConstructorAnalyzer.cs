using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotCallVirtualMethodInConstructorAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.DoNotCallVirtualMethodInConstructor,
        title: "Do not call overridable members in constructor",
        messageFormat: "Do not call overridable members in constructor",
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
        if (context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken) is not IMethodSymbol methodSymbol || methodSymbol.MethodKind != MethodKind.Constructor)
            return;

        // A sealed class cannot contains virtual methods
        if (methodSymbol.ContainingType.IsSealed)
            return;

        var body = (SyntaxNode?)node.Body ?? node.ExpressionBody;
        if (body == null)
            return;

        var operation = context.SemanticModel.GetOperation(body, context.CancellationToken);
        if (operation == null)
            return;

        // Check method calls
        var invocationOperations = operation.DescendantsAndSelf().OfType<IInvocationOperation>();
        foreach (var invocationOperation in invocationOperations)
        {
            if (IsOverridable(invocationOperation.TargetMethod) && IsCurrentInstanceMethod(invocationOperation.Instance))
            {
                context.ReportDiagnostic(s_rule, invocationOperation.Syntax);
            }
        }

        // Check property access
        var references = operation.DescendantsAndSelf().OfType<IMemberReferenceOperation>();
        foreach (var reference in references)
        {
            var member = reference.Member;
            if (IsOverridable(member) && !reference.IsInNameofOperation())
            {
                var children = reference.Children.Take(2).ToList();
                if (children.Count == 1 && IsCurrentInstanceMethod(children[0]))
                {
                    context.ReportDiagnostic(s_rule, reference.Syntax);
                }
            }
        }
    }

    private static bool IsOverridable(ISymbol symbol)
    {
        return !symbol.IsSealed && (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride);
    }

    private static bool IsCurrentInstanceMethod(IOperation? operation)
    {
        if (operation == null)
            return false;

        return operation is IInstanceReferenceOperation i && i.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance;
    }
}

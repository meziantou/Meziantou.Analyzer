using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommaAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.MissingCommaInObjectInitializer,
        title: "Add a comma after the last value",
        messageFormat: "Add comma after the last value",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingCommaInObjectInitializer));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private static readonly ImmutableArray<SyntaxKind> ObjectInitializerKinds = ImmutableArray.Create(SyntaxKind.ObjectInitializerExpression, SyntaxKind.ArrayInitializerExpression, SyntaxKind.CollectionInitializerExpression);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(HandleObjectInitializer, ObjectInitializerKinds);
        context.RegisterSyntaxNodeAction(HandleAnonymousObjectInitializer, SyntaxKind.AnonymousObjectCreationExpression);
        context.RegisterSyntaxNodeAction(HandleEnumDeclaration, SyntaxKind.EnumDeclaration);
#if CSHARP12_OR_GREATER
        context.RegisterSyntaxNodeAction(HandleCollectionExpression, SyntaxKind.CollectionExpression);
#endif
    }

    private static void HandleSeparatedList<T>(SyntaxNodeAnalysisContext context, SyntaxNode node, SeparatedSyntaxList<T> elements) where T : SyntaxNode
    {
        if (elements.Count == 0)
            return;

        if (elements.Count == elements.SeparatorCount || !node.SpansMultipleLines(context.CancellationToken))
            return;

        var lastMember = elements[^1];
        context.ReportDiagnostic(Rule, lastMember);
    }

#if CSHARP12_OR_GREATER
    private void HandleCollectionExpression(SyntaxNodeAnalysisContext context)
    {
        var node = (CollectionExpressionSyntax)context.Node;
        HandleSeparatedList(context, node, node.Elements);
    }
#endif

    private static void HandleEnumDeclaration(SyntaxNodeAnalysisContext context)
    {
        var node = (EnumDeclarationSyntax)context.Node;
        HandleSeparatedList(context, node, node.Members);
    }

    private static void HandleObjectInitializer(SyntaxNodeAnalysisContext context)
    {
        var node = (InitializerExpressionSyntax)context.Node;
        HandleSeparatedList(context, node, node.Expressions);
    }

    private static void HandleAnonymousObjectInitializer(SyntaxNodeAnalysisContext context)
    {
        var node = (AnonymousObjectCreationExpressionSyntax)context.Node;
        HandleSeparatedList(context, node, node.Initializers);
    }
}

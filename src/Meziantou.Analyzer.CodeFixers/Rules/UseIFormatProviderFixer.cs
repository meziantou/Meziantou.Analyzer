using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseIFormatProviderFixer : CodeFixProvider
{
    private const string CurrentCultureExpression = "System.Globalization.CultureInfo.CurrentCulture";
    private const string InvariantCultureExpression = "System.Globalization.CultureInfo.InvariantCulture";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseIFormatProviderParameter);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        var invocationExpression = nodeToFix?.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocationExpression is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetOperation(invocationExpression, context.CancellationToken) is not IInvocationOperation invocationOperation)
            return;

        var formatProviderSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.IFormatProvider");
        var stringSymbol = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
        if (formatProviderSymbol is null || stringSymbol is null)
            return;

        var overloadFinder = new OverloadFinder(semanticModel.Compilation);
        var overload = overloadFinder.FindOverloadWithAdditionalParameterOfType(
            invocationOperation,
            new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: false),
            [new OverloadParameterType(formatProviderSymbol, AllowInherits: true)]);

        if (overload is not null && TryGetFormatProviderParameterInfo(invocationOperation.TargetMethod, overload, formatProviderSymbol, out var insertionIndex, out var parameterName))
        {
            RegisterCodeFix(CurrentCultureExpression, "Use CultureInfo.CurrentCulture");
            RegisterCodeFix(InvariantCultureExpression, "Use CultureInfo.InvariantCulture");
            return;
        }

        if (invocationOperation.TargetMethod.Name == nameof(object.ToString) && invocationOperation.Arguments.IsEmpty)
        {
            overload = overloadFinder.FindOverloadWithAdditionalParameterOfType(
                invocationOperation,
                new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: false),
                [new OverloadParameterType(stringSymbol), new OverloadParameterType(formatProviderSymbol, AllowInherits: true)]);

            if (overload is not null && CanFixToStringOverload(overload, formatProviderSymbol))
            {
                RegisterToStringCodeFix(CurrentCultureExpression, "Use CultureInfo.CurrentCulture");
                RegisterToStringCodeFix(InvariantCultureExpression, "Use CultureInfo.InvariantCulture");
            }
        }

        void RegisterCodeFix(string formatProviderExpression, string title)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => AddFormatProviderArgument(context.Document, invocationExpression, insertionIndex, parameterName, formatProviderExpression, ct),
                    equivalenceKey: title),
                context.Diagnostics);
        }

        void RegisterToStringCodeFix(string formatProviderExpression, string title)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => AddFormatAndFormatProviderArguments(context.Document, invocationExpression, overload!, formatProviderSymbol, formatProviderExpression, ct),
                    equivalenceKey: title),
                context.Diagnostics);
        }
    }

    private static async Task<Document> AddFormatProviderArgument(Document document, InvocationExpressionSyntax invocationExpression, int insertionIndex, string parameterName, string formatProviderExpression, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var formatProviderSyntax = SyntaxFactory.ParseExpression(formatProviderExpression);
        var currentArguments = invocationExpression.ArgumentList.Arguments;
        var newArguments = insertionIndex > currentArguments.Count
            ? currentArguments.Add((ArgumentSyntax)generator.Argument(parameterName, RefKind.None, formatProviderSyntax))
            : currentArguments.Insert(insertionIndex, (ArgumentSyntax)generator.Argument(formatProviderSyntax));

        editor.ReplaceNode(invocationExpression, invocationExpression.WithArgumentList(SyntaxFactory.ArgumentList(newArguments)));
        return editor.GetChangedDocument();
    }

    private static async Task<Document> AddFormatAndFormatProviderArguments(Document document, InvocationExpressionSyntax invocationExpression, IMethodSymbol overload, ITypeSymbol formatProviderSymbol, string formatProviderExpression, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var arguments = new List<ArgumentSyntax>(capacity: overload.Parameters.Length);
        foreach (var parameter in overload.Parameters)
        {
            ExpressionSyntax expression;
            if (parameter.Type.IsString())
            {
                expression = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
            }
            else if (parameter.Type.IsOrInheritFrom(formatProviderSymbol))
            {
                expression = SyntaxFactory.ParseExpression(formatProviderExpression);
            }
            else
            {
                return document;
            }

            arguments.Add(SyntaxFactory.Argument(expression));
        }

        var newInvocation = invocationExpression.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
        var newRoot = root.ReplaceNode(invocationExpression, newInvocation);
        return document.WithSyntaxRoot(newRoot);
    }

    private static bool TryGetFormatProviderParameterInfo(IMethodSymbol method, IMethodSymbol overload, ITypeSymbol formatProviderSymbol, out int insertionIndex, out string parameterName)
    {
        for (var i = 0; i < overload.Parameters.Length; i++)
        {
            var parameter = overload.Parameters[i];
            if (!parameter.Type.IsOrInheritFrom(formatProviderSymbol))
                continue;

            if (i >= method.Parameters.Length || !method.Parameters[i].Type.IsOrInheritFrom(formatProviderSymbol))
            {
                insertionIndex = i;
                parameterName = parameter.Name;
                return true;
            }
        }

        insertionIndex = -1;
        parameterName = string.Empty;
        return false;
    }

    private static bool CanFixToStringOverload(IMethodSymbol overload, ITypeSymbol formatProviderSymbol)
    {
        if (overload.Parameters.Length != 2)
            return false;

        return overload.Parameters.Any(parameter => parameter.Type.IsString()) &&
               overload.Parameters.Any(parameter => parameter.Type.IsOrInheritFrom(formatProviderSymbol)) &&
               overload.Parameters.All(parameter => parameter.Type.IsString() || parameter.Type.IsOrInheritFrom(formatProviderSymbol));
    }
}
